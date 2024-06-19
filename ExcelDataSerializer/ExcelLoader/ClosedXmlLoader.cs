using ClosedXML.Excel;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.ExcelLoader;

public class ClosedXmlLoader : ILoader
{
#region Xlsx
    public async UniTask LoadWorkbookAsync(FileStream fs, List<TableInfo.DataTable> dataTables)
    {
        var workbook = new XLWorkbook(fs);   
        foreach (var sheet in workbook.Worksheets)
        {
            var sheetName = sheet.Name.Replace("_", string.Empty);
            Logger.Instance.LogLine();
            Logger.Instance.LogLine($" - {sheetName}");
            var range = sheet.RangeUsed();
            if(range == null)
                continue;
        
            var dataTable = await CreateDataTableAsync(sheetName, range);
            if (dataTable == null) continue;
        
            // dataTable.PrintHeader();
            // dataTable.PrintData();
            dataTables.Add(dataTable);
        }
    }
#endregion // Xlsx

#region Sheet
    private static async UniTask<TableInfo.DataTable?> CreateDataTableAsync(string name, IXLRange range)
    {
        if (!Util.Util.IsValidName(name))
            return null;

        var header = CreateHeaderRow(range);
        if (header == null)
            return null;

        var validColumnIndices = header.SchemaCells.Select(cell => cell.Index).ToArray();
        var result = new TableInfo.DataTable
        {
            Name = name,
            ClassName = NamingRule.Check(name),
            Header = header,
            Data = await CreateDataRowsAsync(header, range, validColumnIndices),
            TableType = LoaderUtil.GetTableType(header),
        };
        return result;
    }
#endregion // Sheet

#region Header Row
    private static TableInfo.Header? CreateHeaderRow(IXLRange range)
    {
        var result = new TableInfo.Header();

        var headerNameRow = range.Row(LoaderConstant.HEADER_NAME_ROW);
        var schemaRow = range.Row(LoaderConstant.SCHEMA_ROW);
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
            var schemaInfo = LoaderUtil.ParseSchemaInfo(tokens);
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
#endregion // Header Row

#region Data Row
    private static async UniTask<TableInfo.DataRow[]> CreateDataRowsAsync(TableInfo.Header header, IXLRange range, int[] validColumIndices)
    {
        var rows = new List<TableInfo.DataRow>();
        var cells = new List<TableInfo.DataCell>();
        var rangeRows = range.Rows(LoaderConstant.DATA_BEGIN_ROW, range.RowCount());
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
            default:
                return value;
        }
    }
#endregion // Data Row
}