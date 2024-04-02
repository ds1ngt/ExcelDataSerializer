namespace ExcelDataSerializer.Model;

public enum SchemaTypes
{
    None,
    Primitive,
    Array,
    List,
    Dictionary,
    EnumGet,
    EnumSet,
    Custom,
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

    static SchemaExtension()
    {
        var values = Enum.GetValues<SchemaTypes>();
        foreach (var value in values)
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
        var values = Enum.GetValues<Types>();
        foreach (var value in values)
            _strTypesMap.Add(value.ToString().ToLower(), value);
    }
    public static string GetTypeStr(this Types types)
    {
        switch (types)
        {
            case Types.Byte: return "System.Byte";
            case Types.Short: return "System.Int16";
            case Types.UShort: return "System.UInt16";
            case Types.Int: return "System.Int32";
            case Types.UInt: return "System.UInt32";
            case Types.Long: return "System.Int64";
            case Types.ULong: return "System.UInt64";
            case Types.Float: return "System.Single";
            case Types.Double: return "System.Double";
            case Types.Decimal: return "System.Decimal";
            case Types.Boolean: return "System.Boolean";
            case Types.String: return "System.String";
            case Types.Vector3: return "UnityEngine.Vector3";
            case Types.Quaternion: return "UnityEngine.Quaternion";
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