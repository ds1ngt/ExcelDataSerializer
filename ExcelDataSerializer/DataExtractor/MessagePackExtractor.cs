using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using Newtonsoft.Json;

namespace ExcelDataSerializer.DataExtractor;

public abstract partial class MessagePackExtractor
{
#region Fields
    private const string PROJECT_DIR = "Mpc";
    private const string BUILD_DIR = "Build";
    private const string DATA_DIR = "Data";
    private const string MESSAGEPACK_GENERATED_FILE = "MessagePackGenerated.cs";
    private static readonly string _mpcGeneratedFilePath = Path.Combine(PROJECT_DIR, MESSAGEPACK_GENERATED_FILE);
#endregion // Fields

#region Public Methods
    public static async UniTask RunAsync(DataClassInfo[] classInfos, Dictionary<string, TableInfo.DataTable> dataTables, RunnerInfo info)
    {
        await CompileAsync(classInfos);
        await ExportDataAsync(dataTables);
        await BuildCsProj(PROJECT_DIR, BUILD_DIR);
        await ExtractMessagePackDataAsync(PROJECT_DIR);
        await CopyOutputFilesAsync(PROJECT_DIR, info);
        
        Logger.Instance.LogLine($"Done.");
    }
#endregion // Public Methods

#region Compile
    private static async UniTask CompileAsync(DataClassInfo[] classInfos)
    {
        var mpcInstalled = await IsMpcInstalledAsync();
        if (!mpcInstalled)
            await InstallMpcAsync();

        await CreateProjectAsync(PROJECT_DIR);
        await CopyCsFilesAsync(PROJECT_DIR, classInfos);
        await RunMpcAsync(PROJECT_DIR);
    }
    private static async UniTask<bool> IsMpcInstalledAsync()
    {
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "dotnet",
            Argument = "tool list --global",
            OutputDataReceived = OnOutput,
        };

        var isInstalled = false;
        await ProcessUtil.RunAsync(request);
        return isInstalled;

        void OnOutput(string msg)
        {
            if (msg.Contains("messagepack.generator"))
                isInstalled = true;
        }
    }

    private static async UniTask InstallMpcAsync()
    {
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "dotnet",
            Argument = "tool install --global MessagePack.Generator"
        };

        await ProcessUtil.RunAsync(request);
    }

    private static async UniTask CreateProjectAsync(string projectDir)
    {
        if (Directory.Exists(projectDir))
            Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        foreach (var (fileName, code) in _fileMap)
        {
            await File.WriteAllTextAsync(Path.Combine(projectDir, fileName), code); 
        }
    }

    private static async UniTask CopyCsFilesAsync(string projectDir, DataClassInfo[] classInfos)
    {
        foreach (var info in classInfos.DistinctBy(info => info.CsFileName))
        {
            Logger.Instance.LogLine($"CopyCsFiles = {info.CsFileName} [{info.Name}]");
            var filePath = Path.Combine(projectDir, $"{info.CsFileName}.cs");
            await File.WriteAllTextAsync(filePath, info.Code);
        }
    }

    private static async UniTask RunMpcAsync(string projectDir)
    {
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "mpc",
            Argument = $"-i {projectDir} -o {_mpcGeneratedFilePath}",
        };

        request.Print();
        await ProcessUtil.RunAsync(request);
    }

    private static async UniTask BuildCsProj(string projectDir, string buildDir)
    {
        var workingDir = Path.Combine(Directory.GetCurrentDirectory(), projectDir);
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "dotnet",
            Argument = $"build -o {buildDir}",
            WorkingDirectory = workingDir,
        };
        
        request.Print();
        await ProcessUtil.RunAsync(request);
    }
#endregion // Compile

#region Export Data
    private static async UniTask ExportDataAsync(Dictionary<string, TableInfo.DataTable> dataTables)
    {
        var serialized = JsonConvert.SerializeObject(dataTables.Values.ToArray());
        var filePath = Path.Combine(PROJECT_DIR, "Data.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
        Logger.Instance.LogLine($"Export Data => {filePath}");
        await File.WriteAllTextAsync(filePath, serialized);
    }
#endregion // Export Data

#region Extract MessagePack Data
    private static async UniTask ExtractMessagePackDataAsync(string projectDir)
    {
        var workingDir = Path.Combine(Directory.GetCurrentDirectory(), projectDir);
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "dotnet",
            Argument = "run",
            WorkingDirectory = workingDir,
            OutputDataReceived = msg => Logger.Instance.LogLine($"[Extract] {msg}"),
        };
        
        request.Print();
        await ProcessUtil.RunAsync(request);
    }
#endregion // Extract MessagePack Data

#region Copy Output Files
    private static async UniTask CopyOutputFilesAsync(string projectDir, RunnerInfo info)
    {
        if (!Directory.Exists(info.DataOutputDir))
            Directory.CreateDirectory(info.DataOutputDir);

        var dataPath = Path.Combine(projectDir, DATA_DIR);
        if (!Directory.Exists(dataPath))
            return;
        
        CopyGeneratedCode(info);
        CopyDataFiles(dataPath, info.DataOutputDir);
    }

    private static void CopyGeneratedCode(RunnerInfo info)
    {
        var from = _mpcGeneratedFilePath;
        var to = Path.Combine(info.CSharpOutputDir, MESSAGEPACK_GENERATED_FILE);

        if (!File.Exists(from))
            return;

        File.Copy(from, to, true);
    }
    private static void CopyDataFiles(string dataPath, string dataOutputDir)
    {
        var dataFiles = Directory.GetFiles(dataPath);
        foreach (var dataFile in dataFiles)
        {
            var fileName = Path.GetFileName(dataFile);
            File.Copy(dataFile, Path.Combine(dataOutputDir, fileName), true);
        }
    }
#endregion // Copy Output Files
}