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
            var inputs = BuildEtlInputs();
            LastSpName   = inputs.SpName;
            LastViewName = inputs.ViewName;

            var result = await Task.Run(() => _services.Orchestrator.RunBatchAsync(inputs, ct), ct);

            sw.Stop();
            timer.Stop();
            ElapsedTime    = sw.Elapsed.ToString(@"hh\:mm\:ss");
            RecordCount    = result.TotalRows;
            OutputFilePath = result.LastOutputFilePath ?? string.Empty;

            if (result.Cancelled)
            {
                SystemStatus  = SystemStatus.Cancelled;
                StatusMessage = "Operation cancelled.";
            }
            else if (result.HasAnySuccess)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = result.TotalCombinations == 1
                    ? $"Done! {result.TotalRows:N0} rows → {Path.GetFileName(result.LastOutputFilePath)}"
                    : $"Done! {result.FilesGenerated}/{result.TotalCombinations} files, {result.TotalRows:N0} total rows.";
            }
            else if (result.TotalRows == 0 && result.FailedCount == 0)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = "No data found for the selected filters.";
            }
            else
            {
                SystemStatus  = SystemStatus.Failed;
                StatusMessage = result.ErrorMessage ?? "One or more combinations failed.";
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

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits a comma-separated input string into trimmed scalar values.
    /// Blank or whitespace inputs return ["%"] (wildcard = all).
    /// </summary>
    private static List<string> SplitAndTrim(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ["%"];

        var values = input
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count > 0 ? values : ["%"];
    }

    private EtlInputs BuildEtlInputs() => new()
    {
        CountryId    = SelectedCountry!.Id,
        CountryName  = SelectedCountry.Name,
        Mode         = CurrentMode,
        SpName       = SelectedSP!,
        ViewName     = SelectedView!,
        TableName    = _currentTableName,
        FromMonth    = Filter.FromMonthInt,
        ToMonth      = Filter.ToMonthInt,
        HsCodes            = SplitAndTrim(Filter.HsCode),
        Products           = SplitAndTrim(Filter.Product),
        IecCodes           = SplitAndTrim(Filter.IecCode),
        CompanyNames       = SplitAndTrim(Filter.CompanyName),
        ForeignCountryCodes = SplitAndTrim(Filter.ForeignCountryCode),
        ForeignNames       = SplitAndTrim(Filter.ForeignName),
        Ports              = SplitAndTrim(Filter.Port),
        OutputDirectory    = Filter.OutputDirectory,
        UserFileName       = Filter.UserFileName
    };
}

