using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.Core.Services;
using GlobalDataCreatorETL.UI.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel : ReactiveObject, IDisposable
{
    private readonly ServiceContainer _services;
    private readonly CompositeDisposable _disposables = new();

    public MainViewModel(ServiceContainer services)
    {
        _services = services;

        // Subscribe to status reporter events
        _services.StatusReporter.StepReported += OnStepReported;

        // Subscribe to DB connection changes
        _services.DatabaseConnection.ConnectionChanged += OnConnectionChanged;
        _services.CancellationManager.CancellationRequested += (_, _) =>
            SystemStatus = SystemStatus.Cancelled;

        InitializeCommands();
    }

    public void Initialize()
    {
        InitializeDefaults();
        UpdateConnectionInfo();
        _ = ConnectAndLoadAsync();
    }

    private void OnStepReported(object? sender, EtlStep step)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentStepPhase = step.Phase;
            StatusMessage = step.Detail;
            if (step.RowCount.HasValue) RecordCount = step.RowCount.Value;
            if (step.IsError) SystemStatus = SystemStatus.Failed;
            StatusLogs.Add(new LogEntry
            {
                Time   = step.Timestamp.ToString("HH:mm:ss"),
                Phase  = step.Phase,
                Detail = step.Detail,
                Level  = step.IsError ? LogEntryLevel.Error : LogEntryLevel.Info
            });
        });
    }

    private void OnConnectionChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(UpdateConnectionInfo);
    }

    private void UpdateConnectionInfo()
    {
        var db = _services.DatabaseConnection;
        ConnectionInfo = new ConnectionInfo
        {
            ServerName   = db.ServerName,
            DatabaseName = db.DatabaseName,
            UserName     = db.UserName,
            IsConnected  = db.IsConnected,
            ResponseTimeMs = db.ResponseTimeMs,
            LastChecked  = db.LastChecked
        };
    }

    public void Dispose()
    {
        _services.StatusReporter.StepReported -= OnStepReported;
        _services.DatabaseConnection.ConnectionChanged -= OnConnectionChanged;
        _disposables.Dispose();
    }
}
