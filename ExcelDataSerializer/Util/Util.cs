using Cysharp.Threading.Tasks;

namespace ExcelDataSerializer.Util;

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
        return TrimInvalidChar(name);
    }

    public static async UniTask SaveToFileAsync(string savePath, string text)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            return;

        var saveDir = Path.GetDirectoryName(savePath);
        if (string.IsNullOrWhiteSpace(saveDir))
            return;

        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        await File.WriteAllTextAsync(savePath, text);
        Logger.Instance.LogLine($"파일 저장 {savePath}");
    }

    public static void DeleteAndRecreateDirectory(string dirPath)
    {
        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);
        Directory.CreateDirectory(dirPath);
    }
    public static string TrimInvalidChar(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = TrimUnderscore(value);
        value = TrimBlankSpace(value);
        return value;
    }
    public static string TrimUnderscore(string value) => value.Replace("_", string.Empty);
    public static string TrimBlankSpace(string value) => value.Replace(" ", string.Empty);
}