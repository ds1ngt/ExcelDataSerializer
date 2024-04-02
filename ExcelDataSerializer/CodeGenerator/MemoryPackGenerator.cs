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
        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            {
            }
                break;
            case TableInfo.TableType.Dictionary:
            {
                var dataCls = new CodeTypeDeclaration(dataTable.Name);
                var dataAttr = new CodeAttributeDeclaration("MemoryPackable");
                dataCls.IsPartial = true;
                dataCls.CustomAttributes.Add(dataAttr);
                foreach (var schema in dataTable.Header.SchemaCells)
                {
                    var member = new CodeMemberField()
                    {
                        Attributes = MemberAttributes.Public,
                        Name = schema.Name,
                        Type = new CodeTypeReference(schema.ValueType)
                    };
                    member.Name += " { get; set; } //";
                    dataCls.Members.Add(member);
                }
                
                var containerCls = new CodeTypeDeclaration($"{dataTable.Name}Table");
                var containerAttr = new CodeAttributeDeclaration("MemoryPackable");
                containerAttr.AddEnum("GenerateType", "Collection");

                containerCls.IsPartial = true;
                containerCls.CustomAttributes.Add(containerAttr);
                
                var cellIdx = dataTable.Header.SchemaCells.FindIndex(cell => cell.Index == dataTable.Header.PrimaryIndex);
                var keyType = dataTable.Header.SchemaCells[cellIdx].ValueType;
                containerCls.BaseTypes.Add($"Dictionary<{keyType}, {dataTable.Name}>");

                var getMethod = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    Name = "Get",
                };
                getMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "base64Str"));
                getMethod.Statements.Add(new CodeAssignStatement
                {
                    Left = new CodeVariableReferenceExpression { VariableName = "var bytes" },
                    Right = new CodeFieldReferenceExpression { FieldName = "System.Convert.FromBase64String(base64Str)" },
                });
                getMethod.Statements.Add(new CodeMethodReturnStatement
                { 
                    Expression = new CodeFieldReferenceExpression { FieldName = $"MemoryPackSerializer.Deserialize<{dataTable.Name}>(bytes)"},
                });
                getMethod.ReturnType = new CodeTypeReference(dataTable.Name);
                containerCls.Members.Add(getMethod);
                
                builder.AddClass(dataCls);
                builder.AddClass(containerCls);
            }
                break;
            case TableInfo.TableType.Enum:
            {
                
            }
                break;
            default:
                break;
        }

        return builder.GenerateCode();
        // var cls = builder.NewContainerClass(dataTable.Name);
        if (dataTable.Header != null)
        {
            var classBuilder = CodeBuilder.ClassBuilder.NewDataClass(dataTable.Name, SchemaTypes.Dictionary);
            classBuilder.ValueType = dataTable.Name;

            foreach (var cell in dataTable.Header.SchemaCells)
            {
                classBuilder.AddField(cell.ValueType, cell.GetTypeStr());
                // cls.Members.Add(new CodeMemberField
                // {
                //     Name = cell.Value,
                //     Attributes = MemberAttributes.Public,
                //     Type = new CodeTypeReference
                //     {
                //         BaseType = cell.GetTypeStr(),
                //     },
                // });
            }
            // var cells = dataTable.Header.SchemaCells;
            // foreach (var cell in cells)
            // {
            //     cls.Members.Add(new CodeMemberField
            //     {
            //         Name = cell.Value,
            //         Attributes = MemberAttributes.Public,
            //         Type = new CodeTypeReference(typeof(string)),
            //     });   
            // }
        }

        var code = builder.GenerateCode();
        // Console.WriteLine(code);
        return builder.GenerateCode();

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

    private static void FillDataClass(CodeTypeDeclaration cls, TableInfo.Header header)
    {
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