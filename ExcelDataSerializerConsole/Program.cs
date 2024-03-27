using ExcelDataSerializer;

namespace ExcelDataSerializerConsole;

internal abstract class Program
{
    static void Main()
    {
        var files = Directory.GetFiles("Excel");
        var loader = new Loader();
        foreach (var file in files)
        {
            var fullPath = Path.GetFullPath(file);
            loader.LoadXls(fullPath); 
        }
    } 
}