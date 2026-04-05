using GlobalDataCreatorETL.Core.Configuration;
using Microsoft.Data.SqlClient;
using System.ComponentModel;

namespace GlobalDataCreatorETL.Core.Database;

/// <summary>
/// Singleton connection manager. SetServerInfo() populates display fields synchronously.
/// InitializeAsync() does the first DB ping and starts background monitoring.
/// </summary>
public sealed class DatabaseConnectionService : INotifyPropertyChanged, IDisposable
{
    private readonly ConfigurationCacheService _config;
    private readonly object _lock = new();
    private Timer? _monitorTimer;
    private bool _isPaused;
    private bool _disposed;

    private bool _isConnected;
    private string _serverName = string.Empty;
    private string _databaseName = string.Empty;
    private string _userName = string.Empty;
    private long _responseTimeMs;
    private DateTime _lastChecked = DateTime.MinValue;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ConnectionChanged;

    public bool IsConnected
    {
        get => _isConnected;
        private set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); ConnectionChanged?.Invoke(this, EventArgs.Empty); }
    }

    public string ServerName   { get => _serverName;    private set { _serverName = value;    OnPropertyChanged(nameof(ServerName)); } }
    public string DatabaseName { get => _databaseName;  private set { _databaseName = value;  OnPropertyChanged(nameof(DatabaseName)); } }
    public string UserName     { get => _userName;      private set { _userName = value;      OnPropertyChanged(nameof(UserName)); } }
    public long ResponseTimeMs { get => _responseTimeMs; private set { _responseTimeMs = value; OnPropertyChanged(nameof(ResponseTimeMs)); } }
    public DateTime LastChecked { get => _lastChecked;  private set { _lastChecked = value;   OnPropertyChanged(nameof(LastChecked)); } }

    public DatabaseConnectionService(ConfigurationCacheService config)
    {
        _config = config;
    }

    /// <summary>
    /// Synchronously populates server/db/user display fields from config.
    /// No network call — safe to call before showing the window.
    /// </summary>
    public void SetServerInfo(Core.Configuration.Models.DbSettings db)
    {
        ServerName   = db.ServerName;
        DatabaseName = db.DatabaseName;
        UserName     = db.AuthMode.Equals("Windows", StringComparison.OrdinalIgnoreCase)
                       ? "Windows Auth"
                       : db.Username;
    }

    /// <summary>
    /// Performs the first DB ping and starts background monitoring.
    /// Called after the window is visible.
    /// </summary>
    public async Task InitializeAsync()
    {
        var db = _config.GetDbSettings();
        await TestConnectionAsync(db.ConnectionTimeoutSeconds);
        StartMonitoring(db.MonitoringIntervalMinutes);
    }

    public async Task<bool> TestConnectionAsync(int timeoutSeconds = 5)
    {
        var connStr = _config.GetDbSettings().BuildConnectionString();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(connStr);
            var openTask = conn.OpenAsync();
            if (await Task.WhenAny(openTask, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))) != openTask)
                throw new TimeoutException("Connection timed out.");
            await openTask;
            sw.Stop();
            ResponseTimeMs = sw.ElapsedMilliseconds;
            LastChecked    = DateTime.Now;
            IsConnected    = true;
            return true;
        }
        catch
        {
            sw.Stop();
            ResponseTimeMs = sw.ElapsedMilliseconds;
            LastChecked    = DateTime.Now;
            IsConnected    = false;
            return false;
        }
    }

    public void Pause()  { lock (_lock) _isPaused = true; }
    public void Resume() { lock (_lock) _isPaused = false; }

    public string BuildConnectionString() => _config.GetDbSettings().BuildConnectionString();

    private void StartMonitoring(int intervalMinutes)
    {
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _monitorTimer = new Timer(async _ =>
        {
            lock (_lock) { if (_isPaused || _disposed) return; }
            await TestConnectionAsync();
        }, null, TimeSpan.FromSeconds(30), interval);
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _disposed = true;
        _monitorTimer?.Dispose();
    }
}
