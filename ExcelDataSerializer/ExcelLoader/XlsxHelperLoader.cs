using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using XlsxHelper;

namespace ExcelDataSerializer.ExcelLoader;

public class XlsxHelperLoader : ILoader
{
    private const int NAME_ROW_IDX = 0;
    private const int SCHEMA_ROW_IDX = 1;
    private const int TARGET_ROW_IDX = 2;
    private const int VALID_ROW_LENGTH = 3;
    private const char TARGET_CLIENT = 'C';
    
    /// <summary>
    /// 워크북 로드
    /// </summary>
    /// <param name="fs">파일 스트림</param>
    /// <param name="dataTables">테이블 리스트</param>
    /// <returns>False이면 즉시 중단할 것</returns>
    public async UniTask<bool> LoadWorkbookAsync(FileStream fs, List<TableInfo.DataTable> dataTables)
    {
        using var workbook = XlsxReader.OpenWorkbook(fs, false);
        foreach (var sheet in workbook.Worksheets)
        {
            var (dataTable, isError) = await CreateDataTableAsync(sheet);
            if (dataTable == null)
            {
                if (!isError)
                    return true;

                Logger.Instance.LogErrorLine($"테이블 파싱 오류 : {sheet.Name}");
                return false;
            }
        
            // dataTable.PrintHeader();
            // dataTable.PrintData();
            dataTables.Add(dataTable);
        }

        return true;
    }
    
    /// <summary>
    /// 워크시트 파싱 및 DataTable 타입으로 변환
    /// </summary>
    /// <param name="sheet">변환된 데이터</param>
    /// <returns>True: 파싱에러, False: 성공 또는 필터링된 시트</returns>
    private static async Task<(TableInfo.DataTable?, bool)> CreateDataTableAsync(Worksheet sheet)
    {
        var sheetName = Util.Util.TrimInvalidChar(sheet.Name);
        if (!Util.Util.IsValidName(sheetName))
            return (null, false);

        Logger.Instance.LogLine();
        Logger.Instance.LogLine($" - {sheetName}");

        var rows = sheet.ToArray();
        if (rows.Length < VALID_ROW_LENGTH)
            return (null, false);;

        var header = CreateHeaderRow(rows[NAME_ROW_IDX], rows[SCHEMA_ROW_IDX], rows[TARGET_ROW_IDX]);
        if (header == null)
            return (null, true);;

        var validColumnIndices = header.SchemaCells
            .Select(cell => cell.Index)
            .ToArray();
        var validColumnNames = GetValidColumnNames(rows[NAME_ROW_IDX]);

        var result = new TableInfo.DataTable
        {
            Name = sheetName,
            ClassName = NamingRule.Check(sheetName),
            Header = header,
            Data = await CreateDataRowsAsync(header, rows, validColumnIndices, validColumnNames),
            TableType = LoaderUtil.GetTableType(header),
        };
        return (result, false);
    }

