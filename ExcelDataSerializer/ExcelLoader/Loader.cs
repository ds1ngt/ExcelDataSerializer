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

            dataTable.PrintHeader();
            dataTable.PrintData();
            dataTables.Add(dataTable);
            // CodeGenerator.GenerateDataClass(dataTable);
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
            Datas = CreateDataRows(range, validColumnIndices),
        };
        return result;
    }
    private static TableInfo.Header CreateHeaderRow(IXLRangeRow firstRow)
    {
        var result = new TableInfo.Header();
        var cells = new List<TableInfo.SchemaCell>();
        var primaryIdx = -1;

        /* Schema 판별
         * 1. (Optional) Primary가 있으면 해당 컬럼은 Primary
         *  - 중복된 Primary 추가 차단
         * 2. (Optional) Array, List, Dictionary 가 있으면 컨테이너 타입
         * 3. (Required) Primitive 타입을 처리
         */
        for (var i = 1; i < firstRow.CellCount(); ++i)
        {
            var cell = firstRow.Cell(i);
            if (!IsValidHeaderCell(cell))
                continue;

            if (!cell.HasComment) continue;

            var comment = cell.GetComment().Text;
            if (string.IsNullOrWhiteSpace(comment))
                continue;

            var tokens = comment.Split('/');
            var isPrimary = IsPrimary(tokens);

            if (isPrimary)
            {
                if (primaryIdx != -1)
                    throw new ArgumentException($"Primary Key Duplicated...({primaryIdx}, {i})");
                primaryIdx = i;
            }

            if (IsContainer(tokens))
            {
                    
            }
            
            
            // cells.Add(new TableInfo.DataCell()
            // {
            //     Index = i,
            //     Value = value,
            // });
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
    private static bool IsPrimary(string[] tokens) => IsContains(SchemaTypes.Primary, tokens);
    private static bool IsContainer(string[] tokens) => IsArray(tokens) || IsList(tokens) || IsDictionary(tokens);
    private static bool IsArray(string[] tokens) => IsContains(SchemaTypes.Array, tokens);
    private static bool IsList(string[] tokens) => IsContains(SchemaTypes.List, tokens);
    private static bool IsDictionary(string[] tokens) => IsContains(SchemaTypes.Dictionary, tokens);
    private static bool IsEnum(string[] tokens) => IsContains(SchemaTypes.EnumGet, tokens);
    private static bool IsContains(SchemaTypes schemaTypes, string[] tokens) => tokens.Any(token => schemaTypes.IsEqual(token));

    private static SchemaInfo? ParseSchemaInfo(string[] tokens)
    {
        var info = new SchemaInfo
        {
            IsPrimary = IsPrimary(tokens), 
        };
        
        return info;
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
}