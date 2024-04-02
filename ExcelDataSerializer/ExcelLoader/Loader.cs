using ClosedXML.Excel;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.ExcelLoader;

public abstract class Loader
{
    public static IEnumerable<TableInfo.DataTable> LoadXls(string path)
    {
        Console.WriteLine($"Load Excel = {path}");
        var dataTables = new List<TableInfo.DataTable>();
        var workbook = new XLWorkbook(path);

        foreach (var sheet in workbook.Worksheets)
        {
            var sheetName = sheet.Name.Replace("_", string.Empty);
            Console.WriteLine($" - {sheetName}");
            var range = sheet.RangeUsed();
            if(range == null)
                continue;

            var dataTable = CreateDataTable(sheetName, range);
            if (dataTable == null) continue;

            // dataTable.PrintHeader();
            // dataTable.PrintData();
            dataTables.Add(dataTable);
        }
        return dataTables;
    }

    private static TableInfo.DataTable? CreateDataTable(string name, IXLRange range)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var header = CreateHeaderRow(range.FirstRow());
        var validColumnIndices = header.SchemaCells.Select(cell => cell.Index);
        var result = new TableInfo.DataTable
        {
            Name = name,
            Header = header,
            Data = CreateDataRows(range, validColumnIndices),
            TableType = GetTableType(header),
        };
        return result;
    }
    private static TableInfo.Header CreateHeaderRow(IXLRangeRow firstRow)
    {
        var result = new TableInfo.Header();
        for (var i = 1; i <= firstRow.CellCount(); ++i)
        {
            var cell = firstRow.Cell(i);
            if (!IsValidHeaderCell(cell))
                continue;

            if (!cell.HasComment)
                continue;

            var comment = cell.GetComment().Text;
            if (string.IsNullOrWhiteSpace(comment))
                continue;

            comment = comment.Substring('\n');
            
            var tokens = comment.Split('/');
            var schemaInfo = ParseSchemaInfo(tokens);
            var cellName = cell.GetValidName();

            if (schemaInfo.IsPrimary)
            {
                if(result.PrimaryIndex != null)
                    throw new ArgumentException($"Primary Key Duplicated...({result.PrimaryIndex}, {i})");
                result.PrimaryIndex = i;
            }

            if (schemaInfo.SchemaType is SchemaTypes.EnumSet or SchemaTypes.EnumGet)
                schemaInfo.DataType = cellName;

            var schemaCell = new TableInfo.SchemaCell
            {
                Index = i,
                Name = cellName,
                SchemaTypes = schemaInfo.SchemaType,
                ValueType = schemaInfo.DataType,
            };
            result.SchemaCells.Add(schemaCell);
            Console.WriteLine($"{schemaCell.Name} [ {schemaCell.Index} ] {schemaCell.SchemaTypes} / {schemaCell.ValueType}");
        }
        return result;
    }

    private static bool IsValidHeaderCell(IXLCell? cell)
    {
        if (cell == null)
            return false;

        if (!cell.TryGetValue<string>(out var cellValue)) 
            return false;

        var value = Util.GetValidName(cellValue);
        return Util.IsValidName(value);
    }
    private static bool IsPrimary(string[] tokens) => IsContains(Constant.Primary, tokens);
    private static bool IsPrimary(string token) => IsContains(Constant.Primary, token);
    private static bool IsContainer(string[] tokens) => IsArray(tokens) || IsList(tokens);
    private static bool IsContainer(string token) => IsArray(token) || IsList(token);
    private static bool IsArray(string[] tokens) => IsContains(SchemaTypes.Array, tokens);
    private static bool IsArray(string token) => IsContains(SchemaTypes.Array, token);
    private static bool IsList(string[] tokens) => IsContains(SchemaTypes.List, tokens);
    private static bool IsList(string token) => IsContains(SchemaTypes.List, token);
    private static bool IsEnumGet(string[] tokens) => IsContains(SchemaTypes.EnumGet, tokens);
    private static bool IsEnumGet(string token) => IsContains(SchemaTypes.EnumGet, token);
    private static bool IsContains(SchemaTypes schemaTypes, string[] tokens) => IsContains(schemaTypes.ToString(), tokens);
    private static bool IsContains(SchemaTypes schemaTypes, string token) => IsContains(schemaTypes.ToString(), token);
    private static bool IsContains(string schemaTypeStr, string[] tokens)
    {
        var compare = schemaTypeStr.ToLower();
        return tokens.Any(token => compare == token.ToLower());
    }
    
    private static bool IsContains(string schemaTypeStr, string token)
    {
        if (string.IsNullOrWhiteSpace(schemaTypeStr) || string.IsNullOrWhiteSpace(token))
            return false;

        var compare = schemaTypeStr.ToLower();
        return compare == token.ToLower();
    }
    private static SchemaInfo ParseSchemaInfo(string[] tokens)
    {
        var info = new SchemaInfo
        {
            IsPrimary = IsPrimary(tokens),
        };

        if (IsContains(SchemaTypes.Array, tokens))
            info.SchemaType = SchemaTypes.Array;
        else if (IsContains(SchemaTypes.List, tokens))
            info.SchemaType = SchemaTypes.List;
        else if (IsContains(SchemaTypes.EnumSet, tokens))
            info.SchemaType = SchemaTypes.EnumSet;
        else if (IsContains(SchemaTypes.EnumGet, tokens))
            info.SchemaType = SchemaTypes.EnumGet;

        if (TryGetPrimitive(tokens, out var type))
        {
            if (info.SchemaType == SchemaTypes.None)
                info.SchemaType = SchemaTypes.Primitive;
            info.DataType = type.GetTypeStr();
        }
        else
        {
            if (info.SchemaType == SchemaTypes.None)
                info.SchemaType = SchemaTypes.Custom;
            info.DataType = GetCustomDataTypeStr(tokens);
        }
        return info;
    }
    private static bool TryGetPrimitive(string[] tokens, out Types type)
    {
        type = Types.Byte;
        foreach (var token in tokens)
        {
            if (TypesExtension.TryGetValue(token, out type))
                return true;
        }

        return false;
    }

    private static string GetCustomDataTypeStr(string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (SchemaExtension.IsSchema(token)) continue;
            if (TypesExtension.IsType(token)) continue;
            return token;
        }

        return string.Empty;
    }
    
    private static TableInfo.DataRow[] CreateDataRows(IXLRange range, IEnumerable<int> validColumIndices)
    {
        var rows = new List<TableInfo.DataRow>();
        var cells = new List<TableInfo.DataCell>();
        foreach (var row in range.Rows().Skip(1))
        {
            cells.Clear();
            foreach (var idx in validColumIndices)
            {
                if (!row.Cell(idx).TryGetValue<string>(out var value))
                    continue;

                cells.Add(new TableInfo.DataCell
                {
                    Index = idx,
                    Value = value,
                });
            }
            rows.Add(new TableInfo.DataRow
            {
                DataCells = cells.ToArray()
            });
        }
        return rows.ToArray();
    }

    private static TableInfo.TableType GetTableType(TableInfo.Header header)
    {
        if (header.HasPrimaryKey)
            return TableInfo.TableType.Dictionary;
        if (header.SchemaCells.Exists(c => c.SchemaTypes == SchemaTypes.EnumSet))
            return TableInfo.TableType.Enum;
        return TableInfo.TableType.List;
    }
}