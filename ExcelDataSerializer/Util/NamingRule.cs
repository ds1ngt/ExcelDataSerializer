﻿namespace ExcelDataSerializer.Util;

public class NamingRule
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
        foreach (var (key, value) in _checkMap)
        {
            if (check.Contains(key))
                return value;
        }

        return check;
    }
}