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
        return 0;
        
        var files = Directory.GetFiles("Excel");
        var info = new RunnerInfo();
        var saveDir = Path.Combine(Path.GetTempPath(), "ExcelDataSerializer");
        info.SetOutputDirectory(saveDir);
        info.AddExcelFiles(files);
        
        Runner.Execute(info);
        // MemoryPackTest();
    }

    static void DeleteAllFiles(string dir)
    {
        if (!Directory.Exists(dir))
            return;
        
        var files = Directory.GetFiles(dir);
        foreach (var file in files)
            File.Delete(file);
    }
}