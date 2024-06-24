namespace ExcelDataSerializer.DataExtractor;

public abstract partial class MessagePackExtractor
{
    private static readonly Dictionary<string, string> _fileMap = new Dictionary<string, string>
    {
        {"BillionaireClient.csproj", PROJ},
        {"DataTable.cs", DATA_TABLE_CLASS},
        {"Extractor.cs", EXTRACTOR_CLASS},
    };
    private const string PROJ = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""MessagePack"" Version=""3.0.54-alpha"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
    <PackageReference Include=""UniTask"" Version=""2.5.4"" />
    <None Update=""Data.json"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
";
    
    private const string DATA_TABLE_CLASS = @"namespace BillionaireClient;

enum TableType
{
    List,
    Dictionary,
    Enum,
    Interface
}
enum SchemaTypes
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

enum Types
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

record DataTable
{
    public string Name;
    public string ClassName;
    public Header? Header;
    public DataRow[] Data;
    public TableType TableType;
}

record Header
{
    public int? PrimaryIndex;
    public List<SchemaCell> SchemaCells = new();
}

internal record DataRow
{
    public DataCell[] DataCells = Array.Empty<DataCell>();
}
record SchemaCell
{
    public string Name;
    public int Index;
    public SchemaTypes SchemaTypes;
    public string ValueType = string.Empty;
}

record DataCell
{
    public int Index;
    public string Value = string.Empty;
}

static class TypesExtension
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
            case Types.Byte: return ""System.Byte"";
            case Types.Short:
            case Types.Int16: 
                return ""System.Int16"";
            case Types.UShort:
            case Types.UInt16: 
                return ""System.UInt16"";
            case Types.Int:
            case Types.Int32: 
                return ""System.Int32"";
            case Types.UInt:
            case Types.UInt32: 
                return ""System.UInt32"";
            case Types.Long:
            case Types.Int64: 
                return ""System.Int64"";
            case Types.ULong:
            case Types.UInt64:
                return ""System.UInt64"";
            case Types.Single:
            case Types.Float: 
                return ""System.Single"";
            case Types.Double: return ""System.Double"";
            case Types.Decimal: return ""System.Decimal"";
            case Types.Bool:
            case Types.Boolean:
                return ""System.Boolean"";
            case Types.String: return ""System.String"";
            case Types.Vector3: return ""UnityEngine.Vector3"";
            case Types.Quaternion: return ""UnityEngine.Quaternion"";
            default:
                return string.Empty;
        }
    }

    public static string GetShortTypeStr(this Types types)
    {
        switch (types)
        {
            case Types.Byte: return ""byte"";
            case Types.Short:
            case Types.Int16:
                return ""short"";
            case Types.UShort:
            case Types.UInt16:
                return ""ushort"";
            case Types.Int:
            case Types.Int32:
                return ""int"";
            case Types.UInt:
            case Types.UInt32:
                return ""uint"";
            case Types.Long:
            case Types.Int64:
                return ""long"";
            case Types.ULong:
            case Types.UInt64:
                return ""ulong"";
            case Types.Float:
            case Types.Single:
                return ""float"";
            case Types.Double:
                return ""double"";
            case Types.Decimal:
                return ""decimal"";
            case Types.Bool:
            case Types.Boolean:
                return ""bool"";
            case Types.String:
                return ""string"";
            case Types.Vector3:
                return ""UnityEngine.Vector3"";
            case Types.Quaternion:
                return ""UnityEngine.Quaternion"";
            default:
                return string.Empty;
        }
    }

    public static Type? GetType(Types types) => types switch
    {
        Types.Byte => typeof(byte),
        Types.Short => typeof(short),
        Types.Int16 => typeof(short),
        Types.UShort => typeof(ushort),
        Types.UInt16 => typeof(ushort),
        Types.Int => typeof(int),
        Types.Int32 => typeof(int),
        Types.UInt => typeof(uint),
        Types.UInt32 => typeof(uint),
        Types.Long => typeof(long),
        Types.Int64 => typeof(long),
        Types.ULong => typeof(ulong),
        Types.UInt64 => typeof(ulong),
        Types.Float => typeof(float),
        Types.Single => typeof(float),
        Types.Double => typeof(double),
        Types.Decimal => typeof(decimal),
        Types.Bool => typeof(bool),
        Types.Boolean => typeof(bool),
        Types.String => typeof(string),
        Types.Vector3 => null,
        Types.Quaternion => null,
        _ => null,
    };
    public static bool TryGetValue(string typeStr, out Types types)
    {
        types = Types.Int;
        if (string.IsNullOrWhiteSpace(typeStr))
            return false;

        if (typeStr.StartsWith(""System.""))
            typeStr = typeStr.Substring(""System."".Length, typeStr.Length - ""System."".Length);

        return _strTypesMap.TryGetValue(typeStr.ToLower(), out types);
    }

    public static bool IsType(string typeStr) => _strTypesMap.ContainsKey(typeStr.ToLower());
}";

    private const string EXTRACTOR_CLASS = @"using System.Reflection;
