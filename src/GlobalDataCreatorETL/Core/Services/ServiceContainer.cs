using GlobalDataCreatorETL.Core.Cancellation;
using GlobalDataCreatorETL.Core.Configuration;
using GlobalDataCreatorETL.Core.DataAccess;
using GlobalDataCreatorETL.Core.Database;
using GlobalDataCreatorETL.Core.Services;
using GlobalDataCreatorETL.Features.Excel;
using GlobalDataCreatorETL.Features.Logging;

namespace GlobalDataCreatorETL.Core.Services;

/// <summary>
/// Manually assembles all application services in dependency order.
/// Build() is fully synchronous — window can show immediately.
/// ConnectAsync() does the DB ping and is called from the ViewModel after the window is visible.
/// </summary>
public sealed class ServiceContainer : IDisposable
{
    // Infrastructure
    public ConfigurationCacheService Config { get; private set; } = null!;
    public DatabaseConnectionService DatabaseConnection { get; private set; } = null!;

    // Database
    public DatabaseObjectValidator DbValidator { get; private set; } = null!;
    public ColumnSchemaReader SchemaReader { get; private set; } = null!;
    public SchemaExtractor SchemaExtractor { get; private set; } = null!;
    public CountryRepository CountryRepo { get; private set; } = null!;
    public ExportDataAccess ExportData { get; private set; } = null!;
    public ImportDataAccess ImportData { get; private set; } = null!;

    // Services
    public CountryService CountryService { get; private set; } = null!;
    public ValidationService ValidationService { get; private set; } = null!;
    public ParameterBuilderService ParameterBuilder { get; private set; } = null!;
    public FileNameService FileNameService { get; private set; } = null!;
    public EtlStatusReporter StatusReporter { get; private set; } = null!;
    public CancellationManager CancellationManager { get; private set; } = null!;

    // Excel
    public ExcelReportService ExcelService { get; private set; } = null!;

    // Logging
    public ExecutionLogger ExecutionLogger { get; private set; } = null!;
    public ErrorLogger ErrorLogger { get; private set; } = null!;
    public SuccessLogger SuccessLogger { get; private set; } = null!;
    public LogParserService LogParser { get; private set; } = null!;

    // Orchestrator
    public EtlOrchestrator Orchestrator { get; private set; } = null!;

    /// <summary>
    /// Synchronously creates all service instances.
    /// No network calls — safe to call before showing the window.
    /// </summary>
    public void Build()
    {
        // 1. Config
        Config = new ConfigurationCacheService();

        var appSettings = Config.GetAppSettings();
        var logDir    = appSettings.LogFilePath;
        var outputDir = appSettings.OutputFilePath;

        // Ensure required directories exist on startup
        if (!string.IsNullOrWhiteSpace(logDir))
            Directory.CreateDirectory(logDir);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        // 2. Logging
        ExecutionLogger = new ExecutionLogger(logDir);
        ErrorLogger     = new ErrorLogger(logDir);
        SuccessLogger   = new SuccessLogger(logDir);
        LogParser       = new LogParserService();

        // 3. Database connection (object only — no ping yet)
        DatabaseConnection = new DatabaseConnectionService(Config);

        // 4. Data access
        DbValidator  = new DatabaseObjectValidator(DatabaseConnection);
        SchemaReader = new ColumnSchemaReader(DatabaseConnection);
        SchemaExtractor = new SchemaExtractor();
        CountryRepo  = new CountryRepository(DatabaseConnection);
        ExportData   = new ExportDataAccess(DatabaseConnection, Config);
        ImportData   = new ImportDataAccess(DatabaseConnection, Config);

        // 5. Services
        CountryService      = new CountryService(CountryRepo);
        ValidationService   = new ValidationService();
        ParameterBuilder    = new ParameterBuilderService();
        FileNameService     = new FileNameService();
        StatusReporter      = new EtlStatusReporter();
        CancellationManager = new CancellationManager();

        // 6. Excel
        ExcelService = new ExcelReportService(
            new WorksheetDataLoader(),
            new ColumnTypeMapper(),
            new ExcelFormattingService(),
            new ExcelSaveService(),
            Config,
            SchemaExtractor);

        // 7. Orchestrator
        Orchestrator = new EtlOrchestrator(
            ValidationService,
            ParameterBuilder,
            FileNameService,
            ExportData,
            ImportData,
            SchemaReader,
            ExcelService,
            ExecutionLogger,
            ErrorLogger,
            SuccessLogger,
            StatusReporter);
    }

    /// <summary>
    /// Async DB ping + background monitor start.
    /// Called from ViewModel.Initialize() after the window is visible.
    /// </summary>
    public async Task ConnectAsync()
    {
        var db = Config.GetDbSettings();
        DatabaseConnection.SetServerInfo(db);
        await DatabaseConnection.InitializeAsync();
    }

    public void Dispose()
    {
        DatabaseConnection?.Dispose();
    }
}
