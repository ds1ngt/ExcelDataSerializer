using ExcelDataSerializer;
using ExcelDataSerializer.Model;

namespace com.haegin.billionaire.Data;
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
        // MemoryPackTest();
    }
}