using System.CodeDom;
using System.Data;
using System.Text;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class RecordGenerator
{
#region Data Class
    public static DataClassInfo? GenerateDataClass(TableInfo.DataTable dataTable)
    {
        using var builder = CodeBuilder.NewBuilder("com.haegin.Billionaire.Board.Data");
        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            case TableInfo.TableType.Dictionary:
            {
                AddDataClass(builder, dataTable);
                AddTableClass(builder, dataTable);
            }
                break;
            case TableInfo.TableType.Enum:
            {
                AddEnumClass(builder, dataTable);
            }
                break;
        }

        return new DataClassInfo
        {
            Name = dataTable.Name,
            TableType = dataTable.TableType,
            Code = builder.GenerateCode()
        };
    }

    private static void AddDataClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        // Data Class
        var cls = new CodeTypeDeclaration($"{dataTable.Name}Data");
        cls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
                
        foreach (var cell in dataTable.Header!.SchemaCells)
        {
            var member = new CodeMemberField
            {
                Attributes = MemberAttributes.Public,
                Name = TrimUnderscore(cell.Name),
                Type = new CodeTypeReference(GetTypeStr(cell.SchemaTypes, cell.ValueType))
            };
            cls.Members.Add(member);
        }
        builder.AddMember(cls);
    }

    private static void AddTableClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        if(dataTable.TableType == TableInfo.TableType.Dictionary && !dataTable.Header!.HasPrimaryKey)
            return;

        var tableCls = new CodeTypeDeclaration($"{dataTable.Name}DataTable");
        tableCls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));

        var keyType = string.Empty;
        if (dataTable.Header!.HasPrimaryKey)
        {
            var primaryCellIdx = dataTable.Header.SchemaCells.FindIndex(schema => schema.Index == dataTable.Header.PrimaryIndex);
            if (primaryCellIdx == -1)
                return;
            keyType = dataTable.Header.SchemaCells[primaryCellIdx].ValueType;
        }
    
        var member = new CodeMemberField
        {
            Attributes = MemberAttributes.Public,
            Name = Constant.DataTableMemberName,
            Type = new CodeTypeReference
            {
                BaseType = $"Dictionary<{keyType}, {dataTable.Name}Data>",
            }
        };
        tableCls.Members.Add(member);
        builder.AddMember(tableCls);
    }

    private static void AddEnumClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        foreach (var schema in dataTable.Header!.SchemaCells)
        {
            var enumType = new CodeTypeDeclaration(schema.Name);
            var column = dataTable.Data.SelectMany(r => r.DataCells.Where(cell => cell.Index == schema.Index));
            foreach (var cell in column)
            {
                if (cell.Index != schema.Index) continue;
                var name = TrimUnderscore(cell.Value);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                enumType.Members.Add(new CodeMemberField {Name = name});
            }
            enumType.IsEnum = true;
            builder.AddMember(enumType);
        }
    }
    private static string GetTypeStr(SchemaTypes schemaTypes, string valueType) => schemaTypes switch
    {
        SchemaTypes.Array => $"{valueType}[]",
        SchemaTypes.List => $"List<{valueType}>",
        _ => valueType
    };
#endregion // Data Class

