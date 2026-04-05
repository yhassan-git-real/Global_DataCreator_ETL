namespace GlobalDataCreatorETL.Core.Models;

public sealed class EtlStep
{
    public required string Phase { get; init; }
    public required string Detail { get; init; }
    public long? RowCount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public bool IsError { get; init; }
    public string? ErrorMessage { get; init; }
}
