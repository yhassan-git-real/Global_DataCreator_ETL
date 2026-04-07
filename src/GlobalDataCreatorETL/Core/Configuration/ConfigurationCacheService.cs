using GlobalDataCreatorETL.Core.Configuration.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace GlobalDataCreatorETL.Core.Configuration;

/// <summary>
/// Thread-safe, lazy-loading configuration cache.
///
/// Resolution order (highest priority first):
///   1. config.json  — sits next to the .exe; user-editable machine overrides
///   2. Config/appsettings.json + Config/dbsettings.json  — project-level defaults
///   3. Built-in defaults  — paths resolve to .\Output and .\Logs next to the exe
///
/// Call Invalidate(key) or InvalidateAll() to force reload on next access.
/// </summary>
public sealed class ConfigurationCacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly string _configBasePath;
    private readonly string _appBaseDir;

    private const string DbKey    = "db";
    private const string AppKey   = "app";
    private const string ExcelKey = "excel";
    private const string RootKey  = "root";

    public ConfigurationCacheService(string? configBasePath = null)
    {
        _appBaseDir    = AppContext.BaseDirectory;
        _configBasePath = configBasePath ?? Path.Combine(_appBaseDir, "Config");
    }

    // ── Public accessors ──────────────────────────────────────────────────────

    public DbSettings GetDbSettings() =>
        (DbSettings)_cache.GetOrAdd(DbKey, _ => LoadDbSettingsMerged());

    public AppSettings GetAppSettings() =>
        (AppSettings)_cache.GetOrAdd(AppKey, _ => LoadAppSettingsMerged());

    public ExcelFormatSettings GetExcelFormatSettings() =>
        (ExcelFormatSettings)_cache.GetOrAdd(ExcelKey, _ => Load<ExcelFormatSettings>("excelformatting.json"));

    /// <summary>Returns the parsed root config.json (empty defaults if file absent).</summary>
    public RootConfig GetRootConfig() =>
        (RootConfig)_cache.GetOrAdd(RootKey, _ => LoadRootConfig());

    // ── Invalidation ─────────────────────────────────────────────────────────

    public void Invalidate(string key) => _cache.TryRemove(key, out _);

    public void InvalidateAll()
    {
        _cache.TryRemove(DbKey,    out _);
        _cache.TryRemove(AppKey,   out _);
        _cache.TryRemove(ExcelKey, out _);
        _cache.TryRemove(RootKey,  out _);
    }

    // ── Merge loaders ────────────────────────────────────────────────────────

    private RootConfig LoadRootConfig()
    {
        var rootConfigFile = Path.Combine(_appBaseDir, "config.json");

        if (!File.Exists(rootConfigFile))
            return new RootConfig();

        var config = new ConfigurationBuilder()
            .SetBasePath(_appBaseDir)
            .AddJsonFile("config.json", optional: true, reloadOnChange: false)
            .Build();

        return config.Get<RootConfig>() ?? new RootConfig();
    }

    private AppSettings LoadAppSettingsMerged()
    {
        var settings = Load<AppSettings>("appsettings.json");
        var root     = GetRootConfig();

        // --- Paths: root config overrides appsettings.json ---
        if (!string.IsNullOrWhiteSpace(root.Paths.OutputDirectory))
            settings.OutputFilePath = root.Paths.OutputDirectory;

        if (!string.IsNullOrWhiteSpace(root.Paths.LogDirectory))
            settings.LogFilePath = root.Paths.LogDirectory;

        // --- Application thresholds ---
        if (root.Application.LargeDatasetThreshold.HasValue)
            settings.LargeDatasetThreshold = root.Application.LargeDatasetThreshold.Value;

        if (root.Application.MaxExcelRows.HasValue)
            settings.MaxExcelRows = root.Application.MaxExcelRows.Value;

        if (!string.IsNullOrWhiteSpace(root.Application.ExportSubDirectory))
            settings.ExportSubDirectory = root.Application.ExportSubDirectory;

        if (!string.IsNullOrWhiteSpace(root.Application.ImportSubDirectory))
            settings.ImportSubDirectory = root.Application.ImportSubDirectory;

        // --- Built-in fallback: relative to the exe ---
        if (string.IsNullOrWhiteSpace(settings.OutputFilePath))
            settings.OutputFilePath = Path.Combine(_appBaseDir, "Output");

        if (string.IsNullOrWhiteSpace(settings.LogFilePath))
            settings.LogFilePath = Path.Combine(_appBaseDir, "Logs");

        return settings;
    }

    private DbSettings LoadDbSettingsMerged()
    {
        var settings = Load<DbSettings>("dbsettings.json");
        var db       = GetRootConfig().Database;

        // Override only when root config provides a non-empty value
        if (!string.IsNullOrWhiteSpace(db.ServerName))    settings.ServerName    = db.ServerName;
        if (!string.IsNullOrWhiteSpace(db.DatabaseName))  settings.DatabaseName  = db.DatabaseName;
        if (!string.IsNullOrWhiteSpace(db.AuthMode))      settings.AuthMode      = db.AuthMode;
        if (!string.IsNullOrWhiteSpace(db.Username))      settings.Username      = db.Username;
        if (!string.IsNullOrWhiteSpace(db.Password))      settings.Password      = db.Password;

        if (db.CommandTimeoutSeconds.HasValue)     settings.CommandTimeoutSeconds     = db.CommandTimeoutSeconds.Value;
        if (db.ConnectionTimeoutSeconds.HasValue)  settings.ConnectionTimeoutSeconds  = db.ConnectionTimeoutSeconds.Value;
        if (db.MonitoringIntervalMinutes.HasValue) settings.MonitoringIntervalMinutes = db.MonitoringIntervalMinutes.Value;

        return settings;
    }

    // ── Generic file loader ───────────────────────────────────────────────────

    private T Load<T>(string fileName) where T : new()
    {
        var filePath = Path.Combine(_configBasePath, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var config = new ConfigurationBuilder()
            .SetBasePath(_configBasePath)
            .AddJsonFile(fileName, optional: false, reloadOnChange: false)
            .Build();

        return config.Get<T>() ?? new T();
    }
}
