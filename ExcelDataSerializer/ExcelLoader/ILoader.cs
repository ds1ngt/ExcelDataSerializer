using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.ExcelLoader;

public interface ILoader
{
    UniTask<bool> LoadWorkbookAsync(FileStream fs, List<TableInfo.DataTable> dataTables);
}