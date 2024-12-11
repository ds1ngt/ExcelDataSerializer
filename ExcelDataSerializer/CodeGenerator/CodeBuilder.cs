using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;

namespace ExcelDataSerializer.CodeGenerator;

public partial class CodeBuilder : IDisposable
{
    private readonly CodeDomProvider _provider;
    private readonly CodeNamespace _namespace;
    private static readonly StringBuilder _sb = new();

    private CodeBuilder(string ns)
    {
        _provider = CodeDomProvider.CreateProvider("CSharp");
        _namespace = new(ns);
        _namespace.Imports.Add(new CodeNamespaceImport("System"));
        _namespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
    }
    public static CodeBuilder NewBuilder(string ns = "com.haegin.billionaire.Data")
    {
        var builder = new CodeBuilder(ns);
        return builder;
    }

    public void Import(string ns)
    {
        _namespace.Imports.Add(new CodeNamespaceImport(ns));
    }
    public void AddMember(CodeTypeDeclaration? cls)
    {
        if (cls == null)
            return;

        _namespace.Types.Add(cls);
    }

    public string GenerateCode()
    {
        var compileUnit = new CodeCompileUnit();
        _sb.Clear();
        compileUnit.Namespaces.Add(_namespace);
        _provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(_sb), new CodeGeneratorOptions());
        return _sb.ToString();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}