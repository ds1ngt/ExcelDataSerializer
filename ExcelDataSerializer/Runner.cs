using ExcelDataSerializer.CodeGenerator;
using ExcelDataSerializer.ExcelLoader;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer;

public abstract class Runner
{
    public static void Execute(RunnerInfo info)
    {
        if (!info.Validate())
        {
            throw new ArgumentException("Execute Failed. Invalid  runner info");
        }

        Console.WriteLine("Runner Info");
        Console.WriteLine($"Output Dir: {info.OutputDir}");
        Console.WriteLine($"Total {info.XlsxFiles.Count} worksheets");

        // 엑셀 변환 준비 (Excel -> DataTable)
        var dataTables = ExcelConvert(info);

        // 데이터 클래스 생성 (DataTable -> C#)
        GenerateDataClass(info.OutputDir, dataTables);

        // 엑셀파일에서 데이터 추출
        
        // 파일로 저장
    }

    private static IEnumerable<TableInfo.DataTable> ExcelConvert(RunnerInfo info)
    {
        var result = new List<TableInfo.DataTable>();
        foreach (var filePath in info.XlsxFiles)
        {
            var dataTables = Loader.LoadXls(filePath);
            result.AddRange(dataTables);
        }

        return result;
    }

    private static void GenerateDataClass(string outputDir, IEnumerable<TableInfo.DataTable> dataTables)
    {
        foreach (var dataTable in dataTables)
        {
            var saveFilePath = Path.Combine(outputDir, $"{dataTable.Name}Table.cs");
            var classText = MemoryPackGenerator.GenerateDataClass(dataTable);
            Util.SaveToFile(saveFilePath, classText);
        }
    }
}