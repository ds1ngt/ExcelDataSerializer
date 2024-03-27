namespace ExcelDataSerializer;

public abstract class Util
{
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.StartsWith("#")) return false;
        return true;
    }

    public static string GetValidName(string name)
    {
        if (!IsValidName(name)) return string.Empty;
        return name.Replace("_", string.Empty);
    }
}