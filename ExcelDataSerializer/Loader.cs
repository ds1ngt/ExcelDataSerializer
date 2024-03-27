using System.Data;
using ClosedXML.Excel;

namespace ExcelDataSerializer;

public class Loader
{
    public void LoadXls(string path)
    {
        Console.WriteLine($"Load Excel = {path}");
        var workbook = new XLWorkbook(path);
        foreach (var sheet in workbook.Worksheets)
        {
            var sheetName = sheet.Name.Replace("_", string.Empty);
            Console.WriteLine($" - {sheetName}");
            var range = sheet.RangeUsed();
            if(range == null)
                continue;

            var dataTable = CreateDataTable(range);
            dataTable.PrintHeader();
            dataTable.PrintData();
        }
    }

    private Info.DataTable CreateDataTable(IXLRange range)
    {
        var headerRow = CreateHeaderRow(range.FirstRow());
        var validColumnIndices = headerRow.HeaderCells.Select(cell => cell.Index);
        var result = new Info.DataTable
        {
            Header = headerRow,
            Datas = CreateDataRows(range, validColumnIndices),
        };
        return result;
    }
    private Info.HeaderRow CreateHeaderRow(IXLRangeRow firstRow)
    {
        var cells = new List<Info.DataCell>();
        for (var i = 1; i < firstRow.CellCount(); ++i)
        {
            if (!firstRow.Cell(i).TryGetValue<string>(out var cellValue))
                continue;

            var value = Util.GetValidName(cellValue);
            if (!Util.IsValidName(value))
                continue;
            
            cells.Add(new Info.DataCell()
            {
                Index = i,
                Value = value,
            });
        }

        var result = new Info.HeaderRow
        {
            HeaderCells = cells.ToArray()
        };
        return result;
    }

    private Info.DataRow[] CreateDataRows(IXLRange range, IEnumerable<int> validColumIndices)
    {
        var rows = new List<Info.DataRow>();
        var cells = new List<Info.DataCell>();
        foreach (var row in range.Rows().Skip(1))
        {
            cells.Clear();
            foreach (var idx in validColumIndices)
            {
                if (!row.Cell(idx).TryGetValue<string>(out var value))
                    continue;

                cells.Add(new Info.DataCell
                {
                    Index = idx,
                    Value = value,
                });
            }
            rows.Add(new Info.DataRow
            {
                DataCells = cells.ToArray()
            });
        }
        return rows.ToArray();
    }
}