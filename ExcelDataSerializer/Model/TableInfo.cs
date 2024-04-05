namespace ExcelDataSerializer.Model;

public abstract class TableInfo
{
    public enum TableType
    {
        List,
        Dictionary,
        Enum,
    }
    public record DataTable
    {
        public string Name;
        public Header? Header = null;
        public DataRow[] Data = Array.Empty<DataRow>();
        public TableType TableType { get; internal set; }

        public void PrintHeader()
        {
            if (Header == null)
                return;

            if (Header.HasPrimaryKey)
                Logger.Instance.LogLine($"Primary Index = {Header.PrimaryIndex}");
            foreach (var cell in Header.SchemaCells)
            {
                Logger.Instance.Log($"{cell.ValueType}\t");
            }
            Logger.Instance.LogLine();
        }

        public void PrintData()
        {
            foreach (var row in Data)
            {
                foreach (var cell in row.DataCells)
                {
                    Logger.Instance.Log($"{cell.Value}\t");
                }
                Logger.Instance.LogLine();
            }
        }
    }
    public record Header
    {
        public int? PrimaryIndex = null;
        public readonly List<SchemaCell> SchemaCells = new();
        public bool HasPrimaryKey => PrimaryIndex.HasValue;
    }
    
    public record DataRow
    {
        public DataCell[] DataCells = Array.Empty<DataCell>();
    }

    public record SchemaCell
    {
        public string Name;
        public int Index;
        public SchemaTypes SchemaTypes;
        public string ValueType = string.Empty;

        public string GetTypeStr()
        {
            return string.Empty;
        }
    }
    public record DataCell
    {
        public int Index;
        public string Value = string.Empty;
    }
}