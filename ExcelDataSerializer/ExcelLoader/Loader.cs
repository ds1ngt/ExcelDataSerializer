using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;

namespace ExcelDataSerializer.ExcelLoader;

public abstract class Loader
{

    public static async UniTask<(IEnumerable<TableInfo.DataTable>, bool)> LoadXlsAsync(string path, ILoader loader)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (Array.Empty<TableInfo.DataTable>(), true);

        Logger.Instance.LogLine($"Excel 로드 = {path}");
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var dataTables = new List<TableInfo.DataTable>();
        var isValid = await loader.LoadWorkbookAsync(fs, dataTables);
        return (dataTables, isValid);
    }
}