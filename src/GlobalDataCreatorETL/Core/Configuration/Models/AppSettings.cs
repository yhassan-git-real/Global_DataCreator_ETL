namespace GlobalDataCreatorETL.Core.Configuration.Models;

public sealed class AppSettings
{
    /// <summary>Resolved by ConfigurationCacheService: config.json → appsettings.json → .\Output</summary>
    public string OutputFilePath { get; set; } = "";

    /// <summary>Resolved by ConfigurationCacheService: config.json → appsettings.json → .\Logs</summary>
    public string LogFilePath { get; set; } = "";

    public string AppName    { get; set; } = "Global DataCreator ETL";
    public string AppVersion { get; set; } = "1.0.0";
    public int LargeDatasetThreshold { get; set; } = 50000;
    public int MaxExcelRows          { get; set; } = 1048575;
}
