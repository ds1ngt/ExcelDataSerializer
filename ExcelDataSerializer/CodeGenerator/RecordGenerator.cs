using System.CodeDom;
using System.Text;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

public class RecordGenerator
{
    public static DataClassInfo? GenerateDataClass(TableInfo.DataTable dataTable)
    {
        using var builder = CodeBuilder.NewBuilder("com.haegin.Billionaire.Board.Data");
        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            case TableInfo.TableType.Dictionary:
            {
                // Data Class
                {
                    var cls = new CodeTypeDeclaration($"{dataTable.Name}Data");
                    cls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));

                    foreach (var cell in dataTable.Header.SchemaCells)
                    {
                        var member = new CodeMemberField();
                        member.Attributes = MemberAttributes.Public;
                        member.Name = cell.Name;
                        member.Type = new CodeTypeReference(cell.ValueType);
                        cls.Members.Add(member);
                    }
                    builder.AddMember(cls);
                }

                // Table Class
                {
                    var tableCls = new CodeTypeDeclaration($"{dataTable.Name}DataTable");
                    tableCls.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));

                    string keyType = string.Empty;
                    if (dataTable.Header.HasPrimaryKey)
                    {
                        var primaryCellIdx = dataTable.Header.SchemaCells.FindIndex(schema => schema.Index == dataTable.Header.PrimaryIndex);
                        if (primaryCellIdx == -1)
                            break;
                        keyType = dataTable.Header.SchemaCells[primaryCellIdx].ValueType;
                    }
        
                    var member = new CodeMemberField();
                    member.Attributes = MemberAttributes.Public;
                    member.Name = "Datas";
                    member.Type = new CodeTypeReference
                    {
                        BaseType = $"Dictionary<{keyType}, {dataTable.Name}Data>",
                    };
                    tableCls.Members.Add(member);
                    builder.AddMember(tableCls);
                }
            }
                break;
            case TableInfo.TableType.Enum:
            {
                foreach (var schema in dataTable.Header.SchemaCells)
                {
                    var enumType = new CodeTypeDeclaration(schema.Name);
                    var column = dataTable.Data.SelectMany(r => r.DataCells.Where(cell => cell.Index == schema.Index));
                    foreach (var cell in column)
                    {
                        if (cell.Index != schema.Index) continue;
                        if (string.IsNullOrWhiteSpace(cell.Value))
                            continue;

                        enumType.Members.Add(new CodeMemberField {Name = cell.Value});
                    }
                    enumType.IsEnum = true;
                    builder.AddMember(enumType);
                }
            }
                break;
            default:
                break;
        }

        return new DataClassInfo
        {
            Name = dataTable.Name,
            TableType = dataTable.TableType,
            Code = builder.GenerateCode()
        };
    }

    public static string GenerateRecordData(TableInfo.DataTable dataTable)
    {
        if (!dataTable.Header.HasPrimaryKey)
            return string.Empty;
        
        var schemaMap = dataTable.Header.SchemaCells.ToDictionary(g => g.Index, g => g);
        var itemMap = new Dictionary<string, string[]>();
        var cellItems = new List<string>();

        var key = string.Empty;
        foreach (var row in dataTable.Data)
        {
            cellItems.Clear();
            foreach (var cell in row.DataCells)
            {
                if (cell.Index == dataTable.Header.PrimaryIndex)
                    key = cell.Value;

                if (schemaMap.TryGetValue(cell.Index, out var schema))
                {
                    cellItems.Add($"\"{schema.Name}\":{GetCellValue(schema, cell)}");
                }
            }

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!itemMap.TryAdd(key, cellItems.ToArray()))
            {
                Console.WriteLine($"KEY DUPLICATE = {dataTable.Name} : {key}");
            }
        }
        
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

    private static string GetCellValue(TableInfo.SchemaCell schema, TableInfo.DataCell cell)
    {
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

                switch (type)
                {
                    case Types.String:
                        return $"\"{cell.Value}\"";
                    default:
                        return cell.Value;
                }
            }
            case SchemaTypes.Array:
            case SchemaTypes.List:
            case SchemaTypes.Custom:
                return cell.Value;
            case SchemaTypes.EnumGet:
                return cell.Value;
            case SchemaTypes.EnumSet:
            case SchemaTypes.None:
            default:
                break;
        }

        return string.Empty;
    }
}