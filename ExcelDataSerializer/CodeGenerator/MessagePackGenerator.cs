using System.CodeDom;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class MessagePackGenerator
{
    #region Data Class

    public static DataClassInfo? GenerateDataClass(TableInfo.DataTable dataTable)
    {
        using var builder = CodeBuilder.NewBuilder(Constant.DATA_NAMESPACE);
        builder.Import("MessagePack");

        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            case TableInfo.TableType.Dictionary:
            {
                AddDataClass(builder, dataTable);
                AddTableClass(builder, dataTable);
            }
                break;
            case TableInfo.TableType.Enum:
            {
                AddEnumClass(builder, dataTable);
            }
                break;
        }

        return new DataClassInfo
        {
            Name = dataTable.Name,
            CsFileName = NamingRule.Check(dataTable.Name),
            TableType = dataTable.TableType,
            Code = builder.GenerateCode()
        };
    }
    private static void AddDataClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        // Data Class
        var name = NamingRule.Check(dataTable.Name);
        var cls = new CodeTypeDeclaration($"{name}{Constant.DATA_SUFFIX}");
        cls.CustomAttributes.Add(new CodeAttributeDeclaration("MessagePackObject"));
        cls.IsPartial = true;

        var keyIdx = 0;
        foreach (var cell in dataTable.Header!.SchemaCells)
        {
            var member = new CodeMemberField
            {
                Attributes = MemberAttributes.Public,
                Name = Util.Util.TrimUnderscore(cell.Name),
                Type = new CodeTypeReference(GetTypeStr(cell.SchemaTypes, cell.ValueType))
            };
            member.CustomAttributes.Add(GetKeyAttr(keyIdx));
            member.Name += " { get; set; } //";
            cls.Members.Add(member);
            keyIdx++;
        }
        builder.AddMember(cls);
    }

    private static CodeAttributeDeclaration GetKeyAttr(int idx)
    {
        return new CodeAttributeDeclaration
        {
            Name = "Key",
            Arguments = { new CodeAttributeArgument { Value = new CodePrimitiveExpression(idx) } }
        };
    }

    private static void AddTableClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        if(dataTable.TableType == TableInfo.TableType.Dictionary && !dataTable.Header!.HasPrimaryKey)
            return;

        var name = NamingRule.Check(dataTable.Name);
        var tableCls = new CodeTypeDeclaration($"{name}{Constant.DATA_TABLE_SUFFIX}");
        tableCls.CustomAttributes.Add(new CodeAttributeDeclaration("MessagePackObject"));
        tableCls.BaseTypes.Add(Constant.INTERFACE_NAME);
        tableCls.IsPartial = true;

        var keyType = string.Empty;
        if (dataTable.Header!.HasPrimaryKey)
        {
            var primaryCellIdx = dataTable.Header.SchemaCells.FindIndex(schema => schema.Index == dataTable.Header.PrimaryIndex);
            if (primaryCellIdx == -1)
                return;
            keyType = dataTable.Header.SchemaCells[primaryCellIdx].ValueType;
        }
    
        var member = new CodeMemberField
        {
            Attributes = MemberAttributes.Public,
            Name = Constant.DataTableMemberName,
            Type = new CodeTypeReference
            {
                BaseType = $"Dictionary<{keyType}, {name}{Constant.DATA_SUFFIX}>",
            }
        };
        member.CustomAttributes.Add(GetKeyAttr(0));

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
            Expression = new CodeFieldReferenceExpression { FieldName = $"MessagePackSerializer.Deserialize<{tableCls.Name}>(bytes)"},
        });
        getMethod.ReturnType = new CodeTypeReference(tableCls.Name);

        tableCls.Members.Add(member);
        tableCls.Members.Add(getMethod);
        builder.AddMember(tableCls);
    }

    private static void AddEnumClass(CodeBuilder builder, TableInfo.DataTable dataTable)
    {
        foreach (var schema in dataTable.Header!.SchemaCells)
        {
            var enumType = new CodeTypeDeclaration(schema.Name);
            var column = dataTable.Data.SelectMany(r => r.DataCells.Where(cell => cell.Index == schema.Index));
            foreach (var cell in column)
            {
                if (cell.Index != schema.Index) continue;
                var name = Util.Util.TrimUnderscore(cell.Value);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                enumType.Members.Add(new CodeMemberField {Name = name});
            }
            enumType.IsEnum = true;
            builder.AddMember(enumType);
        }
    }
    private static string GetTypeStr(SchemaTypes schemaTypes, string valueType) => schemaTypes switch
    {
        SchemaTypes.Array => $"{valueType}[]",
        SchemaTypes.List => $"List<{valueType}>",
        _ => valueType
    };
#endregion // Data Class
}