using System.CodeDom;

namespace ExcelDataSerializer.CodeGenerator;

public static class CodeDomExtensions
{
    public static void AddEnum(this CodeAttributeDeclaration attr, string type, string value)
    {
        attr.Arguments.Add(new CodeAttributeArgument
        {
            Value = new CodeFieldReferenceExpression
            {
                TargetObject =new CodeTypeReferenceExpression(type),
                FieldName = value,
            }
        });
    }
}