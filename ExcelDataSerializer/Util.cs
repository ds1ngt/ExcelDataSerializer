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

    public static void SaveToFile(string savePath, string text)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            return;

        var saveDir = Path.GetDirectoryName(savePath);
        if (string.IsNullOrWhiteSpace(saveDir))
            return;

        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        File.WriteAllText(savePath, text);
    }
}