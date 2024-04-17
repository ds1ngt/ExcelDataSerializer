using System.Reflection;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using MemoryPack;

namespace ExcelDataSerializer.DataExtractor;

public abstract class AssemblyDataInjector
{
    public static void Test(Dictionary<string, TableInfo.DataTable> dataTableMap, Dictionary<string, CodeAssemblyInfo> assemblyMap)
    {
        var keys = dataTableMap.Keys;
        foreach (var key in keys)
        {
            var tableInfo = dataTableMap[key];
            if (!assemblyMap.ContainsKey(key))
            {
                Logger.Instance.LogLine($"Assembly Not Exist : {key}");
                continue;
            }
            var assemblyInfo = assemblyMap[key];
            Process(tableInfo, assemblyInfo);
        }
    }

    private static void Process(TableInfo.DataTable dataTable, CodeAssemblyInfo assemblyInfo)
    {
        var name = assemblyInfo.Name;
        switch (dataTable.TableType)
        {
            case TableInfo.TableType.List:
            {
                // var instance = assemblyInfo.TypeInstanceMap[name];
            }
                break;
            case TableInfo.TableType.Dictionary:
            {
                Logger.Instance.LogLine($"Fill Data -> {name}");
                var tableName = $"{name}Table";
                var tableInstance = assemblyInfo.TypeInstanceMap[tableName];
                var header = dataTable.Header;
                if (header is not { HasPrimaryKey: true })
                    break;

                var tableInstanceType = tableInstance.GetType() as Type;
                var instanceType = assemblyInfo.TypeInstanceMap[name].GetType() as Type;
                if (tableInstanceType == null || instanceType == null)
                    break;

                var instanceProps = instanceType.GetProperties();
                var instancePropsMap = instanceProps.ToDictionary(g => g.Name, g => g);

                foreach (var key in instancePropsMap.Keys)
                {
                    Logger.Instance.LogLine($"{instanceType.Name} Property {key} = {instancePropsMap[key]}");
                }
                var keyIdx = header.PrimaryIndex!.Value;
                for (var i = 0; i < dataTable.Data.Length; i++)
                {
                    var row = dataTable.Data[i];
                    if (!assemblyInfo.TryCreateNewInstance(name, out var instance))
                        break;

                    dynamic? key = default;
                    string keyValueStr = string.Empty;
                    var mergedData = header.SchemaCells.Join(row.DataCells, cell => cell.Index, cell => cell.Index,
                        (cell, dataCell) => new
                        {
                            Index = cell.Index,
                            Name = cell.Name,
                            SchemaTypes = cell.SchemaTypes,
                            Value = dataCell.Value,
                            ValueType = cell.ValueType,
                        }).ToArray();

                    Logger.Instance.LogLine($"Parse Data ({mergedData.Length})");
                    foreach (var item in mergedData)
                    {
                        var value = ParseData(item.SchemaTypes, item.ValueType, item.Value);
                        if (item.Index == keyIdx)
                        {
                            key = value;
                            keyValueStr = item.Value;
                        }
                        
                        if (value == null)
                            continue;
                        
                        if (!instancePropsMap.TryGetValue(item.Name, out var prop))
                        {
                            Logger.Instance.LogLine($"Property Not Found {item.Name}");
                            continue;
                        }

                        Logger.Instance.LogLine($"[{item.Index}] Name: {item.Name}, [{item.SchemaTypes} / {item.ValueType}], Value: {item.Value}, DynamicValue: {value}");
                        prop.SetValue(instance, value);
                    }

                    if (key == null)
                        throw new NullReferenceException($"{name} 테이블에서 키를 확인할 수 없습니다. ({i + 2} 번 째 줄) = {keyValueStr}");

                    Logger.Instance.LogLine($"Add KEY : {key}");
                    tableInstance.Add(key, instance);
                }
                
                Logger.Instance.LogLine($"Table {name} has ({tableInstance.Count}) items., {tableInstanceType.FullName}");
                
                // foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                // {
                //     Logger.Instance.LogLine($"Executing Assembly Type = {type.Name} , {type.FullName}");
                // }
                //
                // foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                // {
                //     Logger.Instance.LogLine($"------------ {assembly.FullName} ({assembly.GetTypes().Length}) ------------");
                //     foreach (var type in assembly.GetTypes())
                //     {
                //         Logger.Instance.LogLine($"[{type.Name}] {type.FullName}", false);
                //     }
                // }

                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                    break;

                Logger.Instance.LogLine($"ENTRY ASSEMBLY = {entryAssembly.FullName}");
                foreach (var type in entryAssembly.GetTypes())
                {
                    Logger.Instance.LogLine($"====== {type}");
                }
                
                // var typeName = $"{GetAssemblyName(entryAssembly.FullName)}.{tableName}";
                // var tableType = entryAssembly.GetType(typeName);
                // if (tableType == null)
                    // break;
                
                // var bytes = MemoryPackSerializer.Serialize(tableInstance);
                // var base64Str = Convert.ToBase64String(bytes);
                // Logger.Instance.LogLine("Base64 Encoded --------------------------------");
                // Logger.Instance.LogLine(base64Str);

                // // if (!assemblyInfo.TryCreateNewInstance(name, out var instance))
                // //     return;
                // var type = instance.GetType() as Type;
                // var props = type.GetProperties();
                // foreach (var prop in props)
                // {
                //     Logger.Instance.LogLine($"Instance Prop : {prop.Name}");
                //     
                // }
                // MemoryPackSerializer.Serialize(tableInstance)
            }
                break;
            case TableInfo.TableType.Enum:
            {
                foreach (var enumType in assemblyInfo.Assembly.GetTypes())
                {
                    Logger.Instance.LogLine($"Enum : {enumType.Name}");
                    var values = Enum.GetValues(enumType);
                    foreach (var value in values)
                    {
                        Logger.Instance.LogLine($"--> {value} [{(int) value}]");
                    }
                }
            }
                break;
            default:
                break;
        }
    }

