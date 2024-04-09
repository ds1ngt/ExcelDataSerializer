using System.CodeDom;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

// Class
public partial class CodeBuilder
{
    public class ClassBuilder
    {
        private readonly string _className;
        private SchemaTypes _schemaType;
        public string? KeyType { get; set; } = null;
        public string? ValueType { get; set; } = null;

        private readonly List<CodeMemberField> _fields = new();
        private ClassBuilder(string name, SchemaTypes type)
        {
            _className = name;
            _schemaType = type;
        }
        public static ClassBuilder NewDataClass(string name, SchemaTypes type) => new(name, type);
        public static ClassBuilder NewEnumClass() => new(Constant.Enum, SchemaTypes.EnumGet);
        public void AddField(string name, string type, MemberAttributes attribute = MemberAttributes.Public)
        {
            var newField = new CodeMemberField
            {
                Name = name,
                Type = new CodeTypeReference
                {
                    BaseType = type
                },
                Attributes = attribute,
            };
            _fields.Add(newField);
        }

        public CodeTypeDeclaration Generate()
        {
            var cls = new CodeTypeDeclaration(_className);

            foreach (var field in _fields)
            {
                cls.Members.Add(field);
            }

            return cls;
        }

        private void SetClassAttribute(CodeTypeDeclaration cls)
        {
            var memoryPackableAttr = new CodeAttributeDeclaration("MemoryPackable");
            switch (_schemaType)
            {
                case SchemaTypes.Array:
                case SchemaTypes.List:
                case SchemaTypes.Dictionary:
                    memoryPackableAttr.AddEnum("GenerateType", "Collection");
                    break;
                case SchemaTypes.EnumGet:
                case SchemaTypes.EnumSet:
                case SchemaTypes.None:
                case SchemaTypes.Primitive:
                default:
                    break;
            }
            cls.CustomAttributes.Add(memoryPackableAttr);
        }
    }
}