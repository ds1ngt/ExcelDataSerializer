using System.Reflection;

namespace ExcelDataSerializer.Model;

public class CodeAssemblyInfo
{
    public string Name;
    public Assembly Assembly;
    public Dictionary<string, dynamic> TypeInstanceMap;

    public bool TryCreateNewInstance(string name, out dynamic newInstance)
    {
        newInstance = default;

        if (!TypeInstanceMap.TryGetValue(name, out var instance))
            return false;
        var type = instance.GetType() as Type;
        newInstance = Assembly.CreateInstance(type.FullName);
        return true;
    }
}