using Cysharp.Threading.Tasks;
using MessagePack;
using Newtonsoft.Json;

namespace BillionaireClient;

class Extractor
{
    private const string TYPE_NAMESPACE = ""com.haegin.Billionaire.Data"";
    private const string JSON_FILENAME = ""Data.json"";
    private const string OUTPUT_DIR = ""Data"";
    public static void Main()
    {
        Extract().AsTask().Wait();
    }

    private static async UniTask Extract()
    {
        var dataTables = await LoadDataTablesAsync(JSON_FILENAME);
        if (dataTables == null)
            return;
        
        foreach (var table in dataTables)
        {
            if (!IsValidTableName(table.Name))
                continue;

            await ProcessTableAsync(table);
        }
    }

    private static async UniTask<DataTable[]?> LoadDataTablesAsync(string jsonFile)
    {
        var jsonStr = await File.ReadAllTextAsync(jsonFile);
        var dataTables = JsonConvert.DeserializeObject<DataTable[]>(jsonStr);
        Console.WriteLine($""ExtractJson: {jsonStr.Length}, Datas: {dataTables?.Length}"");
        return dataTables;
    }
    private static bool IsValidTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;
        if (tableName == ""Enum"")
            return false;
        return true;
    }

    private static async UniTask ProcessTableAsync(DataTable table)
    {
        var fileName = table.Name;
        var dataClassName = GetDataTypeStr(table.ClassName);
        var tableClassName = GetTableTypeStr(table.ClassName);
        var dataType = Type.GetType(dataClassName);
        var tableType = Type.GetType(tableClassName);

        Console.WriteLine($""FileName: {fileName}, Name: {table.Name}, ClassName: {dataClassName} / {tableClassName}, Rows: {table.Data.Length}"");

        if (dataType == null || tableType == null)
        {
            Console.WriteLine($""{table.Name} Cannot Find Type Data:({dataType}), Table:({tableType})"");
            return;
        }

        dynamic tableInstance = Activator.CreateInstance(tableType)!;
        var datasField = tableType.GetField(""Datas"");
        var datas = datasField.GetValue(tableInstance);
        
        foreach (var row in table.Data)
        {
            if (!IsValidRow(row))
                continue;

            var (key, keyType) = GetPrimaryKeyAndType(table, row);
            InstanceData? instanceData = CreateInstance(dataType, table.Header, row);

            if (instanceData.Value.instance == null)
                continue;

            var isDictionary = key != null;
            if (isDictionary)
            {
                datas = CheckAndCreateDictionary(datas, keyType, dataType);
                datasField.SetValue(tableInstance, datas);

                if (!datas.TryAdd(key, instanceData.Value.instance))
                    Console.WriteLine($""--- Key Duplicated [{tableClassName}] Key: {key}"");
            }
            else
                datas.Add(instanceData.Value.instance);
        }

        var serialized = MessagePackSerializer.Serialize(tableInstance) as byte[];
        var base64String = Convert.ToBase64String(serialized);
        await SaveDataTableAsync($""{table.Name}.data"", base64String);
    }
    private static string GetDataTypeStr(string className) => $""{TYPE_NAMESPACE}.{className}Data"";
    private static string GetTableTypeStr(string className) => $""{TYPE_NAMESPACE}.{className}DataTable"";

    private static bool IsValidRow(DataRow? row)
    {
        if (row == null)
            return false;

        var isEmptyRow = row.DataCells.All(cell => string.IsNullOrWhiteSpace(cell.Value));
        return !isEmptyRow;
    }
    
    private static (dynamic?, Type?) GetPrimaryKeyAndType(DataTable? table, DataRow? row)
    {
        if (!table.Header.PrimaryIndex.HasValue)
            return (null, null);

        var primaryIndex = table.Header.PrimaryIndex.Value;
        if (!TryGetSchemaCell(table.Header.SchemaCells, primaryIndex, out var schemaCell))
            return (null, null);

        if (!TryGetDataCell(row, primaryIndex, out var primaryCell))
            return (null, null);

        if (TypesExtension.TryGetValue(schemaCell.ValueType, out var types))
        {
            var key = GetPrimitive(types, primaryCell.Value);
            var keyType = TypesExtension.GetType(types);
            return (key, keyType);
        }
        else
        {
            var keyType = Type.GetType($""{TYPE_NAMESPACE}.{schemaCell.ValueType}"");
            if (Enum.TryParse(keyType, primaryCell.Value, true, out var value))
            {
                var key = value;
                return (key, keyType);
            }
            else
                Console.WriteLine($""CANNOT FIND ENUM VALUE {keyType.Name} : {primaryCell.Value}"");
        }

        return (null, null);
    }
    private static bool TryGetSchemaCell(List<SchemaCell> schemas, int index, out SchemaCell? find)
    {
        find = null;

        var idx = schemas.FindIndex(s => s.Index == index);
        if (idx == -1)
            return false;

        find = schemas[idx];
        return true;
    }
    private static bool TryGetDataCell(DataRow row, int index, out DataCell find)
    {
        find = null;
        foreach (var cell in row.DataCells)
        {
            if (cell.Index != index) 
                return false;
            find = cell;
            return true;
        }

        return false;
    }
    
    private static dynamic? CheckAndCreateDictionary(dynamic? datas, Type? keyType, Type dataType)
    {
        if (datas != null)
            return datas;
        
        var dictionaryType = typeof(Dictionary<,>);
        var genericType = dictionaryType.MakeGenericType(keyType, dataType);
        datas = Activator.CreateInstance(genericType);
        
        return datas;
    }
    
