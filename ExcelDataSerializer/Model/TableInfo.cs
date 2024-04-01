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
        public DataRow[] Datas = Array.Empty<DataRow>();
        public TableType TableType { get; internal set; }

        public void PrintHeader()
        {
            if (Header == null)
                return;

            foreach (var cell in Header.SchemaCells)
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

    public abstract record SchemaCell
    {
        public int Index;
        public SchemaTypes SchemaTypes;
        public ContainerType? ContainerType; // SchemaType is (Array, List, Dictionary)
        public string Value = string.Empty;

        public string GetTypeStr()
        {
            return string.Empty;
        }

        private bool TryGetContainerTypeStr(out string typeStr)
        {
            typeStr = string.Empty;
            if (ContainerType == null)
                return false;

            var containerKeyType = ContainerType.KeyType.GetTypeStr();
            var containerValueType = ContainerType.ValueType.GetTypeStr();

            switch (SchemaTypes)
            {
                case SchemaTypes.Array:
                    typeStr = $"{containerValueType}[]";
                    return true;
                case SchemaTypes.List:
                    typeStr = $"System.Collections.Generic.List<{containerValueType}>";
                    return true;
                case SchemaTypes.Dictionary:
                    typeStr = $"System.Collections.Generic.Dictionary<{containerKeyType}, {containerValueType}>";
                    return true;
                default:
                    return false;
            }

            return false;
        }
    }
    public record DataCell
    {
        public int Index;
        public string Value = string.Empty;
    }
}