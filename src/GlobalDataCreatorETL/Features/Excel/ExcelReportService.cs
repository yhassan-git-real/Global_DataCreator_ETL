using GlobalDataCreatorETL.Core.Configuration;
using GlobalDataCreatorETL.Core.Database;
using GlobalDataCreatorETL.Core.Models;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;

namespace GlobalDataCreatorETL.Features.Excel;

/// <summary>
/// Main Excel generation entry point.
/// Coordinates data loading, type mapping, formatting, and saving.
/// </summary>
public sealed class ExcelReportService
{
    private readonly WorksheetDataLoader _dataLoader;
    private readonly ColumnTypeMapper _typeMapper;
    private readonly ExcelFormattingService _formatting;
    private readonly ExcelSaveService _saveService;
    private readonly ConfigurationCacheService _config;
    private readonly SchemaExtractor _schemaExtractor;

    public ExcelReportService(
        WorksheetDataLoader dataLoader,
        ColumnTypeMapper typeMapper,
        ExcelFormattingService formatting,
        ExcelSaveService saveService,
        ConfigurationCacheService config,
        SchemaExtractor schemaExtractor)
    {
        _dataLoader = dataLoader;
        _typeMapper = typeMapper;
        _formatting = formatting;
        _saveService = saveService;
        _config = config;
        _schemaExtractor = schemaExtractor;
    }

    public async Task GenerateAsync(
        SqlDataReader reader,
        long expectedRowCount,
        IReadOnlyList<ColumnInfo> schema,
        EtlRequest request,
        string outputFilePath,
        CancellationToken ct)
    {
        var formatSettings = _config.GetExcelFormatSettings();
        var worksheetName = BuildWorksheetName(request);

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add(worksheetName);

        // Load data (peek at schema from reader during first read)
        long actualRowCount = await _dataLoader.LoadFromReaderAsync(ws, reader, ct);
        ct.ThrowIfCancellationRequested();

        // Extract the actual schema from the reader results
        // This ensures we use the exact column structure from the view, not the table
        var actualSchema = _schemaExtractor.ExtractFromDataReader(reader);
        var columnTypeMap = actualSchema.Count > 0 
            ? _typeMapper.MapColumns(actualSchema) 
            : _typeMapper.MapColumns(schema); // Fallback to provided schema if extraction fails

        // Apply formatting
        _formatting.Apply(ws, columnTypeMap, formatSettings);
        ct.ThrowIfCancellationRequested();

        // Sort data rows by configured column positions (final step — after formatting)
        _formatting.ApplySort(ws, formatSettings, columnTypeMap.TotalColumns);
        ct.ThrowIfCancellationRequested();

        // Save
        await _saveService.SaveAsync(pkg, outputFilePath, actualRowCount);
    }

    private static string BuildWorksheetName(EtlRequest request)
    {
        // Worksheet name max 31 chars, no special chars
        var name = $"{request.CountryName} {request.Mode}";
        return name.Length > 31 ? name[..31] : name;
    }
}
