using GlobalDataCreatorETL.Core.Cancellation;
using GlobalDataCreatorETL.Core.DataAccess;
using GlobalDataCreatorETL.Core.Database;
using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.Features.Excel;
using GlobalDataCreatorETL.Features.Logging;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace GlobalDataCreatorETL.Core.Services;

/// <summary>
/// Orchestrates the full ETL pipeline:
/// Validate → Resolve CountryMeta → Build Parameters
/// → Execute SP → Read Schema → Stream View → Generate Excel → Log Result
/// </summary>
public sealed class EtlOrchestrator
{
    private readonly ValidationService _validation;
    private readonly ParameterBuilderService _paramBuilder;
    private readonly FileNameService _fileNameService;
    private readonly ExportDataAccess _exportData;
    private readonly ImportDataAccess _importData;
    private readonly ColumnSchemaReader _schemaReader;
    private readonly ExcelReportService _excelService;
    private readonly ExecutionLogger _executionLogger;
    private readonly ErrorLogger _errorLogger;
    private readonly SuccessLogger _successLogger;
    private readonly EtlStatusReporter _reporter;

    public EtlOrchestrator(
        ValidationService validation,
        ParameterBuilderService paramBuilder,
        FileNameService fileNameService,
        ExportDataAccess exportData,
        ImportDataAccess importData,
        ColumnSchemaReader schemaReader,
        ExcelReportService excelService,
        ExecutionLogger executionLogger,
        ErrorLogger errorLogger,
        SuccessLogger successLogger,
        EtlStatusReporter reporter)
    {
        _validation = validation;
        _paramBuilder = paramBuilder;
        _fileNameService = fileNameService;
        _exportData = exportData;
        _importData = importData;
        _schemaReader = schemaReader;
        _excelService = excelService;
        _executionLogger = executionLogger;
        _errorLogger = errorLogger;
        _successLogger = successLogger;
        _reporter = reporter;
    }

