namespace ExcelDataSerializer;

public class Logger : IDisposable
{
    private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new());
    public static Logger Instance => _instance.Value;

    private FileStream? _fs;
    private StreamWriter? _sw;

    public Logger()
    {
        var path = Path.Combine(Path.GetTempPath(), "ExcelDataSerializer", "Log", $"{DateTime.Now.ToString("yyMMdd_HHmmss")}.log");
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _fs = new FileStream(path, FileMode.OpenOrCreate);
        _sw = new StreamWriter(_fs);
        Console.WriteLine($"Log File Created: {path}");
    }
    public void Log(string msg = "")
    {
        Console.Write(msg);
        _sw?.Write(msg);
        _sw?.Flush();
    }

    public void LogLine(string msg = "", bool printConsole = true)
    {
        if (printConsole)
            Console.WriteLine(msg);
        _sw?.WriteLine(msg);
        _sw?.Flush();
    }

    public void Dispose()
    {
        _fs?.Dispose();
        _sw?.Dispose();
        _fs = null;
        _sw = null;
    }
}