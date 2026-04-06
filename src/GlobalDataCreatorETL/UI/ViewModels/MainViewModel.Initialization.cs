using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.UI.Models;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel
{
    private Core.Models.CountryMeta? _currentCountryMeta = null;

    private void InitializeDefaults()
    {
        var appSettings = _services.Config.GetAppSettings();
        Filter.OutputDirectory = appSettings.OutputFilePath;
        CurrentMode = "Export";
    }

    /// <summary>
    /// Fires after the window is visible. Pings DB then loads countries.
    /// Any failure shows a message in the status bar — never crashes the app.
    /// </summary>
    private async Task ConnectAndLoadAsync()
    {
        try
        {
            StatusMessage = "Connecting to SQL Server...";
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DB", Detail = "Connecting to SQL Server (Matrix,1434) — database: Process" });
            await _services.ConnectAsync();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateConnectionInfo);
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DB", Detail = "Connection established — SQL Server online and responsive" });
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB connection failed: {ex.Message}";
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DB", Detail = $"Connection failed: {ex.Message}", Level = LogEntryLevel.Error });
        }

        await LoadCountriesAsync();
    }

    private async Task LoadCountriesAsync()
    {
        try
        {
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DATA", Detail = "Loading active countries from dbo.mst_country" });
            var list = await _services.CountryService.GetAllActiveCountriesAsync();
            _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Countries.Clear();
                foreach (var c in list)
                    Countries.Add(c);

                if (Countries.Count > 0)
                    SelectedCountry = Countries[0];

                StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DATA", Detail = $"Loaded {Countries.Count} active countries — application is ready" });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load countries: {ex.Message}";
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DATA", Detail = $"Failed to load countries: {ex.Message}", Level = LogEntryLevel.Error });
        }
    }

    private async Task LoadCountryMetaAsync(int countryId)
    {
        try
        {
            var meta = await _services.CountryService.GetCountryMetaAsync(countryId);
            if (meta is null) return;

            _currentCountryMeta = meta;  // Store metadata for mode changes
            _currentTableName = meta.TableName;
            UpdateDbSelections(meta);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load country data: {ex.Message}";
            StatusLogs.Add(new LogEntry { Time = DateTime.Now.ToString("HH:mm:ss"), Phase = "DATA", Detail = $"Failed to load country data: {ex.Message}", Level = LogEntryLevel.Error });
        }
    }

    private void UpdateDbSelections(Core.Models.CountryMeta? meta = null)
    {
        // Use stored metadata if not provided (when mode changes)
        meta ??= _currentCountryMeta;
        
        if (meta is null) return;

        if (CurrentMode.Equals("Export", StringComparison.OrdinalIgnoreCase))
        {
            SelectedView = meta.ExportView;
            SelectedSP   = meta.ExportSP;
        }
        else
        {
            SelectedView = meta.ImportView;
            SelectedSP   = meta.ImportSP;
        }
    }
}