#region Json Data
    public static string GenerateRecordData(TableInfo.DataTable dataTable)
    {
        if (dataTable.Header is { HasPrimaryKey: false })
            return string.Empty;

        var itemMap = ConvertToDictionary(dataTable);

        var sb = new StringBuilder();
        var i = 0;
        var itemLen = itemMap.Keys.Count;
        sb.Append($"{{\"Datas\":");
        sb.Append("{");
        foreach (var (itemKey, itemValue) in itemMap)
        {
            sb.Append($"\"{itemKey}\"");
            sb.Append(":{");
            sb.Append(itemValue.Aggregate((l, r) => $"{l}, {r}"));
            sb.Append("}");
            if (i < itemLen - 1)
                sb.Append(",");
            sb.AppendLine();
            i++;
        }
        sb.AppendLine("}}");
        return sb.ToString();
    }

    private static Dictionary<string, string[]> ConvertToDictionary(TableInfo.DataTable dataTable)
    {
        var schemaMap = dataTable.Header!.SchemaCells.ToDictionary(g => g.Index, g => g);
        var itemMap = new Dictionary<string, string[]>();

        foreach (var row in dataTable.Data)
        {
            var key = GetPrimaryKey(row);
            var cellItems = GetCellItems(row);

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!itemMap.TryAdd(key, cellItems.ToArray()))
            {
                Console.WriteLine($"KEY DUPLICATE = {dataTable.Name} : {key}");
            }
        }

        string GetPrimaryKey(TableInfo.DataRow row)
        {
            var idx = Array.FindIndex(row.DataCells, c => c.Index == dataTable.Header.PrimaryIndex);
            return idx == -1 ? string.Empty : row.DataCells[idx].Value;
        }

        string[] GetCellItems(TableInfo.DataRow row)
        {
            var result = new List<string>();
            foreach (var cell in row.DataCells)
            {
                if (schemaMap.TryGetValue(cell.Index, out var schema))
                {
                    result.Add($"\"{schema.Name}\":{GetCellValue(schema, cell)}");
                }
            }

            return result.ToArray();
        }
        return itemMap;
    }

    private static string GetCellValue(TableInfo.SchemaCell schema, TableInfo.DataCell cell)
    {
        var result = string.Empty;

        switch (schema.SchemaTypes)
        {
            case SchemaTypes.Primitive:
            {
                var valueType = schema.ValueType;
                var idx = valueType.LastIndexOf('.');
                if (idx != -1)
                {
                    valueType = valueType.Substring(idx + 1);
                }

                if (!TypesExtension.TryGetValue(valueType, out var type))
                {
                    Console.WriteLine($"Invalid Type = {valueType}");
                    return string.Empty;
                }

                result =  GetPrimitiveValueString(type, cell.Value);
            }
                break;
            case SchemaTypes.Array:
            case SchemaTypes.List:
                result = !IsContainer(cell.Value) ? "null" : cell.Value;
                break;
            case SchemaTypes.Custom:
                result = cell.Value;
                break;
            case SchemaTypes.EnumGet:
                result = cell.Value;
                break;
            case SchemaTypes.EnumSet:
            case SchemaTypes.None:
            default:
                break;
        }
        return TrimUnderscore(result);
    }

    private static string TrimUnderscore(string value) => value.Replace("_", string.Empty);
    private static bool IsContainer(string value) => value.Length > 2 && value.StartsWith('[') && value.EndsWith(']');

    private static string GetPrimitiveValueString(Types type, string value)
    {
        string result;
        switch (type)
        {
            case Types.Byte:
                result = byte.TryParse(value, out var b) ? b.ToString() : default(byte).ToString();
                break;
            case Types.Short:
            case Types.Int16:
                result = short.TryParse(value, out var s) ? s.ToString() : default(short).ToString();
                break;
            case Types.UShort:
            case Types.UInt16:
                result = ushort.TryParse(value, out var us) ? us.ToString() : default(short).ToString();
                break;
            case Types.Int:
            case Types.Int32:
                result = int.TryParse(value, out var i) ? i.ToString() : default(int).ToString();
                break;
            case Types.UInt:
            case Types.UInt32:
                result = uint.TryParse(value, out var ui) ? ui.ToString() : default(short).ToString();
                break;
            case Types.Long:
            case Types.Int64:
                result = long.TryParse(value, out var l) ? l.ToString() : default(short).ToString();
                break;
            case Types.ULong:
            case Types.UInt64:
                result = ulong.TryParse(value, out var ul) ? ul.ToString() : default(short).ToString();
                break;
            case Types.Float:
            case Types.Single:
                result = float.TryParse(value, out var f) ? f.ToString() : default(short).ToString();
                break;
            case Types.Double:
                result = double.TryParse(value, out var d) ? d.ToString() : default(short).ToString();
                break;
            case Types.Decimal:
                result = decimal.TryParse(value, out var dec) ? dec.ToString() : default(short).ToString();
                break;
            case Types.Boolean:
                result = bool.TryParse(value, out var bo) ? bo.ToString() : default(short).ToString();
                break;
            case Types.String:
                result = $"\"{value}\"";
                break;
            case Types.Vector3:
            case Types.Quaternion:
            default:
                result = string.Empty;
                break;
        }

        return result;
    }
#endregion // Json Data
}