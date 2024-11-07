using System.Text;

namespace ExcelDataSerializerUI.Util;

public class Logger
{
    private readonly StringBuilder _sb = new();

    public string Message => _sb.ToString();

    public void AppendLog(string msg) => _sb.Append(msg);
    public void AppendLogLine(string msg) => _sb.AppendLine(msg);
    public void Clear() => _sb.Clear();
}