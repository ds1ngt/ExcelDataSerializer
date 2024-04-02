using ClosedXML.Excel;

namespace ExcelDataSerializer.ExcelLoader;

public static class ClosedXmlExtension
{
    public static string GetValidName(this IXLCell? cell)
    {
        if (cell == null)
            return string.Empty;

        if (!cell.TryGetValue<string>(out var cellValue)) 
            return string.Empty;

        var value = Util.GetValidName(cellValue);
        return Util.IsValidName(value) ? value : string.Empty;
    }
}