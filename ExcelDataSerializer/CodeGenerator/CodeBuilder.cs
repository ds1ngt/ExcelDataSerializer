using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace ExcelDataSerializer.CodeGenerator;

public partial class CodeBuilder
{
    private readonly CodeDomProvider _provider;
    private readonly CodeNamespace _namespace = new("com.haegin.billionaire.Data");
    private static readonly StringBuilder _sb = new();

    private CodeBuilder()
    {
        _provider = CodeDomProvider.CreateProvider("CSharp");
        _namespace.Imports.Add(new CodeNamespaceImport("System"));
        _namespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
        _namespace.Imports.Add(new CodeNamespaceImport("MemoryPack"));
    }
    public static CodeBuilder NewBuilder()
    {
        var builder = new CodeBuilder();
        return builder;
    }
    public void AddMember(CodeTypeDeclaration cls) => _namespace.Types.Add(cls);
    public string GenerateCode()
    {
        var compileUnit = new CodeCompileUnit();
        _sb.Clear();
        compileUnit.Namespaces.Add(_namespace);
        _provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(_sb), new CodeGeneratorOptions());
        return _sb.ToString();
    }
}