#region Create Instance
    struct InstanceData
    {
        public dynamic Key;
        public dynamic instance;
    }
    static InstanceData CreateInstance(Type dataType, Header tableHeader, DataRow row)
    {
        dynamic instance = Activator.CreateInstance(dataType);
        object? key = null;
        bool isKeySchema = false;
        foreach (var schemaCell in tableHeader.SchemaCells)
        {
            var dataIdx = Array.FindIndex(row.DataCells, c => c.Index == schemaCell.Index);
            if(dataIdx == -1)
                continue;

            var dataCell = row.DataCells[dataIdx];
            var property = dataType.GetProperty(schemaCell.Name);
            
            if (tableHeader.PrimaryIndex.HasValue)
            {
                
            }

            switch (schemaCell.SchemaTypes)
            {
                case SchemaTypes.Primitive: // 2
                {
                    if (TypesExtension.TryGetValue(schemaCell.ValueType, out var types))
                        SetPrimitiveValue(instance, property, types, dataCell.Value);
                }
                    break;
                case SchemaTypes.Array: // 3
                {
                    if (TypesExtension.TryGetValue(schemaCell.ValueType, out var types))
                        SetArrayValue(instance, property, types, dataCell.Value);
                }
                    break;
                case SchemaTypes.EnumGet:       // 7
                    SetEnumValue(instance, property, dataCell.Value);
                    break;
                case SchemaTypes.EnumSet:       // 8
                    break;
            }
        }

        return new InstanceData
        {
            Key = key,
            instance = instance
        };
    }
#endregion // Create Instance

