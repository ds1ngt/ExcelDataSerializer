using ExcelDataSerializer.Util;

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
        private TableType _tableType;
        private DataRow[] _data = Array.Empty<DataRow>();
        private Dictionary<string, DataCell[]> _dataColumnMap = new();
        private Dictionary<string, (string, int)[]> _enumDataColumnMap;

        public string Name;
        public Header? Header = null;

        public DataRow[] Data
        {
            get => _data;
            set
            {
                _data = value;
                RefreshDataColumnMap();

                if(TableType == TableType.Enum)
                    RefreshEnumColumnMap();
            }
        }

        public TableType TableType
        {
            get => _tableType;
            internal set
            {
                _tableType = value;
                if (_tableType == TableType.Enum)
                    RefreshEnumColumnMap();
            }
        }
        public DataCell[] GetDataColumn(string key) => _dataColumnMap.TryGetValue(key, out var items) ? items : Array.Empty<DataCell>();

        public (string, int)[] GetEnumColumn(string key)
        {
            if (TableType != TableType.Enum)
                return Array.Empty<(string, int)>();

            if (_enumDataColumnMap.TryGetValue(key, out var result))
                return result;

            return Array.Empty<(string, int)>();
        }

        public bool TryGetEnumValue(string key, string enumKey, out int value)
        {
            if (int.TryParse(enumKey, out value))
                return true;

            value = 0;

            var column = GetEnumColumn(key);
            foreach (var (enumKeyStr, enumValue) in column)
            {
                if (enumKeyStr == enumKey)
                {
                    value = enumValue;
                    return true;
                }
            }

            return false;
        }
        private void RefreshDataColumnMap()
        {
            _dataColumnMap.Clear();
            var schemaCellMap = Header.SchemaCells.ToDictionary(cell => cell.Index, cell => cell.Name);
            var tempMap = new Dictionary<string, List<DataCell>>();
            foreach (var row in _data)
            {
                foreach (var cell in row.DataCells)
                {
                    if (!schemaCellMap.TryGetValue(cell.Index, out var name)) continue;
                    if (string.IsNullOrWhiteSpace(cell.Value))
                        continue;

                    if (tempMap.TryGetValue(name, out var list))
                        list.Add(cell);
                    else
                        tempMap.Add(name, new List<DataCell> {cell});
                }
            }
            _dataColumnMap = tempMap.ToDictionary(g => g.Key, g => g.Value.ToArray());
        }

        private void RefreshEnumColumnMap()
        {
            _enumDataColumnMap ??= new();
            if (_dataColumnMap.Count == 0)
                return;

            var enumValueTuples = new List<(string, int)>();
            foreach (var (key, columnItems) in _dataColumnMap)
            {
                enumValueTuples.Clear();
                var idx = 0;
                foreach (var columnItem in columnItems)
                {
                    var tokens = columnItem.Value.Split("=");
                    if (tokens.Length == 0)
                        continue;
                    if(string.IsNullOrWhiteSpace(tokens[0]))
                        continue;

                    var enumKey = tokens[0];
                    enumKey = enumKey.Trim();

                    // SomeType=1
                    if (tokens.Length == 2)
                    {
                        if (!int.TryParse(tokens[1], out var num))
                            continue;

                        enumValueTuples.Add( (enumKey, num) );
                        idx = num + 1;
                    }
                    else
                    {
                       enumValueTuples.Add( (enumKey, idx ));
                       idx++;
                    }
                }
                
                _enumDataColumnMap.Add(key, enumValueTuples.ToArray());
            }
        }
        // {
        //     key = Util.Util.TrimUnderscore(key);
        //     var idx = Header.SchemaCells.FindIndex(cell => Util.Util.TrimUnderscore(cell.Name) == key);
        //     if (idx == -1)
        //         return Array.Empty<DataCell>();
        //
        //     var result = new List<DataCell>();
        //     var columnIdx = Header.SchemaCells[idx].Index;
        //     foreach (var row in Data)
        //     {
        //         var cellIdx = Array.FindIndex(row.DataCells, cell => cell.Index == columnIdx);
        //         if(cellIdx == -1)
        //             continue;
        //
        //         result.Add(row.DataCells[cellIdx]);
        //     }
        //     return result.ToArray();
        // }
#region Debug
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
#endregion // Debug
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