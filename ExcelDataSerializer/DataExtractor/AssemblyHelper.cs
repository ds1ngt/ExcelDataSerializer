using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using MemoryPack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExcelDataSerializer.DataExtractor;

public abstract class AssemblyHelper
{
    public static Dictionary<string, CodeAssemblyInfo> CompileDataClassInfos(params DataClassInfo[] infos)
    {
        var result = new Dictionary<string, CodeAssemblyInfo>();
        foreach (var info in infos)
        {
            if (!TryCompileCode(info.Code, out var assembly)) continue;
            var instanceMap = CreateInstanceInAssembly(assembly);
            var assemblyInfo = new CodeAssemblyInfo
            {
                Name = info.Name,
                Assembly = assembly,
                TypeInstanceMap = instanceMap,
            };
            if (!result.TryAdd(assemblyInfo.Name, assemblyInfo))
                Logger.Instance.LogLine($"CompileDataClassInfos : Type Duplicate!!! {assemblyInfo.Name}");
        }

        return result;
    }
    private static bool TryCompileCode(string code, out Assembly assembly)
    {
        assembly = default!;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var asmName = Path.GetRandomFileName();
        var refPaths = new[]
        {
            typeof(MemoryPackableAttribute).GetTypeInfo().Assembly.Location,
            typeof(Object).GetTypeInfo().Assembly.Location,
            Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location) ?? string.Empty, "System.Runtime.dll"),
            Path.Combine(Path.GetDirectoryName(typeof(Memory<>).GetTypeInfo().Assembly.Location) ?? string.Empty, "System.Memory.dll"),
        };

        MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
        var compilation = CSharpCompilation.Create(
            asmName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        foreach (var type in Assembly.GetEntryAssembly().GetTypes())
        {
            Logger.Instance.LogLine($"TryCompileCode Entry Assembly Type = {type}");
        }
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
            foreach (var diagnostic in failures)
            {
                Console.Error.WriteLine($"t{diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }
        else
        {
            ms.Seek(0, SeekOrigin.Begin);
            assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            return true;
        }

        return false;
    }

    private static Dictionary<string, dynamic> CreateInstanceInAssembly(Assembly assembly)
    {
        var result = new Dictionary<string, dynamic>();
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            if (string.IsNullOrWhiteSpace(type.FullName))
                continue;
            
            var instance = assembly.CreateInstance(type.FullName);
            if (instance == null)
                continue;

            if (!result.TryAdd(type.Name, instance))
                Logger.Instance.LogLine($"CreateInstance : Type Duplicate!!! {type.Name}");
        }
        return result;
    }
    public static void PrintAssemblyInfos(Dictionary<string, CodeAssemblyInfo> assemblyInfoMap)
    {
        Logger.Instance.LogLine($"------------ Compiled Assembly Info ({assemblyInfoMap.Keys.Count}) ------------");
        var keys = assemblyInfoMap.Keys;
        var idx = 0;
        foreach (var key in keys)
        {
            var info = assemblyInfoMap[key];
            var instanceKeys = info.TypeInstanceMap.Keys;
            var typeStr = instanceKeys.Count == 0 ? string.Empty : instanceKeys.Aggregate((l, r) => $"{l}, {r}");
                
            Logger.Instance.LogLine($"[{idx}] Assembly: {info.Name},  Types: {typeStr}");
            idx++;
        }
    }
}