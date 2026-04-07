using GlobalDataCreatorETL.Core.Cancellation;
using GlobalDataCreatorETL.Core.Configuration;
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
    private readonly ConfigurationCacheService _config;

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
        EtlStatusReporter reporter,
        ConfigurationCacheService config)
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
        _config = config;
    }

    /// <summary>
    /// Runs the full ETL batch: iterates all Cartesian combinations of the multi-value
    /// filter lists in <paramref name="inputs"/> and calls <see cref="RunAsync"/> for each.
    /// </summary>
    public async Task<BatchCompletionResult> RunBatchAsync(EtlInputs inputs, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        int filesGenerated = 0, failedCount = 0;
        long totalRows = 0;
        string? lastOutputFilePath = null;
        int combinationIndex = 0;
        int total = inputs.TotalCombinations;

        if (total > 1)
            _reporter.ReportPhase("BATCH_START", $"Batch initiated — {total} filter combinations queued for processing");

        try
        {
            foreach (var hsCode in inputs.HsCodes)
            foreach (var product in inputs.Products)
            foreach (var iec in inputs.IecCodes)
            foreach (var company in inputs.CompanyNames)
            foreach (var forCountry in inputs.ForeignCountryCodes)
            foreach (var forName in inputs.ForeignNames)
            foreach (var port in inputs.Ports)
            {
                ct.ThrowIfCancellationRequested();
                combinationIndex++;

                if (total > 1)
                {
                    var activeFilters = new List<string>();
                    if (hsCode     != "%") activeFilters.Add($"HS: {hsCode}");
                    if (product    != "%") activeFilters.Add($"Product: {product}");
                    if (iec        != "%") activeFilters.Add($"IEC: {iec}");
                    if (company    != "%") activeFilters.Add($"Company: {company}");
                    if (forCountry != "%") activeFilters.Add($"ForeignCountry: {forCountry}");
                    if (forName    != "%") activeFilters.Add($"ForeignName: {forName}");
                    if (port       != "%") activeFilters.Add($"Port: {port}");
                    var filterDesc = activeFilters.Count > 0
                        ? string.Join("  |  ", activeFilters)
                        : "All filters: wildcard (%)";
                    _reporter.ReportPhase("COMBINATION", $"[{combinationIndex}/{total}]  {filterDesc}");
                }

                var request = new EtlRequest
                {
                    CountryId          = inputs.CountryId,
                    CountryName        = inputs.CountryName,
                    Mode               = inputs.Mode,
                    SpName             = inputs.SpName,
                    ViewName           = inputs.ViewName,
                    TableName          = inputs.TableName,
                    FromMonth          = inputs.FromMonth,
                    ToMonth            = inputs.ToMonth,
                    HsCode             = hsCode == "%" ? null : hsCode,
                    Product            = product == "%" ? null : product,
                    IecCode            = iec == "%" ? null : iec,
                    CompanyName        = company == "%" ? null : company,
                    ForeignCountryCode = forCountry == "%" ? null : forCountry,
                    ForeignName        = forName == "%" ? null : forName,
                    Port               = port == "%" ? null : port,
                    OutputDirectory    = inputs.OutputDirectory,
                    UserFileName       = total == 1 ? inputs.UserFileName : null
                };

                try
                {
                    var result = await RunAsync(request, ct);
                    if (result.Success && result.RowCount > 0)
                    {
                        filesGenerated++;
                        totalRows += result.RowCount;
                        lastOutputFilePath = result.OutputFilePath;
                    }
                    else if (!result.Success && result.RowCount == 0 &&
                             result.ErrorMessage != "No data found for the selected filters.")
                    {
                        failedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _errorLogger.LogError(ex, "BATCH", $"HS={hsCode}, Product={product}: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return new BatchCompletionResult
            {
                FilesGenerated    = filesGenerated,
                TotalCombinations = total,
                TotalRows         = totalRows,
                FailedCount       = failedCount,
                Cancelled         = true,
                Duration          = sw.Elapsed,
                LastOutputFilePath = lastOutputFilePath,
                ErrorMessage      = "Operation cancelled by user."
            };
        }

        sw.Stop();

        if (total > 1)
            _reporter.ReportPhase("BATCH_DONE",
                $"Batch complete — {filesGenerated}/{total} file(s) generated, {failedCount} failed, {totalRows:N0} total rows exported");

        return new BatchCompletionResult
        {
            FilesGenerated    = filesGenerated,
            TotalCombinations = total,
            TotalRows         = totalRows,
            FailedCount       = failedCount,
            Cancelled         = false,
            Duration          = sw.Elapsed,
            LastOutputFilePath = lastOutputFilePath
        };
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
            _reporter.ReportPhase("PREPARING", $"Building SP parameters — target: {request.SpName} [{request.Mode} mode]");
            var spParams = _paramBuilder.Build(request);
            _executionLogger.LogStart(request);

            // 3. Execute SP
            _reporter.ReportPhase("EXECUTING_SP", $"Executing stored procedure: {request.SpName}");
            var spSw = Stopwatch.StartNew();
            if (isExport)
                await _exportData.ExecuteSpAsync(request.SpName, spParams, ct);
            else
                await _importData.ExecuteSpAsync(request.SpName, spParams, ct);
            spSw.Stop();
            _executionLogger.LogSpExecuted(request.SpName, spSw.ElapsedMilliseconds);
            _reporter.ReportPhase("SP_DONE", $"Stored procedure completed in {spSw.ElapsedMilliseconds}ms — view data ready");

            ct.ThrowIfCancellationRequested();

            // 4. Row count pre-check
            _reporter.ReportPhase("COUNTING", $"Querying row count from view: {request.ViewName}");
            long rowCount = isExport
                ? await _exportData.GetRowCountAsync(request.ViewName, ct)
                : await _importData.GetRowCountAsync(request.ViewName, ct);
            _executionLogger.LogRowsRead(rowCount);
            _reporter.ReportPhase("COUNTING", $"View has {rowCount:N0} rows available for export", rowCount);

            if (rowCount == 0)
            {
                _reporter.ReportPhase("NO_DATA", $"No data found in {request.ViewName} for the selected filters — file not generated");
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

            // 5. Read column schema — use ViewName (mode-aware) not TableName
            _reporter.ReportPhase("SCHEMA", $"Reading column schema from INFORMATION_SCHEMA — view: {request.ViewName}");
            var schema = await _schemaReader.GetColumnInfoAsync(request.ViewName, ct);

            ct.ThrowIfCancellationRequested();

            // 6. Resolve output file name — route to mode-specific subdirectory (names from config)
            var appSettings = _config.GetAppSettings();
            var subDir = request.Mode.Equals("Export", StringComparison.OrdinalIgnoreCase)
                ? appSettings.ExportSubDirectory
                : appSettings.ImportSubDirectory;
            var resolvedOutputDir = Path.Combine(request.OutputDirectory, subDir);
            Directory.CreateDirectory(resolvedOutputDir);
            var fileName = _fileNameService.Resolve(request, resolvedOutputDir);
            outputFilePath = Path.Combine(resolvedOutputDir, fileName);

            // 7. Stream view → Excel
            _reporter.ReportPhase("READING_DATA", $"Opening data stream — {rowCount:N0} rows from {request.ViewName}");
            SqlConnection? conn = null;
            SqlDataReader? reader = null;
            try
            {
                if (isExport)
                    (conn, reader) = await _exportData.GetDataReaderAsync(request.ViewName, ct);
                else
                    (conn, reader) = await _importData.GetDataReaderAsync(request.ViewName, ct);

                _reporter.ReportPhase("GENERATING_EXCEL", $"Building Excel workbook — {rowCount:N0} rows x {schema.Count} columns");
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
            _reporter.ReportPhase("DONE", $"File saved: {fileName}  [{rowCount:N0} rows | {schema.Count} cols | {sw.Elapsed.TotalSeconds:F1}s]", rowCount);

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
            _reporter.ReportPhase("CANCELLED", "Operation cancelled by user — partial file removed");
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
