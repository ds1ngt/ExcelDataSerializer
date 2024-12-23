namespace ExcelDataSerializer;

public static class RunnerRules
{
    public static readonly Dictionary<string, SheetConvertRule> SheetConvertRules = new Dictionary<string, SheetConvertRule>
    {
        {
            "String", 
            new SheetConvertRule
            {
                SheetName = "String",
                UseMessagePack = true,
                UseCsv = true,
                AdditionalRule = new StringTableRule(),
            }
        }
    };
}

public record SheetConvertRule
{
    public string SheetName = string.Empty;
    public bool UseMessagePack;
    public bool UseCsv;
    public AdditionalRule? AdditionalRule = null;
}

public abstract record AdditionalRule { }

public record StringTableRule : AdditionalRule
{
    public readonly Dictionary<string, string> KeyConvertMap = new Dictionary<string, string>
    {
        {"ID", "Key"},
        {"KO", "Korean(ko)"},
        {"EN", "English(en)"},
        {"AR", "Arabic(ar)"},
        {"ES", "Spanish(es)"},
        {"FR", "French(fr)"},
        {"JA", "Japanese(ja)"},
        {"JP", "Japanese(ja)"},
        {"ZH-Hans", "Chinese (Simplified)"},
    };
}