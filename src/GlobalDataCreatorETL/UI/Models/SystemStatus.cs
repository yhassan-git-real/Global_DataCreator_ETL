using Avalonia.Media;

namespace GlobalDataCreatorETL.UI.Models;

public enum SystemStatus { Idle, Processing, Completed, Failed, Cancelled }

public static class SystemStatusExtensions
{
    public static string GetStatusMessage(this SystemStatus status) => status switch
    {
        SystemStatus.Idle       => "Ready",
        SystemStatus.Processing => "Processing…",
        SystemStatus.Completed  => "Completed",
        SystemStatus.Failed     => "Failed",
        SystemStatus.Cancelled  => "Cancelled",
        _                       => "Unknown"
    };

    public static string GetStatusColor(this SystemStatus status) => status switch
    {
        SystemStatus.Idle       => "#888888",
        SystemStatus.Processing => "#2196F3",
        SystemStatus.Completed  => "#4CAF50",
        SystemStatus.Failed     => "#F44336",
        SystemStatus.Cancelled  => "#FF9800",
        _                       => "#888888"
    };

    public static IBrush GetStatusBrush(this SystemStatus status) =>
        new SolidColorBrush(Color.Parse(status.GetStatusColor()));
}
