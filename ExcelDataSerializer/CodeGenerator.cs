using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace ExcelDataSerializer;

public abstract class CodeGenerator
{
    public static void GenerateDataClass(Info.DataTable dataTable)
    {
        var provider = CodeDomProvider.CreateProvider("CSharp");
        var compileUnit = new CodeCompileUnit();
        var ns = new CodeNamespace("com.haegin.billionaire.Data");

        ns.Imports.Add(new CodeNamespaceImport("MemoryPack"));

        compileUnit.Namespaces.Add(ns);

        var dataClass = new CodeTypeDeclaration(dataTable.Name);
        dataClass.IsPartial = true;
        dataClass.CustomAttributes.Add(new CodeAttributeDeclaration("MemoryPackable"));
        
        if (dataTable.Header != null)
        {
            foreach (var cell in dataTable.Header.HeaderCells)
            {
                dataClass.Members.Add(new CodeMemberField
                {
                    Name = cell.Value,
                    Attributes = MemberAttributes.Public,
                    Type = new CodeTypeReference
                    {
                        
                    },
                });
            }
        }
        ns.Types.Add(dataClass);

        var sb = new StringBuilder();
        provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(sb), new CodeGeneratorOptions());
        Console.WriteLine(sb.ToString());
    }
}