#region Parse FieldData
    static Object SetPrimitiveValue(Object instance, PropertyInfo property, Types type, string strValue)
    {
        // strValue = strValue.Replace(""\"""", string.Empty);
        switch (type)
        {
            case Types.Byte:
            {
                byte.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Short:
            case Types.Int16:
            {
                short.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.UShort:
            case Types.UInt16:
            {
                ushort.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Int:
            case Types.Int32:
            {
                int.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.UInt:
            case Types.UInt32:
            {
                uint.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Long:
            case Types.Int64:
            {
                long.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.ULong:
            case Types.UInt64:
            {
                ulong.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Float:
            case Types.Single:
            {
                float.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Double:
            {
                double.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Decimal:
            {
                decimal.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.Bool:
            case Types.Boolean:
            {
                bool.TryParse(strValue, out var value);
                property.SetValue(instance, value);
                return value;
            }
            case Types.String:
            {
                property.SetValue(instance, strValue);
                return strValue;
            }
            case Types.Vector3:
            case Types.Quaternion:
                break;
            default:
                break;
        }

        return null;
    }

    static dynamic? GetPrimitive(Types types, string strValue)
    {
        switch (types)
        {
            case Types.Byte:
            {
                byte.TryParse(strValue, out var value);
                return value;
            }
            case Types.Short:
            case Types.Int16:
            {
                short.TryParse(strValue, out var value);
                return value;
            }
            case Types.UShort:
            case Types.UInt16:
            {
                ushort.TryParse(strValue, out var value);
                return value;
            }
            case Types.Int:
            case Types.Int32:
            {
                int.TryParse(strValue, out var value);
                return value;
            }
            case Types.UInt:
            case Types.UInt32:
            {
                uint.TryParse(strValue, out var value);
                return value;
            }
            case Types.Long:
            case Types.Int64:
            {
                long.TryParse(strValue, out var value);
                return value;
            }
            case Types.ULong:
            case Types.UInt64:
            {
                ulong.TryParse(strValue, out var value);
                return value;
            }
            case Types.Float:
            case Types.Single:
            {
                float.TryParse(strValue, out var value);
                return value;
            }
            case Types.Double:
            {
                double.TryParse(strValue, out var value);
                return value;
            }
            case Types.Decimal:
            {
                decimal.TryParse(strValue, out var value);
                return value;
            }
            case Types.Bool:
            case Types.Boolean:
            {
                bool.TryParse(strValue, out var value);
                return value;
            }
            case Types.String:
            {
                return strValue;
            }
            case Types.Vector3:
            case Types.Quaternion:
                break;
            default:
                break;
        }

        return null;
    }
    static void SetArrayValue(Object instance, PropertyInfo property, Types types, string value)
    {
        if (!IsArrayValue(value))
            return;

        var items = GetArrayItems(value);
        if (items.Length == 0)
            return;
        
        var array = CreateArray(types, items.Length);
        if (array == null)
            return;

        for (var i = 0; i < items.Length; ++i)
            array[i] = GetPrimitive(types, items[i]);
        property.SetValue(instance, array);
        return;
        
        bool IsArrayValue(string v) => string.IsNullOrWhiteSpace(v) || v.StartsWith('[') && v.EndsWith(']');

        string[] GetArrayItems(string v)
        {
            if (v.Length <= 2)
                return [];

            v = v.Substring(1, value.Length - 2);
            var tokens = v.Trim().Split(',');
            return tokens;
        }

        dynamic? CreateArray(Types type, int size) => type switch
        {
            Types.Byte => new byte[size],
            Types.Short => new short[size],
            Types.Int16 => new short[size],
            Types.UShort => new ushort[size],
            Types.UInt16 => new ushort[size],
            Types.Int => new int[size],
            Types.Int32 => new int[size],
            Types.UInt => new uint[size],
            Types.UInt32 => new uint[size],
            Types.Long => new long[size],
            Types.Int64 => new long[size],
            Types.ULong => new ulong[size],
            Types.UInt64 => new ulong[size],
            Types.Float => new float[size],
            Types.Single => new float[size],
            Types.Double => new double[size],
            Types.Decimal => new decimal[size],
            Types.Bool => new bool[size],
            Types.Boolean => new bool[size],
            Types.String => new string[size],
            Types.Vector3 => null,
            Types.Quaternion => null,
            _ => null,
        };
    }

    static void SetEnumValue(Object instance, PropertyInfo property, string strValue)
    {
        // strValue = Util.ToEnumItemName(strValue);
        if (Enum.TryParse(property.PropertyType, strValue, out var enumValue))
            property.SetValue(instance, enumValue);
    }

    static object GetEnumValue(Object instance, FieldInfo field, string strValue)
    {
        // strValue = Util.ToEnumItemName(strValue);
        if (Enum.TryParse(field.FieldType, strValue, out var result))
            return result;
        return null;
    }
#endregion

#region Save Data

    private static async UniTask SaveDataTableAsync(string fileName, string base64String)
    {
        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);
        
        var filePath = Path.Combine(OUTPUT_DIR, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        
        await File.WriteAllTextAsync(filePath, base64String);
    }

#endregion // Save Data
}";
}