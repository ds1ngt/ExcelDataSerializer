namespace ExcelDataSerializer.Model;

public class RunnerInfo
{
    private string _outputDir = string.Empty;
    private readonly List<string> _xlsxFiles = new List<string>();


#region Properties
    internal string OutputDir => _outputDir;
    internal IReadOnlyList<string> XlsxFiles => _xlsxFiles;
#endregion // Properties

#region Public Methods
    public void SetOutputDirectory(string outputDir) => _outputDir = outputDir;
    public void AddExcelFiles(params string[]? files)
    {
        if (files == null || files == Array.Empty<string>())
            return;

        _xlsxFiles.AddRange(files);
    }
#endregion // Public Methods

#region Private Methods
    internal bool Validate()
    {
        if (string.IsNullOrWhiteSpace(_outputDir))
            return false;
        return true;
    }
#endregion // Private Methods
}