namespace ExcelDataSerializer.Model;

public enum SchemaTypes
{
    None,
    Primary,
    Primitive,
    Array,
    List,
    Dictionary,
    Enum,
    EnumGet,
    EnumSet,
    Custom,
}
public enum Types
{
    // Primitive Types
    Byte,
    Short,          // int 16
    Int16,          // 데이터 파싱에 사용
    UShort,
    UInt16,         // 데이터 파싱에 사용
    Int,            // int 32
    Int32,          // 데이터 파싱에 사용
    UInt,
    UInt32,         // 데이터 파싱에 사용
    Long,           // int 64
    Int64,          // 데이터 파싱에 사용
    ULong,
    UInt64,         // 데이터 파싱에 사용
    Float,
    Single,         // 데이터 파싱에 사용
    Double,
    Decimal,
    Bool,           // 데이터 파싱에 사용
    Boolean,
    String,
    
    // Unity Types
    Vector3,
    Quaternion,
}

public abstract record ContainerType
{
    public Types KeyType;
    public Types ValueType;
}

public static class SchemaExtension
{
    private static readonly Dictionary<string, SchemaTypes> _strTypesMap = new();

    static SchemaExtension()
    {
        var values = Enum.GetValues(typeof(SchemaTypes));
        foreach (SchemaTypes value in values)
            _strTypesMap.Add(value.ToString().ToLower(), value);
    }

    public static bool IsEqual(this SchemaTypes schemaTypes, string enumStr)
    {
        if (TryGetValue(enumStr, out var types))
            return schemaTypes == types;
        return false;
    }

    private static bool TryGetValue(string typeStr, out SchemaTypes types)
    {
        types = SchemaTypes.None;
        return _strTypesMap.TryGetValue(typeStr, out types);
    }

    public static bool IsSchema(string typeStr) => _strTypesMap.ContainsKey(typeStr.ToLower());
}

// public static class EnumExtension<TEnum> where TEnum : struct, Enum
// {
//     private static readonly Dictionary<string, TEnum> _strEnumMap = new();
//     static EnumExtension()
//     {
//         var values = Enum.GetValues<TEnum>();
//         foreach (var value in values)
//             _strEnumMap.Add(value.ToString().ToLower(), value);
//     }
//
//     public static bool IsEnum(string str) => _strEnumMap.ContainsKey(str);
// }
public static class TypesExtension
{
    private static readonly Dictionary<string, Types> _strTypesMap = new();

    static TypesExtension()
    {
        var values = Enum.GetValues(typeof(Types));
        foreach (Types value in values)
        {
            var typeValues = value.ToString().ToLower();
            _strTypesMap.Add(typeValues, value);
        }
    }
    public static string GetTypeStr(this Types types)
    {
        switch (types)
        {
            case Types.Byte: return "System.Byte";
            case Types.Short:
            case Types.Int16: 
                return "System.Int16";
            case Types.UShort:
            case Types.UInt16: 
                return "System.UInt16";
            case Types.Int:
            case Types.Int32: 
                return "System.Int32";
            case Types.UInt:
            case Types.UInt32: 
                return "System.UInt32";
            case Types.Long:
            case Types.Int64: 
                return "System.Int64";
            case Types.ULong:
            case Types.UInt64:
                return "System.UInt64";
            case Types.Single:
            case Types.Float: 
                return "System.Single";
            case Types.Double: return "System.Double";
            case Types.Decimal: return "System.Decimal";
            case Types.Bool:
            case Types.Boolean:
                return "System.Boolean";
            case Types.String: return "System.String";
            case Types.Vector3: return "UnityEngine.Vector3";
            case Types.Quaternion: return "UnityEngine.Quaternion";
            default:
                return string.Empty;
        }
    }

    public static string GetShortTypeStr(this Types types)
    {
        switch (types)
        {
            case Types.Byte: return "byte";
            case Types.Short:
            case Types.Int16:
                return "short";
            case Types.UShort:
            case Types.UInt16:
                return "ushort";
            case Types.Int:
            case Types.Int32:
                return "int";
            case Types.UInt:
            case Types.UInt32:
                return "uint";
            case Types.Long:
            case Types.Int64:
                return "long";
            case Types.ULong:
            case Types.UInt64:
                return "ulong";
            case Types.Float:
            case Types.Single:
                return "float";
            case Types.Double:
                return "double";
            case Types.Decimal:
                return "decimal";
            case Types.Bool:
            case Types.Boolean:
                return "bool";
            case Types.String:
                return "string";
            case Types.Vector3:
                return "UnityEngine.Vector3";
            case Types.Quaternion:
                return "UnityEngine.Quaternion";
            default:
                return string.Empty;
        }
    }

    public static bool TryGetValue(string typeStr, out Types types)
    {
        types = Types.Int;
        return _strTypesMap.TryGetValue(typeStr.ToLower(), out types);
    }

    public static bool IsType(string typeStr) => _strTypesMap.ContainsKey(typeStr.ToLower());
}