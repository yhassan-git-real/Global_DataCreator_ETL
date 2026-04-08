using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.UI.Models;
using GlobalDataCreatorETL.UI.Views.Dialogs;
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

        // Captured outside try/finally so the dialog is shown AFTER IsBusy = false.
        BatchCompletionResult? pendingDialog = null;
        ExecutionDialogStatus  pendingStatus = ExecutionDialogStatus.Success;

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
                // no popup for user-initiated cancellation
            }
            else if (result.HasAnySuccess)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = result.TotalCombinations == 1
                    ? $"Done! {result.TotalRows:N0} rows → {Path.GetFileName(result.LastOutputFilePath)}"
                    : $"Done! {result.FilesGenerated}/{result.TotalCombinations} files, {result.TotalRows:N0} total rows.";

                pendingDialog = result;
                pendingStatus = ExecutionDialogStatus.Success;
            }
            else if (result.TotalRows == 0 && result.FailedCount == 0)
            {
                SystemStatus  = SystemStatus.Completed;
                StatusMessage = "No data found for the selected filters.";

                pendingDialog = result;
                pendingStatus = ExecutionDialogStatus.NoData;
            }
            else
            {
                SystemStatus  = SystemStatus.Failed;
                StatusMessage = result.ErrorMessage ?? "One or more combinations failed.";

                pendingDialog = result;
                pendingStatus = ExecutionDialogStatus.Error;
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
            // Always clear busy state before the dialog is shown so the
            // UI is fully unblocked when the modal window appears.
            _services.CancellationManager.Complete();
            IsBusy = false;
        }

        // Show the result dialog ONCE, after IsBusy = false, for both
        // single and batch (multi-combination) executions.
        if (pendingDialog is not null)
            await ShowExecutionResultDialogAsync(pendingDialog, pendingStatus);
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

    private static async Task ShowExecutionResultDialogAsync(BatchCompletionResult result, ExecutionDialogStatus status)
    {
        var owner = Avalonia.Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is null) return;

        string outputDir = string.Empty;
        if (!string.IsNullOrEmpty(result.LastOutputFilePath))
            outputDir = Path.GetDirectoryName(result.LastOutputFilePath) ?? string.Empty;

        var vm = new ExecutionResultDialogViewModel
        {
            Status             = status,
            FilesGenerated     = result.FilesGenerated,
            GeneratedFilePaths = result.GeneratedFilePaths,
            OutputDirectory    = outputDir,
            TotalRows          = result.TotalRows,
            Duration           = result.Duration.ToString(@"hh\:mm\:ss"),
            TotalCombinations  = result.TotalCombinations,
            SuccessfulRuns     = result.FilesGenerated,
            FailedRuns         = result.FailedCount,
            ErrorMessage       = result.ErrorMessage ?? "An unexpected error occurred.",
            ErrorDetail        = result.FailedCount > 0
                ? $"{result.FailedCount} combination(s) failed out of {result.TotalCombinations}."
                : string.Empty
        };

        var dialog = new ExecutionResultDialog { DataContext = vm };
        await dialog.ShowDialog(owner);
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
        ForeignCountryCodes = SplitAndTrim(Filter.ForeignCountry),
        ForeignNames       = SplitAndTrim(Filter.ForeignName),
        Ports              = SplitAndTrim(Filter.Port),
        OutputDirectory    = Filter.OutputDirectory,
        UserFileName       = Filter.UserFileName
    };
}

