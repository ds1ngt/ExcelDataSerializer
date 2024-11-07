using System;
using System.Reflection;
using ExcelDataSerializer;
using ExcelDataSerializerUI.Models;
using ReactiveUI;
using Logger = ExcelDataSerializerUI.Util.Logger;

namespace ExcelDataSerializerUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    private string _excelPath;
    private string _savePath;
    private string _csOutputPath;
    private string _dataOutputPath;
    private SettingInfo? _settingInfo;

    private readonly Logger _log = new();
    private readonly Logger _errorLog = new();

    private string _logStr = string.Empty;
    private string _errorStr = string.Empty;

    private string _version;
    private string _libraryVersion;
    private bool _isBusy;
    public MainWindowViewModel()
    {
        UpdateVersionStr();
        LoadSettingInfo();
    }

    private void UpdateVersionStr()
    {
        try
        {
            _version = Assembly.GetAssembly(typeof(MainWindowViewModel)).GetName().Version.ToString();
            _libraryVersion = Assembly.GetAssembly(typeof(Runner)).GetName().Version.ToString();
        }
        catch (Exception _)
        {
            _version = string.Empty;
            _libraryVersion = string.Empty;
        }
    }
#region Binding Property
    public string Title => $"ExcelDataSerializerUI v{_version}, (Core: v{_libraryVersion})";
    public string ExcelPath
    {
        get => _excelPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _excelPath, value);
            SettingInfo.ExcelPath = _excelPath;
            SettingInfo.Save();
        }
    }

    public string SavePath
    {
        get => _savePath;
        set => this.RaiseAndSetIfChanged(ref _savePath, value);
    }

    public string CsOutputPath
    {
        get => _csOutputPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _csOutputPath, value);
            SettingInfo.CsOutputPath = _csOutputPath;
            SettingInfo.Save();
        }
    }

    public string DataOutputPath
    {
        get => _dataOutputPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _dataOutputPath, value);
            SettingInfo.DataOutputPath = _dataOutputPath;
            SettingInfo.Save();
        }
    }

    public string Log
    {
        get => _logStr;
        set => this.RaiseAndSetIfChanged(ref _logStr, value);
    }

    public string ErrorLog
    {
        get => _errorStr;
        set => this.RaiseAndSetIfChanged(ref _errorStr, value);
    }
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
#endregion // Binding Property

#region Setting Info

    private SettingInfo SettingInfo => _settingInfo ??= SettingInfo.Load();

    private void LoadSettingInfo()
    {
        ExcelPath = SettingInfo.ExcelPath;
        CsOutputPath = SettingInfo.CsOutputPath;
        DataOutputPath = SettingInfo.DataOutputPath;
    }
#endregion // Setting Info

#region Log
    public void ClearLog()
    {
        _log.Clear();
        RefreshLogStr();
    }

    public void AppendLog(string msg)
    {
        _log.AppendLog(msg);
        RefreshLogStr();
    }

    public void AppendLogLine(string msg)
    {
        _log.AppendLogLine(msg);
        RefreshLogStr();
    }
    private void RefreshLogStr() => Log = _log.Message;
#endregion // Log

#region ErrorLog
    public void ClearErrorLog()
    {
        _errorLog.Clear();
        RefreshErrorLogStr();
    }

    public void AppendErrorLog(string msg)
    {
        _errorLog.AppendLog(msg);
        RefreshErrorLogStr();
    }

    public void AppendErrorLogLine(string msg)
    {
        _errorLog.AppendLogLine(msg);
        RefreshErrorLogStr();
    }
    private void RefreshErrorLogStr() => ErrorLog = _errorLog.Message;
#endregion // ErrorLog
#pragma warning restore CA1822 // Mark members as static
}