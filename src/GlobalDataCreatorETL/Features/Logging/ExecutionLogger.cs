using GlobalDataCreatorETL.Core.Models;
using Serilog;
using Serilog.Core;

namespace GlobalDataCreatorETL.Features.Logging;

/// <summary>
/// Logs step-by-step ETL execution events to the execution log file.
/// </summary>
public sealed class ExecutionLogger
{
    private readonly ILogger _log;

    public ExecutionLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "EXECUTION_.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                shared: true)
            .CreateLogger();
    }

    public void LogStart(EtlRequest request) =>
        _log.Information("[START] Country={Country} | Mode={Mode} | SP={SP} | View={View} | From={From} | To={To}",
            request.CountryName, request.Mode, request.SpName, request.ViewName,
            request.FromMonth, request.ToMonth);

    public void LogSpExecuted(string spName, long durationMs) =>
        _log.Information("[SP_EXEC] {SP} completed in {Ms}ms", spName, durationMs);

    public void LogViewQueried(string viewName) =>
        _log.Information("[VIEW] Querying {View}", viewName);

    public void LogRowsRead(long count) =>
        _log.Information("[DATA] RowCount={Count}", count);

    public void LogFileSaved(string filePath) =>
        _log.Information("[FILE] Saved: {Path}", filePath);
}
