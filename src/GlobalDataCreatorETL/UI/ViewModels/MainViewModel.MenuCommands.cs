using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Diagnostics;

namespace GlobalDataCreatorETL.UI.ViewModels;

/// <summary>
/// Menu bar commands: File, View, Settings, About.
/// </summary>
public sealed partial class MainViewModel
{
    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenConfigFolderCommand { get; } =
        ReactiveUI.ReactiveCommand.Create(() =>
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Config");
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        });

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenOutputFolderCommand =>
        ReactiveUI.ReactiveCommand.Create(() =>
        {
            var path = Filter.OutputDirectory;
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        });

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExitCommand =>
        ReactiveUI.ReactiveCommand.Create(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
                app.Shutdown();
        });

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCountriesCommand =>
        ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
        {
            _services.CountryService.InvalidateCache();
            await LoadCountriesAsync();
        });

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenSettingsCommand =>
        ReactiveUI.ReactiveCommand.Create(() =>
        {
            // Placeholder: open settings dialog in a future iteration
            StatusMessage = "Settings dialog coming soon.";
        });

    public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenAboutCommand =>
        ReactiveUI.ReactiveCommand.Create(() =>
        {
            StatusMessage = "Global DataCreator ETL v1.0.0 — Enterprise ETL Desktop Application";
        });

    // Progress value (0–100) for determinate progress bar future use
    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }
}
