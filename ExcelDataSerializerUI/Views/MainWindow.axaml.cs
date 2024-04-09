using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using ExcelDataSerializerUI.ViewModels;

namespace ExcelDataSerializerUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnOpenExcelFolder(object? sender, RoutedEventArgs e)
    {
        var path = string.Empty;
        OpenFolder("Excel 경로 선택", ref path);

        if (DataContext is not MainWindowViewModel vm)
            return;
        vm.ExcelPath = path;
    }

    private void OnOpenSaveFolder(object? sender, RoutedEventArgs e)
    {
        var path = string.Empty;
        OpenFolder("저장 경로 선택", ref path);

        if (DataContext is not MainWindowViewModel vm)
            return;
        vm.SavePath = path;
    }

    private void OnOpenCsOutputFolder(object? sender, RoutedEventArgs e)
    {
        var path = string.Empty;
        OpenFolder("C# 저장 경로 선택", ref path);

        if (DataContext is not MainWindowViewModel vm)
            return;
        vm.CsOutputPath = path;
    }
    private void OnOpenDataOutputFolder(object? sender, RoutedEventArgs e)
    {
        var path = string.Empty;
        OpenFolder("Data 저장 경로 선택", ref path);

        if (DataContext is not MainWindowViewModel vm)
            return;
        vm.DataOutputPath = path;
    }
    private void OpenFolder(string title, ref string path)
    {
        var result = StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        }).Result;

        if (result.Count == 0)
            return;
        
        path = result[0].Path.AbsolutePath;
    }

    private void OnExecute(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (!Validate())
            return;

        vm.ClearLog();

        Logger.Instance.OnLog -= OnLog;
        Logger.Instance.OnLog += OnLog;

        var excelFiles = Directory.GetFiles(vm.ExcelPath, "*.xlsx");
        var csOutput = vm.CsOutputPath;
        var dataOutput = vm.DataOutputPath;
        
        var runnerInfo = new RunnerInfo();
        runnerInfo.AddExcelFiles(excelFiles);
        runnerInfo.SetOutputDirectory(csOutput, dataOutput);
        
        ExcelDataSerializer.Runner.Execute(runnerInfo);
    }

    private void OnLog(string msg, bool lineBreak)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (lineBreak)
            vm.AppendLogLine(msg);
        else
            vm.AppendLog(msg);
    }

    private bool Validate()
    {
        if (DataContext is not MainWindowViewModel vm)
            return false;

        if (string.IsNullOrWhiteSpace(vm.ExcelPath) || string.IsNullOrWhiteSpace(vm.CsOutputPath) || string.IsNullOrWhiteSpace(vm.DataOutputPath))
            return false;

        if (!Directory.Exists(vm.ExcelPath))
            return false;

        return true;
    }
}