using ExcelDataSerializer;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializerConsole;
internal abstract class Program
{
    static void Main()
    {
        var files = Directory.GetFiles("Excel");
        var info = new RunnerInfo();
        var saveDir = Path.Combine(Path.GetTempPath(), "ExcelDataSerializer");
        info.SetOutputDirectory(saveDir);
        info.AddExcelFiles(files);

        Runner.Execute(info);
        // Runner.Execute();
        // var loader = new ExcelDataSerializer.ExcelLoader.Loader();
        // foreach (var file in files)
        // {
        //     var fullPath = Path.GetFullPath(file);
        //     loader.LoadXls(fullPath); 
        // }
    } 
}