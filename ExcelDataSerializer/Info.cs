namespace ExcelDataSerializer;

public abstract class Info
{
    public record DataTable
    {
        public string Name;
        public HeaderRow? Header = null;
        public DataRow[] Datas = Array.Empty<DataRow>();

        public void PrintHeader()
        {
            if (Header == null)
                return;

            foreach (var cell in Header.HeaderCells)
            {
                Console.Write($"{cell.Value}\t");
            }
            Console.WriteLine();
        }

        public void PrintData()
        {
            foreach (var row in Datas)
            {
                foreach (var cell in row.DataCells)
                {
                    Console.Write($"{cell.Value}\t");
                }
                Console.WriteLine();
            }
        }
    }
    public record HeaderRow
    {
        public DataCell[] HeaderCells = Array.Empty<DataCell>();
    }
    
    public record DataRow
    {
        public DataCell[] DataCells = Array.Empty<DataCell>();
    }

    public record DataCell
    {
        public int Index;
        public string Value = string.Empty;
    }
}