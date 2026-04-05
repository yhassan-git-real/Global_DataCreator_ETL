namespace GlobalDataCreatorETL.Features.Logging;

public sealed class ExecutionSummary
{
    public bool IsSuccess { get; init; }
    public string StatusLine { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long RowCount { get; init; }
    public string Duration { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Parses the most recent SUCCESS or ERROR log file to provide a
/// summary for the bottom status panel.
/// </summary>
public sealed class LogParserService
{
    public async Task<ExecutionSummary?> GetLatestSummaryAsync(string logDirectory)
    {
        if (!Directory.Exists(logDirectory))
            return null;

        // Try success log first, then error log
        var summary = await TryParseLatestAsync(logDirectory, "SUCCESS_") ??
                      await TryParseLatestAsync(logDirectory, "ERROR_");
        return summary;
    }

    private static async Task<ExecutionSummary?> TryParseLatestAsync(string dir, string prefix)
    {
        var files = Directory.GetFiles(dir, $"{prefix}*.txt")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .Take(1)
            .ToArray();

        if (files.Length == 0) return null;

        try
        {
            var lines = await File.ReadAllLinesAsync(files[0]);
            return ParseLines(lines, prefix.StartsWith("SUCCESS"));
        }
        catch
        {
            return null;
        }
    }

    private static ExecutionSummary ParseLines(string[] lines, bool isSuccess)
    {
        string file = string.Empty, rows = string.Empty, duration = string.Empty;

        foreach (var line in lines)
        {
            if (line.Contains("File     :")) file = line.Split(':').Last().Trim();
            if (line.Contains("Rows     :")) rows = line.Split(':').Last().Trim();
            if (line.Contains("Duration :")) duration = line.Split(':').Last().Trim();
        }

        _ = long.TryParse(rows.Replace(",", ""), out var rowCount);

        return new ExecutionSummary
        {
            IsSuccess = isSuccess,
            StatusLine = isSuccess ? "Completed" : "Failed",
            FilePath = file,
            RowCount = rowCount,
            Duration = duration,
            Timestamp = DateTime.Now
        };
    }
}
