namespace ExcelDataSerializer.Model;

public enum SchemaTypes
{
    None,
    Primary,
    Array,
    List,
    Dictionary,
    EnumGet,
    EnumSet,
}
public enum Types
{
    // Primitive Types
    Byte,
    Short,          // int 16
    UShort,
    Int,            // int 32
    UInt,
    Long,           // int 64
    ULong,
    Float,
    Double,
    Decimal,
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

    public static bool IsEqual(this SchemaTypes schemaTypes, string enumStr)
    {
        if (TryGetValue(enumStr, out var types))
            return schemaTypes == types;
        return false;
    }

    private static bool TryGetValue(string typeStr, out SchemaTypes types)
    {
        types = SchemaTypes.Primary;
        return _strTypesMap.TryGetValue(typeStr, out types);
    }
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
        var values = Enum.GetValues<Types>();
        foreach (var value in values)
            _strTypesMap.Add(value.ToString().ToLower(), value);
    }
    public static string GetTypeStr(this Types types)
    {
        switch (types)
        {
            case Types.Byte: return "byte";
            case Types.Short: return "short";
            case Types.UShort: return "ushort";
            case Types.Int: return "int";
            case Types.UInt: return "uint";
            case Types.Long: return "long";
            case Types.ULong: return "ulong";
            case Types.Float: return "float";
            case Types.Double: return "double";
            case Types.Decimal: return "decimal";
            case Types.Boolean: return "bool";
            case Types.String: return "string";
            case Types.Vector3: return "UnityEngine.Vector3";
            case Types.Quaternion: return "UnityEngine.Quaternion";
            default:
                return string.Empty;
        }
    }

    public static bool TryGetValue(string typeStr, out Types types)
    {
        types = Types.Int;
        return _strTypesMap.TryGetValue(typeStr, out types);
    }
}