using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cysharp.Threading.Tasks;
using ExcelDataSerializer.Model;
using ExcelDataSerializer.Util;
using ExcelDataSerializerUI.ViewModels;

namespace ExcelDataSerializerUI.Views;

public partial class MainWindow : Window
{
    private struct LogMessageInfo
    {
        public string Message;
        public bool LineBreak;
    }
    
    private UniTask<Task>? _workTask;
    private UniTask<Task>? _uiInvokeTask;
    private Queue<LogMessageInfo> _infoQueue = new();
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnOpenExcelFolder(object? sender, RoutedEventArgs e)
    {
        _ = OpenFolderAsync("Excel 경로 선택", path => {
            if (DataContext is not MainWindowViewModel vm)
                return;
            vm.ExcelPath = path;
        });
    }

    private void OnOpenSaveFolder(object? sender, RoutedEventArgs e)
    {
        _ = OpenFolderAsync("저장 경로 선택", path => {
            if (DataContext is not MainWindowViewModel vm)
                return;
            vm.SavePath = path;
        });
    }

    private void OnOpenCsOutputFolder(object? sender, RoutedEventArgs e)
    {
        _ = OpenFolderAsync("C# 저장 경로 선택", path => {
            if (DataContext is not MainWindowViewModel vm)
                return;
            vm.CsOutputPath = path;
        });
    }
    private void OnOpenDataOutputFolder(object? sender, RoutedEventArgs e)
    {
        _ = OpenFolderAsync("Data 저장 경로 선택", path =>
        {
            if (DataContext is not MainWindowViewModel vm)
                return;
            vm.DataOutputPath = path;
        });
    }
    private async Task<string> OpenFolderAsync(string title, Action<string> onComplete)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        var path = result.Count > 0 ? Uri.UnescapeDataString(result[0].Path.AbsolutePath): string.Empty;
        onComplete?.Invoke(path);
        return path;
    }

    private async void OnExecute(object? sender, RoutedEventArgs e)
    {
        if (_workTask.HasValue)
            return;

        if (DataContext is not MainWindowViewModel vm)
            return;

        if (!Validate())
            return;

        vm.ClearLog();

        Logger.Instance.OnLog -= OnLog;
        Logger.Instance.OnLog += OnLog;

        var excelFiles = Directory.GetFiles(vm.ExcelPath, "*.xlsx", SearchOption.AllDirectories);
        var csOutput = vm.CsOutputPath;
        var dataOutput = vm.DataOutputPath;

        var runnerInfo = new RunnerInfo();
        runnerInfo.AddExcelFiles(excelFiles);
        runnerInfo.SetOutputDirectory(csOutput, dataOutput);

        _workTask = UniTask.Run(async () =>
        {
            await ExcelDataSerializer.Runner.ExecuteAsync(runnerInfo);
            _workTask = null;
        });
        _uiInvokeTask ??= UniTask.Run(async () => await UpdateUiAsync());
    }

    private async UniTask UpdateUiAsync()
    {
        while (true)
        {
            if (_infoQueue.TryPeek(out var message))
            {
                Dispatcher.UIThread.Invoke(() => OnLogMessage(message.Message, message.LineBreak));
                _infoQueue.Dequeue();
            }
            await UniTask.Yield();
        }
    }
    
    private void OnLog(string msg, bool lineBreak)
    {
        _infoQueue.Enqueue(new LogMessageInfo { Message = msg, LineBreak = lineBreak});
        // UniTask.Run(() =>
        // {
        //     Dispatcher.UIThread.Invoke(() => OnLogMessage(msg, lineBreak));
        // });
    }

    private void OnLogMessage(string msg, bool lineBreak)
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