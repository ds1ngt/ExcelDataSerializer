using ExcelDataSerializer;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializerConsole;
internal abstract class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine($"Usage : ExcelDataSerializerConsole [ExcelDirectory] [CSharp Save Directory] [Data Save Directory]");
            return -1;
        }

        var excelDir = args[0];
        var csOutputDir = args[1];
        var dataOutputDir = args[2];
        
        Console.WriteLine($"EXCEL DIR : {Path.GetFullPath(excelDir)}");
        Console.WriteLine($"C# DIR : {Path.GetFullPath(csOutputDir)}");
        Console.WriteLine($"Data DIR : {Path.GetFullPath(dataOutputDir)}");
        if (string.IsNullOrWhiteSpace(excelDir) || string.IsNullOrWhiteSpace(csOutputDir) || string.IsNullOrWhiteSpace(dataOutputDir))
            return -1;

        DeleteAllFiles(csOutputDir);
        DeleteAllFiles(dataOutputDir);
        
        var files = GetExcelFiles(excelDir);
        if(files == Array.Empty<string>())
            return 0;

        var info = new RunnerInfo();
        info.SetOutputDirectory(csOutputDir, dataOutputDir);
        info.AddExcelFiles(files);
        
        Runner.Execute(info);
        return 0;
    }

    static void DeleteAllFiles(string dir)
    {
        if (!Directory.Exists(dir))
            return;
        
        var files = Directory.GetFiles(dir);
        foreach (var file in files)
            File.Delete(file);
    }

    static string[] GetExcelFiles(string dir)
    {
        if (!Directory.Exists(dir))
            return Array.Empty<string>();

        return Directory.GetFiles(dir, "*.xlsx");
    }
}