using GlobalDataCreatorETL.Core.Models;
using Serilog;

namespace GlobalDataCreatorETL.Features.Logging;

public sealed class SuccessLogger
{
    private readonly ILogger _log;

    public SuccessLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "SUCCESS_.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}",
                shared: true)
            .CreateLogger();
    }

    public void LogSuccess(EtlRequest request, string outputFile, long rowCount, TimeSpan duration)
    {
        _log.Information("[SUCCESS]");
        _log.Information("  Country  : {Country}", request.CountryName);
        _log.Information("  Mode     : {Mode}", request.Mode);
        _log.Information("  SP       : {SP}", request.SpName);
        _log.Information("  View     : {View}", request.ViewName);
        _log.Information("  From     : {From} | To: {To}", request.FromMonth, request.ToMonth);
        _log.Information("  File     : {File}", outputFile);
        _log.Information("  Rows     : {Rows:N0}", rowCount);
        _log.Information("  Duration : {Duration:hh\\:mm\\:ss\\.fff}", duration);
    }
}
