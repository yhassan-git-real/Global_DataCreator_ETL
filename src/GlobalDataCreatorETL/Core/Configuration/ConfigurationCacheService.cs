using GlobalDataCreatorETL.Core.Configuration.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace GlobalDataCreatorETL.Core.Configuration;

/// <summary>
/// Thread-safe, lazy-loading configuration cache.
/// All config files are read once and stored in memory.
/// Call Invalidate(key) to force reload on next access.
/// </summary>
public sealed class ConfigurationCacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly string _configBasePath;

    private const string DbKey = "db";
    private const string AppKey = "app";
    private const string ExcelKey = "excel";

    public ConfigurationCacheService(string? configBasePath = null)
    {
        _configBasePath = configBasePath ?? Path.Combine(AppContext.BaseDirectory, "Config");
    }

    public DbSettings GetDbSettings() =>
        (DbSettings)_cache.GetOrAdd(DbKey, _ => Load<DbSettings>("dbsettings.json"));

    public AppSettings GetAppSettings() =>
        (AppSettings)_cache.GetOrAdd(AppKey, _ => Load<AppSettings>("appsettings.json"));

    public ExcelFormatSettings GetExcelFormatSettings() =>
        (ExcelFormatSettings)_cache.GetOrAdd(ExcelKey, _ => Load<ExcelFormatSettings>("excelformatting.json"));

    public void Invalidate(string key) => _cache.TryRemove(key, out _);

    public void InvalidateAll()
    {
        _cache.TryRemove(DbKey, out _);
        _cache.TryRemove(AppKey, out _);
        _cache.TryRemove(ExcelKey, out _);
    }

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