    private static string GetAssemblyName(string fullName)
    {
        var idx = fullName.IndexOf(',');
        if (idx == -1)
            return fullName;

        fullName = fullName.Substring(0, idx);
        return fullName;
    }
    private static void InsertDictionaryItem(dynamic dictionary, dynamic key, dynamic value)
    {
    }

    private static int ConvertToInt(string value) => int.TryParse(value, out var result) ? result : 0;

    private static dynamic? ParseData(SchemaTypes schemaType, string valueType, string value)
    {
        switch (schemaType)
        {
            case SchemaTypes.Primitive:
                return ParsePrimitive(valueType, value);
            case SchemaTypes.Array:
                return ParseArray(valueType, value);
            case SchemaTypes.List:
                return ParseList(valueType, value);
            case SchemaTypes.EnumGet:
                break;
            case SchemaTypes.EnumSet:
                break;
            case SchemaTypes.Custom:
                break;
            case SchemaTypes.None:
            default:
                return null;
        }

        return null;
    }

    private static dynamic? ParsePrimitive(string valueType, string value)
    {
        valueType = TrimPrimitiveTypeName(valueType);
        if (!TypesExtension.TryGetValue(valueType, out var type))
            return null;

        switch (type)
        {
            case Types.Byte:
            {
                if (byte.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Short:
            case Types.Int16:
            {
                if (short.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.UShort:
            case Types.UInt16:
            {
                if (ushort.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Int:
            case Types.Int32:
            {
                if (int.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.UInt:
            case Types.UInt32:
            {
                if (uint.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Long:
            case Types.Int64:
            {
                if (long.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.ULong:
            case Types.UInt64:
            {
                if (ulong.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Float:
            case Types.Single:
            {
                if (float.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Double:
            {
                if (double.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Decimal:
            {
                if (decimal.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.Boolean:
            {
                if (bool.TryParse(value, out var v))
                    return v;
            }
                break;
            case Types.String:
                return value;
            
            // TODO : Parse Unity Primitive Type
            case Types.Vector3:
                break;
            case Types.Quaternion:
                break;
            default:
                break;
        }

        return null;
    }

    private static string TrimPrimitiveTypeName(string valueType)
    {
        if (string.IsNullOrWhiteSpace(valueType))
            return string.Empty;

        var idx = valueType.LastIndexOf('.');
        if (idx == -1)
            return valueType;

        valueType = valueType.Substring(idx + 1);
        return valueType;
    }
    private static dynamic? ParseArray(string valueType, string value)
    {
        if (!TryGetContainerTokens(value, out var tokens))
            return null;

        return tokens.Select(t => ParsePrimitive(valueType, t)).ToArray();
    }
    private static dynamic? ParseList(string valueType, string value)
    {
        if (!TryGetContainerTokens(value, out var tokens))
            return null;

        return tokens.Select(t => ParsePrimitive(valueType, t)).ToList();
    }
    private static dynamic ParseEnum(string valueType, string value)
    {
        return default;
    }

    private static bool TryGetContainerTokens(string value, out string[] tokens)
    {
        tokens = Array.Empty<string>();

        if (!IsContainer(value)) 
            return false;

        value = value.Substring(1, value.Length - 2);
        tokens = value.Trim().Split(',');
        return true;
    }
    private static bool IsContainer(string value) => value.Length > 2 && value.StartsWith('[') && value.EndsWith(']');
}