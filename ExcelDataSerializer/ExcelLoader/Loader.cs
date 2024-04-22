using ClosedXML.Excel;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.ExcelLoader;

public abstract class Loader
{
    private const int HEADER_NAME_ROW = 1;
    private const int SCHEMA_ROW = 2;
    private const int DATA_BEGIN_ROW = 3;
    public static IEnumerable<TableInfo.DataTable> LoadXls(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<TableInfo.DataTable>();

        Logger.Instance.LogLine($"Excel 로드 = {path}");
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var dataTables = new List<TableInfo.DataTable>();
        var workbook = new XLWorkbook(fs);
        foreach (var sheet in workbook.Worksheets)
        {
            var sheetName = sheet.Name.Replace("_", string.Empty);
            Logger.Instance.LogLine();
            Logger.Instance.LogLine($" - {sheetName}");
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

        var header = CreateHeaderRow(range);
        if (header == null)
            return null;

        var validColumnIndices = header.SchemaCells.Select(cell => cell.Index);
        var result = new TableInfo.DataTable
        {
            Name = name,
            Header = header,
            Data = CreateDataRows(header, range, validColumnIndices),
            TableType = GetTableType(header),
        };
        return result;
    }
    private static TableInfo.Header? CreateHeaderRow(IXLRange range)
    {
        var result = new TableInfo.Header();

        var headerNameRow = range.Row(HEADER_NAME_ROW);
        var schemaRow = range.Row(SCHEMA_ROW);
        if (headerNameRow == null || schemaRow == null || headerNameRow.CellCount() != schemaRow.CellCount())
            return null;

        var cellCount = headerNameRow.CellCount();

        for (var i = 1; i <= cellCount; ++i)
        {
            var name = headerNameRow.Cell(i);
            var schema = schemaRow.Cell(i);
            if (name == null || schema == null)
                continue;

            if (!IsValidHeaderCell(name))
                continue;
            
            var schemaText = schema.GetValue<string>();
            if (string.IsNullOrWhiteSpace(schemaText))
                continue;

            var tokens = schemaText.Split('/');
            var schemaInfo = ParseSchemaInfo(tokens);
            var cellName = name.GetValidName();

            if (schemaInfo.IsPrimary)
            {
                if(result.PrimaryIndex != null)
                    throw new ArgumentException($"Primary Key Duplicated...({result.PrimaryIndex}, {i})");
                result.PrimaryIndex = i;
            }

            var schemaCell = new TableInfo.SchemaCell
            {
                Index = i,
                Name = cellName,
                SchemaTypes = schemaInfo.SchemaType,
                ValueType = Util.Util.TrimUnderscore(schemaInfo.DataType),
            };
            result.SchemaCells.Add(schemaCell);
            Logger.Instance.LogLine($"{schemaCell.Name} [ {schemaCell.Index} ] {schemaCell.SchemaTypes} / {schemaCell.ValueType}");
        }

        Logger.Instance.LogLine();
        return result;
    }

    private static bool IsValidHeaderCell(IXLCell? cell)
    {
        if (cell == null)
            return false;

        if (!cell.TryGetValue<string>(out var cellValue)) 
            return false;

        var value = Util.Util.GetValidName(cellValue);
        return Util.Util.IsValidName(value);
    }
    private static bool IsPrimary(string[] tokens) => IsContains(Constant.Primary, tokens);
    private static bool IsPrimary(string token) => IsContains(Constant.Primary, token);
    private static bool IsContainer(string[] tokens) => IsArray(tokens) || IsList(tokens);
    private static bool IsContainer(string token) => IsArray(token) || IsList(token);
    private static bool IsArray(string[] tokens) => IsContains(SchemaTypes.Array, tokens);
    private static bool IsArray(string token) => IsContains(SchemaTypes.Array, token);
    private static bool IsList(string[] tokens) => IsContains(SchemaTypes.List, tokens);
    private static bool IsList(string token) => IsContains(SchemaTypes.List, token);

    private static bool IsEnumGet(string[] tokens) =>  IsContains(SchemaTypes.EnumGet, tokens) || IsContains(SchemaTypes.Enum, tokens);
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
        else if (IsEnumGet(tokens))
            info.SchemaType = SchemaTypes.EnumGet;

        if (TryGetPrimitive(tokens, out var type))
        {
            if (info.SchemaType == SchemaTypes.None)
                info.SchemaType = SchemaTypes.Primitive;
            info.DataType = type.GetTypeStr();
        }
        else if (TryGetEnum(tokens, info.IsPrimary, out var enumTypeStr))
        {
            var enumName = Util.Util.GetValidName(enumTypeStr);
            info.DataType = enumName;
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

    private static bool TryGetEnum(string[] tokens, bool isPrimary, out string enumTypeStr)
    {
        enumTypeStr = string.Empty;

        if (!IsValid())
            return false;

        var schemaIdx = Constant.SchemaCellIdx;
        var typeIdx = Constant.TypeCellIdx;
        
        if (isPrimary)
        {
            schemaIdx++;
            typeIdx++;
        }
        if (tokens[schemaIdx].ToLower() != Constant.EnumGet.ToLower())
            return false;

        enumTypeStr = tokens[typeIdx];
        return true;

        bool IsValid() => isPrimary ? tokens.Length >= 3 : tokens.Length >= 2;
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
    
    private static TableInfo.DataRow[] CreateDataRows(TableInfo.Header header, IXLRange range, IEnumerable<int> validColumIndices)
    {
        var rows = new List<TableInfo.DataRow>();
        var cells = new List<TableInfo.DataCell>();
        foreach (var row in range.Rows(DATA_BEGIN_ROW, range.RowCount()))
        {
            cells.Clear();
            foreach (var idx in validColumIndices)
            {
                if (!row.Cell(idx).TryGetValue<string>(out var value))
                    continue;

                cells.Add(new TableInfo.DataCell
                {
                    Index = idx,
                    Value = GetDataRowValue(header, idx, value),
                });
            }
            rows.Add(new TableInfo.DataRow
            {
                DataCells = cells.ToArray()
            });
        }
        return rows.ToArray();
    }

    private static string GetDataRowValue(TableInfo.Header header, int idx, string value)
    {
        var schemaIndex = header.SchemaCells.FindIndex(item => item.Index == idx);
        switch (header.SchemaCells[schemaIndex].SchemaTypes)
        {
            case SchemaTypes.Enum:
            case SchemaTypes.EnumGet:
            case SchemaTypes.EnumSet:
                return Util.Util.GetValidName(value);
                break;
            default:
                return value;
        }
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