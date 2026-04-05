namespace GlobalDataCreatorETL.Core.Configuration.Models;

public sealed class DbSettings
{
    public string ServerName { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string AuthMode { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string MasterTableName { get; set; } = null!;
    public int CommandTimeoutSeconds { get; set; }
    public int ConnectionTimeoutSeconds { get; set; }
    public int MonitoringIntervalMinutes { get; set; }

    public string BuildConnectionString()
    {
        if (AuthMode.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return $"Server={ServerName};Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        return $"Server={ServerName};Database={DatabaseName};User Id={Username};Password={Password};TrustServerCertificate=True;";
    }
}
