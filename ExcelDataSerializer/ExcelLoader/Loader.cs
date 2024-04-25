using ClosedXML.Excel;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using XlsxHelper;

namespace ExcelDataSerializer.ExcelLoader;

public abstract class Loader
{
    private const int HEADER_NAME_ROW = 1;
    private const int SCHEMA_ROW = 2;
    private const int DATA_BEGIN_ROW = 3;
    public static async UniTask<IEnumerable<TableInfo.DataTable>> LoadXlsAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<TableInfo.DataTable>();

        Logger.Instance.LogLine($"Excel 로드 = {path}");
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var dataTables = new List<TableInfo.DataTable>();
        await LoadWorkbookAsync(fs, dataTables);
        
        return dataTables;
    }

    private static async UniTask LoadWorkbookAsync(FileStream fs, List<TableInfo.DataTable> dataTables)
    {
        using var workbook = XlsxReader.OpenWorkbook(fs, false);
        foreach (var sheet in workbook.Worksheets)
        {
            var dataTable = await CreateDataTableAsync(sheet);
            if (dataTable == null) continue;
        
            // dataTable.PrintHeader();
            // dataTable.PrintData();
            dataTables.Add(dataTable);
        }
        
        // var workbook = new XLWorkbook(fs);   
        // foreach (var sheet in workbook.Worksheets)
        // {
        //     var sheetName = sheet.Name.Replace("_", string.Empty);
        //     Logger.Instance.LogLine();
        //     Logger.Instance.LogLine($" - {sheetName}");
        //     var range = sheet.RangeUsed();
        //     if(range == null)
        //         continue;
        //
        //     var dataTable = await CreateDataTableAsync(sheetName, range);
        //     if (dataTable == null) continue;
        //
        //     // dataTable.PrintHeader();
        //     // dataTable.PrintData();
        //     dataTables.Add(dataTable);
        // }
    }

    private static async Task<TableInfo.DataTable?> CreateDataTableAsync(Worksheet sheet)
    {
        var sheetName = sheet.Name.Replace("_", string.Empty);
        if (!Util.Util.IsValidName(sheetName))
            return null;

        Console.WriteLine($"READ TABLE = {sheetName}");

        var dic = sheet.ToDictionary(row => row.RowNumber, row => row.Cells);
        foreach (var key in dic.Keys)
        {
            Console.WriteLine($"[{key}] = {dic[key].Length}");
        }
        // var cells = sheet
        //     .Select(row => row.Cells)
        //     .Where(row => !row[0].CellValue?.StartsWith('#') ?? false)
        //     .ToArray();
        //
        // foreach (var cell in cells[0])
        // {
        //     Console.Write($"{cell.CellValue} ");
        // }
        // Console.WriteLine();
        // Console.WriteLine($"READ TABLE = {sheetName} ... {cells.Length} x {cells[0].Length} Done");

        // TableInfo.Header? header;
        // var rowIdx = 1;
        // foreach (var row in sheet)
        // {
        //     switch (rowIdx)
        //     {
        //         case HEADER_NAME_ROW:
        //             header = CreateHeaderRow(row);
        //             break;
        //         case SCHEMA_ROW:
        //             break;
        //         default:
        //             break;
        //     }
        //
        //     rowIdx++;
        // }

        await UniTask.CompletedTask;
        return null;
    }

    private static TableInfo.Header? CreateHeaderRow(Row range)
    {
        return null;
    }

    private static async UniTask<TableInfo.DataTable?> CreateDataTableAsync(string name, IXLRange range)
    {
        if (!Util.Util.IsValidName(name))
            return null;

        var header = CreateHeaderRow(range);
        if (header == null)
            return null;

        var validColumnIndices = header.SchemaCells.Select(cell => cell.Index);
        var result = new TableInfo.DataTable
        {
            Name = name,
            Header = header,
            Data = await CreateDataRowsAsync(header, range, validColumnIndices),
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

            var tokens = schemaText.Split('/').Select(token => token.Trim()).ToArray();
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
    private static bool IsPrimary(IEnumerable<string> tokens) => IsContains(Constant.Primary, tokens);
    private static bool IsPrimary(string token) => IsContains(Constant.Primary, token);
    private static bool IsContainer(IEnumerable<string> tokens) => IsArray(tokens) || IsList(tokens);
    private static bool IsContainer(string token) => IsArray(token) || IsList(token);
    private static bool IsArray(IEnumerable<string> tokens) => IsContains(SchemaTypes.Array, tokens);
    private static bool IsArray(string token) => IsContains(SchemaTypes.Array, token);
    private static bool IsList(IEnumerable<string> tokens) => IsContains(SchemaTypes.List, tokens);
    private static bool IsList(string token) => IsContains(SchemaTypes.List, token);
    private static bool IsEnumGet(IEnumerable<string> tokens) =>  IsContains(SchemaTypes.EnumGet, tokens) || IsContains(SchemaTypes.Enum, tokens);
    private static bool IsEnumGet(string token) => IsContains(SchemaTypes.EnumGet, token);
    private static bool IsContains(SchemaTypes schemaTypes, IEnumerable<string> tokens) => IsContains(schemaTypes.ToString(), tokens);
    private static bool IsContains(SchemaTypes schemaTypes, string token) => IsContains(schemaTypes.ToString(), token);
    private static bool IsContains(string schemaTypeStr, IEnumerable<string> tokens)
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
            IsPrimary = IsPrimary(tokens)
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
    private static bool TryGetPrimitive(IEnumerable<string> tokens, out Types type)
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
    private static string GetCustomDataTypeStr(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (SchemaExtension.IsSchema(token)) continue;
            if (TypesExtension.IsType(token)) continue;
            return token;
        }

        return string.Empty;
    }
    
    private static async UniTask<TableInfo.DataRow[]> CreateDataRowsAsync(TableInfo.Header header, IXLRange range, IEnumerable<int> validColumIndices)
    {
        var rows = new List<TableInfo.DataRow>();
        var cells = new List<TableInfo.DataCell>();
        var rangeRows = range.Rows(DATA_BEGIN_ROW, range.RowCount());
        var uniTaskRangeRows = rangeRows.ToUniTaskAsyncEnumerable();

        await foreach (var row in uniTaskRangeRows)
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