namespace GlobalDataCreatorETL.Core.Configuration.Models;

public sealed class AppSettings
{
    public string OutputFilePath { get; set; } = null!;
    public string LogFilePath { get; set; } = null!;
    public string AppName { get; set; } = null!;
    public string AppVersion { get; set; } = null!;
    public int LargeDatasetThreshold { get; set; }
    public int MaxExcelRows { get; set; }
}
