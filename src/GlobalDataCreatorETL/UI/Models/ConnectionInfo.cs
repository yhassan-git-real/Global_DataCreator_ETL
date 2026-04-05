namespace GlobalDataCreatorETL.UI.Models;

public sealed class ConnectionInfo
{
    public string ServerName { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public long ResponseTimeMs { get; init; }
    public DateTime LastChecked { get; init; }

    public string StatusMessage => IsConnected
        ? $"Connected ({ResponseTimeMs}ms)"
        : "Disconnected";

    public string StatusColor => IsConnected ? "#4CAF50" : "#F44336";
}
