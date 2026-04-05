using Serilog;

namespace GlobalDataCreatorETL.Features.Logging;

public sealed class ErrorLogger
{
    private readonly ILogger _log;

    public ErrorLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _log = new LoggerConfiguration()
            .MinimumLevel.Error()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "ERROR_.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                shared: true)
            .CreateLogger();
    }

    public void LogError(Exception ex, string context, string userFriendlyMessage)
    {
        _log.Error("[CONTEXT] Phase={Context}", context);
        _log.Error("[USER_MSG] {Message}", userFriendlyMessage);
        _log.Error(ex, "[EXCEPT]");
    }
}
