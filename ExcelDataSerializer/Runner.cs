using Cysharp.Threading.Tasks;
using ExcelDataSerializer.CodeGenerator;
using ExcelDataSerializer.DataExtractor;
using ExcelDataSerializer.ExcelLoader;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer;

public abstract class Runner
{
    public enum ExcelLoaderType
    {
        XlsxHelper,
        ClosedXml
    }
    public static void Execute(RunnerInfo info) => ExecuteAsync(info).Forget();
    public static async UniTask ExecuteAsync(RunnerInfo info)
    {
        if (!info.Validate())
        {
            throw new ArgumentException("Execute Failed. Invalid  runner info");
        }

        Logger.Instance.LogLine("실행 정보");
        Logger.Instance.LogLine($"> C# 저장 경로: {info.CSharpOutputDir}");
        Logger.Instance.LogLine($"> 데이터 저장 경로: {info.DataOutputDir}");
        Logger.Instance.LogLine($"> 총 {info.XlsxFiles.Count} 워크시트");
        Logger.Instance.LogLine($"> 로그 파일 : {Logger.Instance.LogPath}");
        
        Util.Util.DeleteFiles(info.DataOutputDir, ".meta");
        Util.Util.DeleteFiles(info.CSharpOutputDir, ".meta");
        
        // 엑셀 변환 준비 (Excel -> DataTable)
        var dataTables = await ExcelConvertAsync(info);
        
        // 데이터 클래스 생성 (DataTable -> C#)
        var classInfos = await GenerateDataClassMessagePackAsync(info.CSharpOutputDir, dataTables);
        
        // var classInfos = GenerateDataClassMemoryPack(info.OutputDir, dataTables);
        // GenerateDataClassRecord(info.CSharpOutputDir, info.DataOutputDir, dataTables);

        // 어셈블리 생성 및 테이블 클래스 인스턴스 생성
        await MessagePackExtractor.RunAsync(classInfos, dataTables, info);
        
        // MessagePackExtractor.Dispose();
        // var assemblyInfoMap = AssemblyHelper.CompileDataClassInfos(classInfos);
        // AssemblyHelper.PrintAssemblyInfos(assemblyInfoMap);

        // 엑셀파일에서 데이터 추출
        // AssemblyDataInjector.Test(dataTables, assemblyInfoMap);
    }

    private static async UniTask<Dictionary<string, TableInfo.DataTable>> ExcelConvertAsync(RunnerInfo info)
    {
        var result = new Dictionary<string, TableInfo.DataTable>();
        var loader = GetLoader(info.ExcelLoaderType);
        foreach (var filePath in info.XlsxFiles)
        {
            var dataTables = await Loader.LoadXlsAsync(filePath, loader);
            foreach (var table in dataTables)
            {
                if (result.ContainsKey(table.Name))
                {
                    if (table.Name == Constant.Enum)
                    {
                        var enumTable = result[table.Name];
                        if (TryMergeEnumSheet(enumTable, table, out var merged))
                            result[table.Name] = merged;
                    }
                    else
                        Logger.Instance.LogLine($"ExcelConvert : 이름 중복!!! {filePath} : {table.Name}");
                }
                else
                    result.Add(table.Name, table);
            }
        }

        return result;
    }

    private static bool TryMergeEnumSheet(TableInfo.DataTable left, TableInfo.DataTable right, out TableInfo.DataTable merged)
    {
        merged = default!;

        if (left.TableType != TableInfo.TableType.Enum || right.TableType != TableInfo.TableType.Enum)
            return false;
        if (left.Header == null || right.Header == null)
            return false;

        merged = new TableInfo.DataTable
        {
            Name = left.Name,
            TableType = TableInfo.TableType.Enum,
            Header = left.Header
        };

        var lastIndex = left.Header.SchemaCells
            .Select(cell => cell.Index)
            .Max(idx => idx) + 1;

        var schemaCells = right.Header.SchemaCells.Select(cell =>
        {
            cell.Index += lastIndex;
            return cell;
        });
        merged.Header.SchemaCells.AddRange(schemaCells);

        var dataCells = right.Data
            .Select(row =>
            {
                foreach (var t in row.DataCells)
                    t.Index += lastIndex;
                return row;
            });
        merged.Data = left.Data.Concat(dataCells).ToArray();
        return true;
    }
    private static ILoader GetLoader(ExcelLoaderType type) => type switch
    {
        ExcelLoaderType.ClosedXml => new ClosedXmlLoader(),
        _ => new XlsxHelperLoader()
    };

    private static async UniTask<DataClassInfo[]> GenerateDataClassMessagePackAsync(string outputDir, Dictionary<string, TableInfo.DataTable> dataTables)
    {
        var result = new List<DataClassInfo>();
        result.Add(RecordGenerator.GenerateInterface()!.Value);

        foreach (var kvp in dataTables)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var name = NamingRule.Check(key);
            var saveFilePath = Path.Combine(outputDir, $"{name}DataTable.cs");
            var classInfo = MessagePackGenerator.GenerateDataClass(value);
            if (!classInfo.HasValue)
                continue;

            result.Add(classInfo.Value);
            await Util.Util.SaveToFileAsync(saveFilePath, classInfo.Value.Code);
        }

        return result.ToArray();
    }

    private static DataClassInfo[] GenerateDataClassMemoryPack(string outputDir, Dictionary<string, TableInfo.DataTable> dataTables)
    {
        var result = new List<DataClassInfo>();
        foreach (var kvp in dataTables)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var saveFilePath = Path.Combine(outputDir, $"{key}Table.cs");
            var classInfo = MemoryPackGenerator.GenerateDataClass(value);
            result.Add(classInfo);
            Util.Util.SaveToFileAsync(saveFilePath, classInfo.Code).Forget();
        }

        return result.ToArray();
    }

    private static DataClassInfo[] GenerateDataClassRecord(string csOutputDir, string dataOutputDir, Dictionary<string, TableInfo.DataTable> dataTables)
    {
        var result = new List<DataClassInfo>();
        var enumTable = dataTables.Values.FirstOrDefault(t => t.TableType == TableInfo.TableType.Enum);
        RecordGenerator.SetEnumTable(enumTable);

        var interfaceInfo = RecordGenerator.GenerateInterface();
        if(interfaceInfo != null)
            Util.Util.SaveToFileAsync(Path.Combine(csOutputDir, $"{interfaceInfo.Value.Name}.cs"), interfaceInfo.Value.Code).Forget();
        
        foreach (var kvp in dataTables)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var name = NamingRule.Check(key);
            var saveFilePath = Path.Combine(csOutputDir, $"{name}DataTable.cs");
            var saveDataFilePath = Path.Combine(dataOutputDir, $"{key}DataTable.json");
            var classInfo = RecordGenerator.GenerateDataClass(value);
            if (classInfo == null)
                continue;
            
            result.Add(classInfo.Value);
            Util.Util.SaveToFileAsync(saveFilePath, classInfo.Value.Code).Forget();

            if (value.TableType == TableInfo.TableType.Enum) continue;

            var data = RecordGenerator.GenerateRecordData(value);
            Util.Util.SaveToFileAsync(saveDataFilePath, data).Forget();
            Logger.Instance.LogLine();
        }

        return result.ToArray();
    }
}