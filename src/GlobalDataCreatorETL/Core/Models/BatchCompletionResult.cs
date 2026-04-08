namespace GlobalDataCreatorETL.Core.Models;

public sealed class BatchCompletionResult
{
    public int FilesGenerated { get; init; }
    public int TotalCombinations { get; init; }
    public long TotalRows { get; init; }
    public int FailedCount { get; init; }
    public bool Cancelled { get; init; }
    public TimeSpan Duration { get; init; }
    public string? LastOutputFilePath { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> GeneratedFilePaths { get; init; } = [];

    public bool HasAnySuccess => FilesGenerated > 0;
}
