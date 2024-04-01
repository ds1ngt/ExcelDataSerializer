using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;

namespace ExcelDataSerializer;

public class CodeBuilder
{
    internal CodeNamespace _namespace = new("com.haegin.billionaire.Data");
    private static StringBuilder _sb = new();
    public static CodeBuilder NewBuilder()
    {
        var builder = new CodeBuilder();
        
        return builder;
    }

    public string Generate()
    {
        var provider = CodeDomProvider.CreateProvider("CSharp");
        var compileUnit = new CodeCompileUnit();
        _sb.Clear();
        compileUnit.Namespaces.Add(_namespace);
        provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(_sb), new CodeGeneratorOptions());
        return _sb.ToString();
    }
}

public static class CodeBuilderExtension
{
    public static CodeTypeDeclaration NewDataClass(this CodeBuilder builder, string name)
    {
        var cls = new CodeTypeDeclaration(name);
        cls.IsPartial = true;
        cls.CustomAttributes.Add(new CodeAttributeDeclaration
        {
            Name = "MemoryPackable",
            Arguments = { new CodeAttributeArgument {
                Name = "GenerateType.Collection",
            }}
        });
        builder._namespace.Types.Add(cls);
        return cls;
    }
}