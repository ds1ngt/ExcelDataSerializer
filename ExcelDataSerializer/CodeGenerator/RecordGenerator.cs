using System.CodeDom;
using System.Globalization;
using System.Text;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class RecordGenerator
{
    private static TableInfo.DataTable _enumTable;
    public static void SetEnumTable(TableInfo.DataTable enumTable) => _enumTable = enumTable ?? new TableInfo.DataTable();

#region Data Class

    public static DataClassInfo? GenerateInterface()
    {
        using var builder = CodeBuilder.NewBuilder(Constant.DATA_NAMESPACE);
        var iTableData = new CodeTypeDeclaration(Constant.INTERFACE_NAME)
        {
            IsInterface = true
        };
        builder.AddMember(iTableData);

        return new DataClassInfo
        {
            Name = Constant.INTERFACE_NAME,
            TableType = TableInfo.TableType.Interface,
            CsFileName = Constant.INTERFACE_NAME,
            Code = builder.GenerateCode(),
            IsInterface = true,
        };
    }
    public static DataClassInfo? GenerateDataClass(TableInfo.DataTable dataTable)
    {
        using var builder = CodeBuilder.NewBuilder(Constant.DATA_NAMESPACE);
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
        var name = NamingRule.Check(dataTable.Name);
        var cls = new CodeTypeDeclaration($"{name}{Constant.DATA_SUFFIX}");
        cls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
        cls.IsPartial = true;

        foreach (var cell in dataTable.Header!.SchemaCells)
        {
            var member = new CodeMemberField
            {
                Attributes = MemberAttributes.Public,
                Name = Util.Util.TrimUnderscore(cell.Name),
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

        var name = NamingRule.Check(dataTable.Name);
        var tableCls = new CodeTypeDeclaration($"{name}{Constant.DATA_TABLE_SUFFIX}");
        tableCls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
        tableCls.BaseTypes.Add(Constant.INTERFACE_NAME);
        tableCls.IsPartial = true;

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
                BaseType = $"Dictionary<{keyType}, {name}{Constant.DATA_SUFFIX}>",
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
                var name = Util.Util.TrimUnderscore(cell.Value);
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
        foreach (var kvp in itemMap)
        {
            var itemKey = kvp.Key;
            var itemValue = kvp.Value;
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
        Logger.Instance.LogLine($"데이터 생성 {dataTable.Name} : ({itemMap.Count})개 항목");
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

            if (itemMap.ContainsKey(key))
                Console.WriteLine($"KEY DUPLICATE = {dataTable.Name} : {key}");
            else
                itemMap.Add(key, cellItems.ToArray());
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

#region Get Cell Value
    private static string GetCellValue(TableInfo.SchemaCell schema, TableInfo.DataCell data)
    {
        var result = string.Empty;

        switch (schema.SchemaTypes)
        {
            case SchemaTypes.Primitive:
                return GetPrimitiveCellValue(schema, data);
            case SchemaTypes.Array:
            case SchemaTypes.List:
                result = !IsContainer(data.Value) ? "null" : data.Value;
                break;
            case SchemaTypes.Custom:
                result = data.Value;
                break;
            case SchemaTypes.EnumGet:
            {
                result = _enumTable.TryGetEnumValue(schema.ValueType, data.Value, out var value) ? value.ToString() : "0";
            }
                break;
            case SchemaTypes.EnumSet:
            case SchemaTypes.None:
            default:
                break;
        }
        
        return Util.Util.TrimUnderscore(result);
    }

    private static string GetPrimitiveCellValue(TableInfo.SchemaCell schema, TableInfo.DataCell data)
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
        // String 타입 파싱할 때 밑줄이 제거되어 주석처리. 문제시 복구
        // return Util.Util.TrimUnderscore(GetPrimitiveValueString(type, data.Value));
        return GetPrimitiveValueString(type, data.Value);
    }
    private static bool IsContainer(string value) => value.Length > 2 && value.StartsWith("[") && value.EndsWith("]");

    private static string GetPrimitiveValueString(Types type, string value)
    {
        switch (type)
        {
            case Types.Byte:
                return byte.TryParse(value, out var b) ? b.ToString() : default(byte).ToString();
            case Types.Short:
            case Types.Int16:
                return short.TryParse(value, out var s) ? s.ToString() : default(short).ToString();
            case Types.UShort:
            case Types.UInt16:
                return ushort.TryParse(value, out var us) ? us.ToString() : default(ushort).ToString();
            case Types.Int:
            case Types.Int32:
                return int.TryParse(value, out var i) ? i.ToString() : default(int).ToString();
            case Types.UInt:
            case Types.UInt32:
                return uint.TryParse(value, out var ui) ? ui.ToString() : default(uint).ToString();
            case Types.Long:
            case Types.Int64:
                return long.TryParse(value, out var l) ? l.ToString() : default(long).ToString();
            case Types.ULong:
            case Types.UInt64:
                return ulong.TryParse(value, out var ul) ? ul.ToString() : default(ulong).ToString();
            case Types.Float:
            case Types.Single:
                return float.TryParse(value, out var f) ? f.ToString(CultureInfo.InvariantCulture) : default(float).ToString(CultureInfo.InvariantCulture);
            case Types.Double:
                return double.TryParse(value, out var d) ? d.ToString(CultureInfo.InvariantCulture) : default(double).ToString(CultureInfo.InvariantCulture);
            case Types.Decimal:
                return decimal.TryParse(value, out var dec) ? dec.ToString(CultureInfo.InvariantCulture) : default(decimal).ToString(CultureInfo.InvariantCulture);
            case Types.Bool:
            case Types.Boolean:
            {
                var valueStr = bool.TryParse(value, out var bo) ? bo.ToString() : default(bool).ToString();
                return $"\"{valueStr}\"";
            }
            case Types.String:
                return $"\"{value}\"";
            case Types.Vector3:
            case Types.Quaternion:
            default:
                return string.Empty;
        }
    }
#endregion // Get Cell Value
#endregion // Json Data
}