namespace GlobalDataCreatorETL.Core.Configuration.Models;

/// <summary>
/// Loaded from config.json at the application root (next to the .exe).
/// Non-empty values override the per-section Config/*.json defaults.
/// Empty strings → app resolves sensible relative defaults automatically.
/// </summary>
public sealed class RootConfig
{
    public RootPathsConfig      Paths       { get; set; } = new();
    public RootDatabaseConfig   Database    { get; set; } = new();
    public RootApplicationConfig Application { get; set; } = new();
}

/// <summary>Paths section — where output files and logs are written.</summary>
public sealed class RootPathsConfig
{
    /// <summary>
    /// Directory where generated Excel files are saved.
    /// Leave empty to default to .\Output  (next to the exe).
    /// </summary>
    public string OutputDirectory { get; set; } = "";

    /// <summary>
    /// Directory where EXECUTION_*.txt / SUCCESS_*.txt / ERROR_*.txt are written.
    /// Leave empty to default to .\Logs  (next to the exe).
    /// </summary>
    public string LogDirectory { get; set; } = "";
}

/// <summary>Database section — connection settings that override dbsettings.json.</summary>
public sealed class RootDatabaseConfig
{
    public string ServerName   { get; set; } = "";
    public string DatabaseName { get; set; } = "";

    /// <summary>"Windows" or "SqlAuth"</summary>
    public string AuthMode  { get; set; } = "";
    public string Username  { get; set; } = "";
    public string Password  { get; set; } = "";

    /// <summary>null = keep the dbsettings.json value.</summary>
    public int? CommandTimeoutSeconds    { get; set; }
    public int? ConnectionTimeoutSeconds  { get; set; }
    public int? MonitoringIntervalMinutes { get; set; }
}

/// <summary>Application section — runtime thresholds that override appsettings.json.</summary>
public sealed class RootApplicationConfig
{
    /// <summary>null = keep the appsettings.json value.</summary>
    public int? LargeDatasetThreshold { get; set; }
    public int? MaxExcelRows          { get; set; }

    /// <summary>Empty = keep the appsettings.json value (default: "Export").</summary>
    public string ExportSubDirectory { get; set; } = "";

    /// <summary>Empty = keep the appsettings.json value (default: "Import").</summary>
    public string ImportSubDirectory { get; set; } = "";
}
