namespace ExcelDataSerializer.Util;

public class Logger : IDisposable
{
    private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new());
    public static Logger Instance => _instance.Value;

    private FileStream? _fs;
    private StreamWriter? _sw;
    private readonly string _logPath;
    public string LogPath => _logPath;
    public event Action<string, bool> OnLog = (msg, lineBreak) => { };
    public event Action<string, bool> OnLogError = (msg, lineBreak) => { };

    public Logger()
    {
        _logPath = Path.Combine(Path.GetTempPath(), "ExcelDataSerializer", "Log", $"{DateTime.Now.ToString("yyMMdd_HHmmss")}.log");
        var dir = Path.GetDirectoryName(_logPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _fs = new FileStream(_logPath, FileMode.OpenOrCreate);
        _sw = new StreamWriter(_fs);
        Console.WriteLine($"Log File Created: {_logPath}");
    }
    public void Log(string msg = "")
    {
        Console.Write(msg);
        _sw?.Write(msg);
        _sw?.Flush();
        OnLog.Invoke(msg, false);
    }

    public void LogLine(string msg = "", bool printConsole = true)
    {
        if (printConsole)
            Console.WriteLine(msg);
        _sw?.WriteLine(msg);
        _sw?.Flush();
        OnLog.Invoke(msg, true);
    }

    public void LogError(string msg = "")
    {
        Console.Write(msg);
        _sw?.Write(msg);
        _sw?.Flush();
        OnLogError.Invoke(msg, false);
    }

    public void LogErrorLine(string msg = "", bool printConsole = true)
    {
        var errorMessage = $"[ERROR] {msg}";
        if (printConsole)
            Console.WriteLine(errorMessage);
        _sw?.WriteLine(errorMessage);
        _sw?.Flush();
        OnLogError.Invoke(errorMessage, true);
    }
    public void Dispose()
    {
        _fs?.Dispose();
        _sw?.Dispose();
        _fs = null;
        _sw = null;
    }
}