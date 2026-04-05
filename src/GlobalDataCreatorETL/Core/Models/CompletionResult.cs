namespace GlobalDataCreatorETL.Core.Models;

public sealed class CompletionResult
{
    public bool Success { get; init; }
    public string OutputFilePath { get; init; } = string.Empty;
    public long RowCount { get; init; }
    public TimeSpan Duration { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string SpName { get; init; } = string.Empty;
    public string ViewName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}
