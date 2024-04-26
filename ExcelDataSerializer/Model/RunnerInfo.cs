namespace ExcelDataSerializer.Model;

public class RunnerInfo
{
    private string _csOutputDir = string.Empty;
    private string _dataOutputDir = string.Empty;
    private readonly List<string> _xlsxFiles = new List<string>();

#region Properties
    public Runner.ExcelLoaderType ExcelLoaderType { get; set; } = Runner.ExcelLoaderType.XlsxHelper;
    internal string CSharpOutputDir => _csOutputDir;
    internal string DataOutputDir => _dataOutputDir;
    internal IReadOnlyList<string> XlsxFiles => _xlsxFiles;
#endregion // Properties

#region Public Methods
    public void SetOutputDirectory(string csOutputDir, string dataOutputDir)
    {
        _csOutputDir = csOutputDir;
        _dataOutputDir = dataOutputDir;
    }
    public void AddExcelFiles(params string[]? files)
    {
        if (files == null || files == Array.Empty<string>())
            return;
        var filtered = files.Where(f => !f.Contains("~$"));
        _xlsxFiles.AddRange(filtered);
    }
#endregion // Public Methods

#region Private Methods
    internal bool Validate() => !string.IsNullOrWhiteSpace(_csOutputDir) && !string.IsNullOrWhiteSpace(_dataOutputDir);
#endregion // Private Methods
}