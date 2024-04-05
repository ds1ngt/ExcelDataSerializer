using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class MemoryPackGenerator
{
    public static DataClassInfo GenerateDataClass(TableInfo.DataTable dataTable)
    {
        var builder = CodeBuilder.NewBuilder();
        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            {
                builder = GenerateListClass(dataTable, builder);
            }
                break;
            case TableInfo.TableType.Dictionary:
            {
                builder = GenerateDictionaryClass(dataTable, builder);
            }
                break;
            case TableInfo.TableType.Enum:
            {
                builder = GenerateEnumClass(dataTable, builder);
            }
                break;
            default:
                break;
        }

        return new DataClassInfo
        {
            Name = dataTable.Name,
            TableType = dataTable.TableType,
            Code = builder.GenerateCode(),
        };
    }

    private static CodeBuilder GenerateListClass(TableInfo.DataTable dataTable, CodeBuilder builder)
    {
        // TODO : List 클래스 생성
        return builder;
    }

    private static CodeBuilder GenerateDictionaryClass(TableInfo.DataTable dataTable, CodeBuilder builder)
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
        
        builder.AddMember(dataCls);
        builder.AddMember(containerCls);
        return builder;
    }

    private static CodeBuilder GenerateEnumClass(TableInfo.DataTable dataTable, CodeBuilder builder)
    {
        // TODO : Enum 클래스 생성
        var header = dataTable.Header;
        if (header == null)
            return builder;

        var dataRows = dataTable.Data;
        foreach (var schema in header.SchemaCells)
        {
            var enumType = new CodeTypeDeclaration(schema.Name);
            var column = dataRows.SelectMany(r => r.DataCells.Where(cell => cell.Index == schema.Index));
            foreach (var cell in column)
            {
                if (cell.Index != schema.Index) continue;
                if (string.IsNullOrWhiteSpace(cell.Value))
                    continue;

                enumType.Members.Add(new CodeMemberField {Name = cell.Value});
            }
            enumType.IsEnum = true;
            builder.AddMember(enumType);
        }
        return builder;
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
        Logger.Instance.LogLine($"Table [{dataTable.Name}] saved. {savePath}");
        Util.Util.SaveToFile(savePath, sb.ToString());
    }
#endregion // Util
}