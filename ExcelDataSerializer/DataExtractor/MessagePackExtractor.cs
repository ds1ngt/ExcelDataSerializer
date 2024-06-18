using System.Reflection;
using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.DataExtractor;

public abstract class MessagePackExtractor
{
    private const string PROJECT_DIR = "Mpc";
    private const string CSPROJ_FILE = "Mpc.csproj";
    private const string OUTPUT_FILE = "MessagePackGenerated.cs";
    private const string BUILD_DIR = "Build";

    private static readonly string _assemblyFile = Path.Combine(Directory.GetCurrentDirectory(), PROJECT_DIR, BUILD_DIR, $"{PROJECT_DIR}.dll");
    private static readonly string _mpcProj = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""MessagePack"" Version=""3.0.54-alpha"" />
  </ItemGroup>

</Project>
";

#region Public Methods
    public static async UniTask RunAsync(DataClassInfo[] classInfos)
    {
        var assembly = await CompileAsync(classInfos);
        await DataExtractAsync(assembly);
    }
#endregion // Public Methods

#region Compile
    private static async UniTask<Assembly> CompileAsync(DataClassInfo[] classInfos)
    {
        var mpcInstalled = await IsMpcInstalledAsync();
        if (!mpcInstalled)
            await InstallMpcAsync();

        await CreateProjectAsync(PROJECT_DIR, CSPROJ_FILE);
        await CopyCsFilesAsync(PROJECT_DIR, classInfos);
        await RunMpcAsync(PROJECT_DIR);
        await BuildCsProj(PROJECT_DIR, BUILD_DIR);

        var asm = Assembly.LoadFile(_assemblyFile);
        return asm;
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

    private static async UniTask CreateProjectAsync(string projectDir, string projectFile)
    {
        if (Directory.Exists(projectDir))
            Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        await File.WriteAllTextAsync(Path.Combine(projectDir, projectFile),_mpcProj);
    }

    private static async UniTask CopyCsFilesAsync(string projectDir, DataClassInfo[] classInfos)
    {
        foreach (var info in classInfos.DistinctBy(info => info.CsFileName))
        {
            Console.WriteLine($"CopyCsFiles = {info.CsFileName} [{info.Name}]");
            var filePath = Path.Combine(projectDir, $"{info.CsFileName}.cs");
            await File.WriteAllTextAsync(filePath, info.Code);
        }
    }

    private static async UniTask RunMpcAsync(string projectDir)
    {
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "mpc",
            Argument = $"-i {projectDir} -o {Path.Combine(projectDir, OUTPUT_FILE)}",
            OutputDataReceived = OnOutput,
        };

        request.Print();
        await ProcessUtil.RunAsync(request);
        
        return;
        
        void OnOutput(string msg)
        {
            // Console.WriteLine($"[Run MPC]: {msg}");
        }
    }

    private static async UniTask BuildCsProj(string projectDir, string buildDir)
    {
        var request = new ProcessUtil.RequestInfo
        {
            Exec = "dotnet",
            Argument = $"build -o {buildDir}",
            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), projectDir),
        };
        
        request.Print();
        await ProcessUtil.RunAsync(request);
    }
#endregion // Compile

#region Data Extract
    private static async UniTask DataExtractAsync(Assembly assembly)
    {
        
    }
#endregion // Data Extract
}