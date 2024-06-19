namespace ExcelDataSerializer.Util;

public abstract class NamingRule
{
    /// <summary>
    /// Key의 문자열을 포함하면 Value로 변환
    /// </summary>
    private static Dictionary<string, string> _checkMap = new Dictionary<string, string>
    {
        {"TilePlacement", "TilePlacement"}
    };

    public static string Check(string check)
    {
        foreach (var kvp in _checkMap)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            if (check.Contains(key))
                return value;
        }

        return check;
    }
}