    public async Task<CompletionResult> RunAsync(EtlRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1. Validate
        var validation = _validation.ValidateRequest(request);
        if (!validation.IsValid)
        {
            _reporter.ReportError("VALIDATION", string.Join("; ", validation.Errors));
            return Fail(request, validation.FirstError, sw.Elapsed);
        }

        string? outputFilePath = null;

        try
        {
            var isExport = request.Mode.Equals("Export", StringComparison.OrdinalIgnoreCase);

            // 2. Build SP parameters
            _reporter.ReportPhase("PREPARING", "Building parameters…");
            var spParams = _paramBuilder.Build(request);
            _executionLogger.LogStart(request);

            // 3. Execute SP
            _reporter.ReportPhase("EXECUTING_SP", $"Executing {request.SpName}…");
            var spSw = Stopwatch.StartNew();
            if (isExport)
                await _exportData.ExecuteSpAsync(request.SpName, spParams, ct);
            else
                await _importData.ExecuteSpAsync(request.SpName, spParams, ct);
            spSw.Stop();
            _executionLogger.LogSpExecuted(request.SpName, spSw.ElapsedMilliseconds);
            _reporter.ReportPhase("SP_DONE", $"SP completed in {spSw.ElapsedMilliseconds}ms");

            ct.ThrowIfCancellationRequested();

            // 4. Row count pre-check
            _reporter.ReportPhase("COUNTING", "Counting rows…");
            long rowCount = isExport
                ? await _exportData.GetRowCountAsync(request.ViewName, ct)
                : await _importData.GetRowCountAsync(request.ViewName, ct);
            _executionLogger.LogRowsRead(rowCount);
            _reporter.ReportPhase("COUNTING", $"Row count: {rowCount:N0}", rowCount);

            if (rowCount == 0)
            {
                _reporter.ReportPhase("NO_DATA", "No data returned. File not generated.");
                return new CompletionResult
                {
                    Success = true,
                    RowCount = 0,
                    Duration = sw.Elapsed,
                    CountryName = request.CountryName,
                    Mode = request.Mode,
                    SpName = request.SpName,
                    ViewName = request.ViewName,
                    ErrorMessage = "No data found for the selected filters."
                };
            }

            if (rowCount > 1_048_575)
            {
                var msg = $"Row count ({rowCount:N0}) exceeds Excel limit of 1,048,575. Narrow your filters.";
                _reporter.ReportError("ROW_LIMIT", msg);
                return Fail(request, msg, sw.Elapsed);
            }

            // 5. Read column schema
            _reporter.ReportPhase("SCHEMA", $"Reading column schema for {request.TableName}…");
            var schema = await _schemaReader.GetColumnInfoAsync(request.TableName, ct);

            ct.ThrowIfCancellationRequested();

            // 6. Resolve output file name
            var fileName = _fileNameService.Resolve(request);
            outputFilePath = Path.Combine(request.OutputDirectory, fileName);

            // 7. Stream view → Excel
            _reporter.ReportPhase("READING_DATA", $"Reading data from {request.ViewName}…");
            SqlConnection? conn = null;
            SqlDataReader? reader = null;
            try
            {
                if (isExport)
                    (conn, reader) = await _exportData.GetDataReaderAsync(request.ViewName, ct);
                else
                    (conn, reader) = await _importData.GetDataReaderAsync(request.ViewName, ct);

                _reporter.ReportPhase("GENERATING_EXCEL", "Generating Excel file…");
                await _excelService.GenerateAsync(reader, rowCount, schema, request, outputFilePath, ct);
            }
            finally
            {
                reader?.Dispose();
                conn?.Dispose();
            }

            sw.Stop();
            _executionLogger.LogFileSaved(outputFilePath);
            _successLogger.LogSuccess(request, outputFilePath, rowCount, sw.Elapsed);
            _reporter.ReportPhase("DONE", $"File saved: {fileName}", rowCount);

            return new CompletionResult
            {
                Success = true,
                OutputFilePath = outputFilePath,
                RowCount = rowCount,
                Duration = sw.Elapsed,
                CountryName = request.CountryName,
                Mode = request.Mode,
                SpName = request.SpName,
                ViewName = request.ViewName
            };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            SafeDeletePartialFile(outputFilePath);
            _reporter.ReportPhase("CANCELLED", "Operation was cancelled.");
            return Fail(request, "Operation cancelled by user.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            SafeDeletePartialFile(outputFilePath);
            var userMsg = BuildUserFriendlyMessage(ex);
            _errorLogger.LogError(ex, "ETL_PIPELINE", userMsg);
            _reporter.ReportError("ERROR", userMsg);
            return Fail(request, userMsg, sw.Elapsed);
        }
    }

    private static CompletionResult Fail(EtlRequest request, string error, TimeSpan duration) =>
        new()
        {
            Success = false,
            Duration = duration,
            CountryName = request.CountryName,
            Mode = request.Mode,
            SpName = request.SpName,
            ViewName = request.ViewName,
            ErrorMessage = error
        };

    private static void SafeDeletePartialFile(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Move to Temp subfolder if delete fails
            try
            {
                var temp = Path.Combine(Path.GetDirectoryName(path)!, "Temp");
                Directory.CreateDirectory(temp);
                File.Move(path, Path.Combine(temp, Path.GetFileName(path)), overwrite: true);
            }
            catch { /* best effort */ }
        }
    }

    private static string BuildUserFriendlyMessage(Exception ex) => ex switch
    {
        SqlException sqlEx when sqlEx.Number == -2 =>
            "Database query timed out. Try a narrower date range or check server load.",
        SqlException sqlEx when sqlEx.Message.Contains("linked server", StringComparison.OrdinalIgnoreCase) =>
            "Linked server is unavailable. Check server connectivity and try again.",
        SqlException =>
            $"Database error: {ex.Message}",
        IOException =>
            $"File system error: {ex.Message}",
        OperationCanceledException =>
            "Operation was cancelled.",
        _ =>
            $"An unexpected error occurred: {ex.Message}"
    };
}
