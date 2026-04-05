using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.UI.Models;
using System.Diagnostics;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel
{
    private async Task ExecuteStartAsync()
    {
        var validation = ValidateInputs();
        if (!validation.IsValid)
        {
            ValidationMessage = validation.FirstError;
            return;
        }

        ValidationMessage = string.Empty;
        IsBusy = true;
        SystemStatus = SystemStatus.Processing;
        StatusMessage = "Starting ETL process…";
        RecordCount = 0;
        ElapsedTime = "00:00:00";

        var ct = _services.CancellationManager.StartNew();
        var sw = Stopwatch.StartNew();

        // Elapsed timer — updates every second on UI thread
        using var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                ElapsedTime = sw.Elapsed.ToString(@"hh\:mm\:ss"));
        timer.Start();

        try
        {
            var request = BuildEtlRequest();
            LastSpName   = request.SpName;
            LastViewName = request.ViewName;

            var result = await Task.Run(() => _services.Orchestrator.RunAsync(request, ct), ct);

            sw.Stop();
            timer.Stop();
            ElapsedTime  = sw.Elapsed.ToString(@"hh\:mm\:ss");
            RecordCount  = result.RowCount;
            OutputFilePath = result.OutputFilePath;

            if (result.Success && result.RowCount > 0)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = $"Done! {result.RowCount:N0} rows → {Path.GetFileName(result.OutputFilePath)}";
            }
            else if (result.RowCount == 0)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = result.ErrorMessage ?? "No data found.";
            }
            else
            {
                SystemStatus  = SystemStatus.Failed;
                StatusMessage = result.ErrorMessage ?? "Operation failed.";
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop(); timer.Stop();
            SystemStatus  = SystemStatus.Cancelled;
            StatusMessage = "Operation cancelled.";
        }
        finally
        {
            _services.CancellationManager.Complete();
            IsBusy = false;
        }
    }

    private void ExecuteCancel()
    {
        StatusMessage = "Cancelling…";
        _services.CancellationManager.Cancel();
    }

    private void ExecuteReset()
    {
        Filter.Reset();
        ValidationMessage = string.Empty;
        SystemStatus  = SystemStatus.Idle;
        StatusMessage = "Ready";
        RecordCount   = 0;
        ElapsedTime   = "00:00:00";
        OutputFilePath = string.Empty;
        StatusLogs.Clear();
    }

    private async Task ExecuteBrowseFolderAsync()
    {
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select Output Folder", AllowMultiple = false });

        if (folders.Count > 0)
            Filter.OutputDirectory = folders[0].Path.LocalPath;
    }

    private EtlRequest BuildEtlRequest() => new()
    {
        CountryId    = SelectedCountry!.Id,
        CountryName  = SelectedCountry.Name,
        Mode         = CurrentMode,
        SpName       = SelectedSP!,
        ViewName     = SelectedView!,
        TableName    = _currentTableName,
        FromMonth    = Filter.FromMonthInt,
        ToMonth      = Filter.ToMonthInt,
        HsCode       = Filter.HsCode,
        Product      = Filter.Product,
        IecCode      = Filter.IecCode,
        CompanyName  = Filter.CompanyName,
        ForeignCountryCode = Filter.ForeignCountryCode,
        ForeignName  = Filter.ForeignName,
        Port         = Filter.Port,
        OutputDirectory = Filter.OutputDirectory,
        UserFileName = Filter.UserFileName
    };
}
