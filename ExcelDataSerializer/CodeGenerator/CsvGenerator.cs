using System.Text;
using ExcelDataSerializer.Model;

namespace ExcelDataSerializer.CodeGenerator;

public abstract class CsvGenerator
{
    public static string GenerateCsv(TableInfo.DataTable dataTable)
    {
        RunnerRules.SheetConvertRules.TryGetValue(dataTable.Name, out var rule);

        var sb = new StringBuilder();
        AppendHeader(dataTable, sb, rule);
        AppendData(dataTable, sb, rule);
        
        Console.WriteLine($"============================ CSV : {dataTable.Name} ({dataTable.Data.Length}) =============================");
        // Console.WriteLine(sb.ToString());
        // Console.WriteLine($"=================================================================================");
        return sb.ToString();
    }

    private static void AppendHeader(TableInfo.DataTable dataTable, StringBuilder sb, SheetConvertRule? rule)
    {
        if (dataTable.Header == null)
            return;
        
        //  Id,Key,언어1,언어2,...
        var list = dataTable.Header.SchemaCells
            .Select(c => GetKey(c.Name))
            .ToList();
        list.Insert(0, "Id");

        var row = list.Aggregate((l, r) => $"{l},{r}");

        sb.AppendLine(row);
        return;

        string GetKey(string oldKey)
        {
            if (rule is null)
                return oldKey;

            if (rule.AdditionalRule is not StringTableRule stringRule)
                return oldKey;

            return stringRule.KeyConvertMap.GetValueOrDefault(oldKey, oldKey);
        }
    }
    
    private static void AppendData(TableInfo.DataTable dataTable, StringBuilder sb, SheetConvertRule? rule)
    {
        for (var i = 0; i < dataTable.Data.Length; i++)
        {
            var id = i+1;
            var data = dataTable.Data[i];
            
            var list = data.DataCells
                .Select(c => Convert(c.Value))
                .ToList();
            list.Insert(0, id.ToString());

            var row = list.Aggregate((l, r) => $"{l},{r}");
            sb.AppendLine(row);
        }

        string Convert(string value)
        {
            // while (value.EndsWith('\n')) 
            //     value = value.TrimEnd('\n');
            
            return $"\"{value}\"";
        }
    }
}