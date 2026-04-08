using ReactiveUI;

namespace GlobalDataCreatorETL.UI.ViewModels;

/// <summary>Three possible outcomes shown by ExecutionResultDialog.</summary>
public enum ExecutionDialogStatus { Success, NoData, Error }

public sealed class ExecutionResultDialogViewModel : ReactiveObject
{
    // ── Core state ─────────────────────────────────────────────────────────────
    public ExecutionDialogStatus Status { get; init; }

    public bool IsSuccess => Status == ExecutionDialogStatus.Success;
    public bool IsNoData  => Status == ExecutionDialogStatus.NoData;
    public bool IsError   => Status == ExecutionDialogStatus.Error;

    // ── Success data ───────────────────────────────────────────────────────────
    public int FilesGenerated { get; init; }
    public IReadOnlyList<string> GeneratedFilePaths { get; init; } = [];
    public string OutputDirectory { get; init; } = string.Empty;
    public long TotalRows { get; init; }
    public string Duration { get; init; } = string.Empty;

    // ── Batch summary ──────────────────────────────────────────────────────────
    public int TotalCombinations { get; init; }
    public int SuccessfulRuns { get; init; }
    public int FailedRuns { get; init; }
    public bool IsBatch => TotalCombinations > 1;
    public bool HasFailedRuns => FailedRuns > 0;

    // ── Error / no-data data ───────────────────────────────────────────────────
    public string ErrorMessage { get; init; } = string.Empty;
    public string ErrorDetail  { get; init; } = string.Empty;

    // ── Derived ────────────────────────────────────────────────────────────────
    public string Title => Status switch
    {
        ExecutionDialogStatus.Success => "Execution Completed",
        ExecutionDialogStatus.NoData  => "No Data Found",
        _                             => "Execution Failed"
    };

    public bool HasErrorDetail => !string.IsNullOrWhiteSpace(ErrorDetail);

    public string FileNamesDisplay =>
        GeneratedFilePaths.Count > 0
            ? string.Join("\n", GeneratedFilePaths.Select(Path.GetFileName))
            : "—";

    public string ClipboardText => Status switch
    {
        ExecutionDialogStatus.Success =>
            $"Execution Completed\n" +
            $"Files Generated  : {FilesGenerated}\n" +
            (IsBatch ? $"Successful Runs  : {SuccessfulRuns}/{TotalCombinations}\n" : "") +
            (HasFailedRuns ? $"Failed Runs      : {FailedRuns}\n" : "") +
            $"Output Directory : {OutputDirectory}\n" +
            $"Total Rows       : {TotalRows:N0}\n" +
            $"Duration         : {Duration}\n\n" +
            $"Files:\n{string.Join("\n", GeneratedFilePaths)}",

        ExecutionDialogStatus.NoData =>
            $"No Data Found\n" +
            $"No files were generated for the selected filters.\n" +
            $"Duration : {Duration}",

        _ =>
            $"Execution Failed\n" +
            $"Error   : {ErrorMessage}\n" +
            $"Details : {ErrorDetail}"
    };

    // ── Show/hide expandable details ───────────────────────────────────────────
    private bool _showDetails;
    public bool ShowDetails
    {
        get => _showDetails;
        set => this.RaiseAndSetIfChanged(ref _showDetails, value);
    }
}
