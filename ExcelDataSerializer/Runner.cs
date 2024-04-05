using ExcelDataSerializer.CodeGenerator;
using ExcelDataSerializer.DataExtractor;
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

        Logger.Instance.LogLine("Runner Info");
        Logger.Instance.LogLine($"Output Dir: {info.OutputDir}");
        Logger.Instance.LogLine($"Total {info.XlsxFiles.Count} worksheets");

        // 엑셀 변환 준비 (Excel -> DataTable)
        var dataTables = ExcelConvert(info);
        
        // 데이터 클래스 생성 (DataTable -> C#)
        var classInfos = GenerateDataClass(info.OutputDir, dataTables);
        
        // 어셈블리 생성 및 테이블 클래스 인스턴스 생성
        var assemblyInfoMap = AssemblyHelper.CompileDataClassInfos(classInfos);
        AssemblyHelper.PrintAssemblyInfos(assemblyInfoMap);

        // 엑셀파일에서 데이터 추출
        AssemblyDataInjector.Test(dataTables, assemblyInfoMap);

        // 파일로 저장
    }

    private static Dictionary<string, TableInfo.DataTable> ExcelConvert(RunnerInfo info)
    {
        var result = new Dictionary<string, TableInfo.DataTable>();
        foreach (var filePath in info.XlsxFiles)
        {
            var dataTables = Loader.LoadXls(filePath);
            foreach (var table in dataTables)
            {
                if (!result.TryAdd(table.Name, table))
                    Logger.Instance.LogLine($"ExcelConvert : Name Duplicate!!! {filePath} : {table.Name}");
            }
        }

        return result;
    }

    private static DataClassInfo[] GenerateDataClass(string outputDir, Dictionary<string, TableInfo.DataTable> dataTables)
    {
        var result = new List<DataClassInfo>();
        foreach (var (key, value) in dataTables)
        {
            var saveFilePath = Path.Combine(outputDir, $"{key}Table.cs");
            var classInfo = MemoryPackGenerator.GenerateDataClass(value);
            result.Add(classInfo);
            Util.Util.SaveToFile(saveFilePath, classInfo.Code);
        }

        return result.ToArray();
    }
}