using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.ExcelLoader;

public abstract class LoaderUtil
{
    public static TableInfo.TableType GetTableType(TableInfo.Header header)
    {
        if (header.HasPrimaryKey)
            return TableInfo.TableType.Dictionary;
        if (header.SchemaCells.Exists(c => c.SchemaTypes == SchemaTypes.EnumSet))
            return TableInfo.TableType.Enum;
        return TableInfo.TableType.List;
    }

    public static int SheetColumnToIdx(string sheetColumn)
    {
        sheetColumn = sheetColumn.ToUpper();
        var result = 0;
        var digit = 0;
        for (var i = sheetColumn.Length - 1; i >= 0; --i)
        {
            var n = sheetColumn[i] - 'A';
            var sum = i == sheetColumn.Length - 1 ? n : 26 * digit * (n+1);
            result += sum;
            digit++;
        }
        return result;
    }
#region Parse Schema Info
    public static SchemaInfo ParseSchemaInfo(string[] tokens)
    {
        var info = new SchemaInfo
        {
            IsPrimary = IsPrimary(tokens)
        };

        if (IsContains(SchemaTypes.Array, tokens))
            info.SchemaType = SchemaTypes.Array;
        else if (IsContains(SchemaTypes.List, tokens))
            info.SchemaType = SchemaTypes.List;
        else if (IsContains(SchemaTypes.EnumSet, tokens))
            info.SchemaType = SchemaTypes.EnumSet;
        else if (IsEnumGet(tokens))
            info.SchemaType = SchemaTypes.EnumGet;

        if (TryGetPrimitive(tokens, out var type))
        {
            if (info.SchemaType == SchemaTypes.None)
                info.SchemaType = SchemaTypes.Primitive;
            info.DataType = type.GetTypeStr();
        }
        else if (TryGetEnum(tokens, info.IsPrimary, out var enumTypeStr))
        {
            var enumName = Util.Util.GetValidName(enumTypeStr);
            info.DataType = enumName;
        }
        else
        {
            if (info.SchemaType == SchemaTypes.None)
                info.SchemaType = SchemaTypes.Custom;
            info.DataType = GetCustomDataTypeStr(tokens);
        }
        return info;
    }
    private static bool IsPrimary(IEnumerable<string> tokens) => IsContains(Constant.Primary, tokens);
    private static bool IsPrimary(string token) => IsContains(Constant.Primary, token);
    private static bool IsContainer(string[] tokens) => IsArray(tokens) || IsList(tokens);
    private static bool IsContainer(string token) => IsArray(token) || IsList(token);
    private static bool IsArray(string[] tokens) => IsContains(SchemaTypes.Array, tokens);
    private static bool IsArray(string token) => IsContains(SchemaTypes.Array, token);
    private static bool IsList(string[] tokens) => IsContains(SchemaTypes.List, tokens);
    private static bool IsList(string token) => IsContains(SchemaTypes.List, token);
    private static bool IsEnumGet(string[] tokens) =>  IsContains(SchemaTypes.EnumGet, tokens) || IsContains(SchemaTypes.Enum, tokens);
    private static bool IsEnumGet(string token) => IsContains(SchemaTypes.EnumGet, token);
    private static bool IsContains(SchemaTypes schemaTypes, IEnumerable<string> tokens) => IsContains(schemaTypes.ToString(), tokens);
    private static bool IsContains(SchemaTypes schemaTypes, string token) => IsContains(schemaTypes.ToString(), token);
    private static bool IsContains(string schemaTypeStr, IEnumerable<string> tokens)
    {
        var compare = schemaTypeStr.ToLower();
        return tokens.Any(token => compare == token.ToLower());
    }
    private static bool IsContains(string schemaTypeStr, string token)
    {
        if (string.IsNullOrWhiteSpace(schemaTypeStr) || string.IsNullOrWhiteSpace(token))
            return false;

        var compare = schemaTypeStr.ToLower();
        return compare == token.ToLower();
    }
    private static bool TryGetPrimitive(IEnumerable<string> tokens, out Types type)
    {
        type = Types.Byte;
        foreach (var token in tokens)
        {
            if (TypesExtension.TryGetValue(token, out type))
                return true;
        }

        return false;
    }
    private static bool TryGetEnum(string[] tokens, bool isPrimary, out string enumTypeStr)
    {
        enumTypeStr = string.Empty;

        if (!IsValid())
            return false;

        var schemaIdx = Constant.SchemaCellIdx;
        var typeIdx = Constant.TypeCellIdx;
        
        if (isPrimary)
        {
            schemaIdx++;
            typeIdx++;
        }
        if (tokens[schemaIdx].ToLower() != Constant.EnumGet.ToLower())
            return false;

        enumTypeStr = tokens[typeIdx];
        return true;

        bool IsValid() => isPrimary ? tokens.Length >= 3 : tokens.Length >= 2;
    }
    private static string GetCustomDataTypeStr(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (SchemaExtension.IsSchema(token)) continue;
            if (TypesExtension.IsType(token)) continue;
            return token;
        }

        return string.Empty;
    }
#endregion // Parse Schema Info
}