    private static string[] GetValidColumnNames(Row row)
    {
        var result = row
            .Where(cell => !string.IsNullOrWhiteSpace(cell.CellValue) && Util.Util.IsValidName(cell.CellValue))
            .Select(cell => cell.ColumnName.ToString())
            .ToArray();
        return result;
    }
#region Header Row
    private static TableInfo.Header? CreateHeaderRow(Row headerRow, Row schemaRow, Row targetRow)
    {
        var result = new TableInfo.Header();
        var validTargetRow = targetRow
            .Where(FilterTargetRow)
            .ToArray();

        var validHeaderRow = headerRow
            .Where(cell => FilterHeaderRow(validTargetRow, cell))
            .ToArray();

        var validSchemaRow = schemaRow
            .Where(cell => FilterSchemaRow(validHeaderRow, cell))
            .ToArray();
        
        if (validHeaderRow.Length != validSchemaRow.Length)
            return null;

        var len = validHeaderRow.Length;
        for (var i = 0; i < len; ++i)
        {
            var name = validHeaderRow[i].CellValue;
            var schema = validSchemaRow[i];
            if (name == null || schema.CellValue == null)
                continue;

            if (!IsValidHeaderCell(name))
                continue;

            if (string.IsNullOrWhiteSpace(schema.CellValue))
                continue;

            var tokens = schema.CellValue
                .Split('/')
                .Select(token => token.Trim())
                .ToArray();

            var index = LoaderUtil.SheetColumnToIdx(validHeaderRow[i].ColumnName.ToString());
            var schemaInfo = LoaderUtil.ParseSchemaInfo(tokens);
            if (schemaInfo.IsPrimary)
            {
                if (result.PrimaryIndex != null)
                {
                    Logger.Instance.LogErrorLine($"Primary Key Duplicated...({result.PrimaryIndex}, {i})");
                    return null;
                    // throw new ArgumentException($"Primary Key Duplicated...({result.PrimaryIndex}, {i})");
                }

                result.PrimaryIndex = index;
            }

            name = Util.Util.GetValidName(name);
            name = Util.Util.IsValidName(name) ? name: string.Empty;

            var schemaCell = new TableInfo.SchemaCell
            {
                Index = index,
                Name = name,
                SchemaTypes = schemaInfo.SchemaType,
                ValueType = Util.Util.TrimUnderscore(schemaInfo.DataType),
            };
            result.SchemaCells.Add(schemaCell);
            Logger.Instance.LogLine($"{schemaCell.Name} [ {schemaCell.Index} ] {schemaCell.SchemaTypes} / {schemaCell.ValueType}");
        }
        Logger.Instance.LogLine();
        return result;
    }

    private static bool FilterTargetRow(Cell cell)
    {
        return !string.IsNullOrWhiteSpace(cell.CellValue) && cell.CellValue.ToUpper().Contains(TARGET_CLIENT);
    }

    private static bool FilterHeaderRow(Cell[] validTargetRow, Cell cell)
    {
        if (!IsValidTargetCell(validTargetRow, cell))
            return false;
        
        return !string.IsNullOrWhiteSpace(cell.CellValue) && Util.Util.IsValidName(cell.CellValue);
    }

    private static bool FilterSchemaRow(Cell[] validHeaderRow, Cell cell)
    {
        return Array.FindIndex(validHeaderRow, headerCell => headerCell.ColumnName == cell.ColumnName) != -1;
    }

    private static bool IsValidTargetCell(Cell[] targetRow, Cell cell)
    {
        var idx = Array.FindIndex(targetRow, targetCell => targetCell.ColumnName == cell.ColumnName);
        return idx != -1;
    }
    private static bool IsValidHeaderCell(string name)
    {
        var validName = Util.Util.GetValidName(name);
        return Util.Util.IsValidName(validName);
    }
#endregion // Header Row

#region Data Row

    private static async UniTask<TableInfo.DataRow[]> CreateDataRowsAsync(TableInfo.Header header, Row[] tableRows, int[] validColumnIndices, string[] validColumNames)
    {
        var rows = new List<TableInfo.DataRow>();
        var cells = new List<TableInfo.DataCell>();
        
        for (var i = LoaderConstant.DATA_BEGIN_ROW - 1; i < tableRows.Length; i++)
        {
            cells.Clear();

            var row = tableRows[i];
            var length = validColumnIndices.Length;
            for (var j = 0; j < length; ++j)
            {
                var idx = validColumnIndices[j];
                var columnName = validColumNames[j];
                var value = string.Empty;
                var rowIdx = Array.FindIndex(row.Cells, cell => cell.ColumnName == columnName);
                if(rowIdx != -1)
                    value = row.Cells[rowIdx].CellValue ?? string.Empty;

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
            case SchemaTypes.Primitive:
            {
                if (header.SchemaCells[schemaIndex].ValueType.Contains("Bool"))
                {
                    value = value switch
                    {
                        "1" => "True",
                        "0" => "False",
                        _ => value
                    };
                }
                return value;
            }
                break;
            default:
                return value;
        }
    }
#endregion // Data Row
}