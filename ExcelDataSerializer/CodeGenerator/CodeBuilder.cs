using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using MemoryPack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;

namespace ExcelDataSerializer.CodeGenerator;

public partial class CodeBuilder
{
    internal CodeNamespace _namespace = new("com.haegin.billionaire.Data");
    private static StringBuilder _sb = new();

    private CodeBuilder()
    {
        _namespace.Imports.Add(new CodeNamespaceImport("System"));
        _namespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
        _namespace.Imports.Add(new CodeNamespaceImport("MemoryPack"));
    }
    public static CodeBuilder NewBuilder()
    {
        var builder = new CodeBuilder();
        return builder;
    }
    public void AddClass(CodeTypeDeclaration cls) => _namespace.Types.Add(cls);
    public string GenerateCode()
    {
        var provider = CodeDomProvider.CreateProvider("CSharp");
        var compileUnit = new CodeCompileUnit();
        _sb.Clear();
        compileUnit.Namespaces.Add(_namespace);
        provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(_sb), new CodeGeneratorOptions());

        var syntaxTree = CSharpSyntaxTree.ParseText(_sb.ToString());
        var asmName = Path.GetRandomFileName();
        var refPaths = new[]
        {
            typeof(MemoryPackableAttribute).GetTypeInfo().Assembly.Location,
            typeof(System.Object).GetTypeInfo().Assembly.Location,
            Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location) ?? string.Empty, "System.Runtime.dll"),
            Path.Combine(Path.GetDirectoryName(typeof(System.Memory<>).GetTypeInfo().Assembly.Location) ?? string.Empty, "System.Memory.dll"),
        };
        
        MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
        var compilation = CSharpCompilation.Create(
            asmName,
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
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
                var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var instance = assembly.CreateInstance(type.Name);
                    if (instance != null)
                    {
                        Console.WriteLine($"Instance Created = {instance.GetType().Name}");
                    }
                    else
                        Console.WriteLine($"Instance Create Failed...{type.Name}");
                }
                // var asmType = assembly.GetType("TilePlacement");
                // var instance = assembly.CreateInstance("com.haegin.billionaire.Data.TilePlacement"); 
                // Console.WriteLine($"Create Instance TilePlacement ... {instance != null}");
                // foreach (var type in assembly.GetTypes())
                // {
                //     Console.WriteLine($">> TYPE {type.Name}");
                // }
                // var props = type.GetProperties();
                // foreach (var prop in props)
                // {
                //     Console.WriteLine($"{prop.Name}");
                // }
            }
        }
        return _sb.ToString();
    }
}

// public static class CodeBuilderExtension
// {
    // public static CodeTypeDeclaration NewContainerClass(this CodeBuilder builder, string name)
    // {
    //     var cls = new CodeTypeDeclaration(name);
    //     var memoryPackableAttr = new CodeAttributeDeclaration("MemoryPackable");
    //     memoryPackableAttr.AddEnum("GenerateType", "Collection");
    //
    //     cls.IsPartial = true;
    //     cls.CustomAttributes.Add(memoryPackableAttr);
    //     builder._namespace.Types.Add(cls);
    //     return cls;
    // }
// }