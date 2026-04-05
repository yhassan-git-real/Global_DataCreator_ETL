using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GlobalDataCreatorETL.Core.Services;
using GlobalDataCreatorETL.UI.Views;
using GlobalDataCreatorETL.UI.ViewModels;
using OfficeOpenXml;
using Serilog;
using System;

namespace GlobalDataCreatorETL;

public partial class App : Application
{
    private ServiceContainer? _serviceContainer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Build all services synchronously — no network calls, no await
            _serviceContainer = new ServiceContainer();
            _serviceContainer.Build();

            // Create ViewModel and Window synchronously
            var mainViewModel = new MainViewModel(_serviceContainer);
            var mainWindow    = new MainWindow { DataContext = mainViewModel };

            desktop.MainWindow = mainWindow;
            desktop.Exit      += OnAppExit;

            // Initialize triggers async DB ping + country load AFTER window is shown
            mainViewModel.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _serviceContainer?.Dispose();
        Log.CloseAndFlush();
    }
}
