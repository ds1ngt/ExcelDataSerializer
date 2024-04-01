using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class MemoryPackGenerator
{
    public static string GenerateDataClass(TableInfo.DataTable dataTable)
    {
        var builder = CodeBuilder.NewBuilder();
        var cls = builder.NewDataClass(dataTable.Name);
        if (dataTable.Header != null)
        {
            foreach (var cell in dataTable.Header.SchemaCells)
            {
                cls.Members.Add(new CodeMemberField
                {
                    Name = cell.Value,
                    Attributes = MemberAttributes.Public,
                    Type = new CodeTypeReference
                    {
                        BaseType = cell.GetTypeStr(),
                    },
                });
            }
            var cells = dataTable.Header.SchemaCells;
            foreach (var cell in cells)
            {
                cls.Members.Add(new CodeMemberField
                {
                    Name = cell.Value,
                    Attributes = MemberAttributes.Public,
                    Type = new CodeTypeReference(typeof(string)),
                });   
            }
        }
        Console.WriteLine(builder.Generate());
        return builder.Generate();

        // var provider = CodeDomProvider.CreateProvider("CSharp");
        // var compileUnit = new CodeCompileUnit();
        // var ns = new CodeNamespace("com.haegin.billionaire.Data");
        //
        // ns.Imports.Add(new CodeNamespaceImport("MemoryPack"));
        //
        // compileUnit.Namespaces.Add(ns);
        //
        // var dataClass = new CodeTypeDeclaration(dataTable.Name);
        // dataClass.IsPartial = true;
        // dataClass.CustomAttributes.Add(new CodeAttributeDeclaration("MemoryPackable"));
        //
        // if (dataTable.Header != null)
        // {
        //     foreach (var cell in dataTable.Header.HeaderCells)
        //     {
        //         dataClass.Members.Add(new CodeMemberField
        //         {
        //             Name = cell.Value,
        //             Attributes = MemberAttributes.Public,
        //             Type = new CodeTypeReference
        //             {
        //                 
        //             },
        //         });
        //     }
        // }
        // ns.Types.Add(dataClass);
        //
        // var sb = new StringBuilder();
        // provider.GenerateCodeFromCompileUnit(compileUnit, new StringWriter(sb), new CodeGeneratorOptions());
        // // Console.WriteLine(sb.ToString());
        // SaveTable(dataTable, sb);
    }

    private static CodeNamespace CreateDataTableClass()
    {
        var ns = CreateNew();
        var dataTableClass = new CodeTypeDeclaration("DataTable")
        {
            Attributes = MemberAttributes.Static,
        };

        dataTableClass.Members.Add(new CodeMemberMethod
        {
            Name = "Serialize",
            Attributes = MemberAttributes.Static,
            ReturnType = new CodeTypeReference(typeof(string)),
        });

        ns.Types.Add(dataTableClass);
        return ns;
    }

#region CodeDom

    private static CodeNamespace CreateNew()
    {
        var provider = CodeDomProvider.CreateProvider("CSharp");
        var compileUnit = new CodeCompileUnit();
        var ns = new CodeNamespace("com.haegin.billionaire.Data");
        return ns;
    }

#endregion // CodeDom
#region Util
    private static void SaveTable(TableInfo.DataTable dataTable, StringBuilder sb)
    {
        var savePath = Path.Combine(Path.GetTempPath(), "ExcelDataSerializer", $"{dataTable.Name}.cs");
        Console.WriteLine($"Table [{dataTable.Name}] saved. {savePath}");
        Util.SaveToFile(savePath, sb.ToString());
    }
#endregion // Util
}