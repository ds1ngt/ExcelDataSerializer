namespace ExcelDataSerializer.Model;

public abstract class Constant
{
    public static readonly int SchemaCellIdx = 0;
    public static readonly int TypeCellIdx = 1;

    public static readonly string Primary = "Primary";
    public static readonly string Enum = "Enum";
    public static readonly string EnumGet = "EnumGet";
    public static readonly string DataTableMemberName = "Datas";

    public static readonly string String = "String";

    public const string DATA_NAMESPACE = "com.haegin.Billionaire.Data";
    public const string DATA_SUFFIX = "Data";
    public const string DATA_TABLE_SUFFIX = "DataTable";
    public const string INTERFACE_NAME = "ITableData";
}