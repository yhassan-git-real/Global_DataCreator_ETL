using GlobalDataCreatorETL.Core.Models;

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
            StatusMessage = "Connecting to database…";
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] • Connecting to database…");
            await _services.ConnectAsync();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateConnectionInfo);
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] • Connected successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB connection failed: {ex.Message}";
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✗ DB connection failed: {ex.Message}");
        }

        await LoadCountriesAsync();
    }

    private async Task LoadCountriesAsync()
    {
        try
        {
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] • Loading countries…");
            var list = await _services.CountryService.GetAllActiveCountriesAsync();
            _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Countries.Clear();
                foreach (var c in list)
                    Countries.Add(c);

                if (Countries.Count > 0)
                    SelectedCountry = Countries[0];

                StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] • Loaded {Countries.Count} countries");
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load countries: {ex.Message}";
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✗ Failed to load countries: {ex.Message}");
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
            StatusLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✗ Failed to load country data: {ex.Message}");
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
