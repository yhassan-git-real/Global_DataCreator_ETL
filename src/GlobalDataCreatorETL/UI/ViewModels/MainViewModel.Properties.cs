using GlobalDataCreatorETL.Core.Models;
using GlobalDataCreatorETL.UI.Models;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel
{
    // ── Busy / Status ──────────────────────────────────────────────────────────
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private SystemStatus _systemStatus = SystemStatus.Idle;
    public SystemStatus SystemStatus
    {
        get => _systemStatus;
        set
        {
            this.RaiseAndSetIfChanged(ref _systemStatus, value);
            this.RaisePropertyChanged(nameof(SystemStatusMessage));
            this.RaisePropertyChanged(nameof(SystemStatusColor));
        }
    }
    public string SystemStatusMessage => _systemStatus.GetStatusMessage();
    public string SystemStatusColor   => _systemStatus.GetStatusColor();

    private string _currentStepPhase = string.Empty;
    public string CurrentStepPhase
    {
        get => _currentStepPhase;
        set => this.RaiseAndSetIfChanged(ref _currentStepPhase, value);
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private long _recordCount;
    public long RecordCount
    {
        get => _recordCount;
        set => this.RaiseAndSetIfChanged(ref _recordCount, value);
    }

    private string _elapsedTime = "00:00:00";
    public string ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    private string _outputFilePath = string.Empty;
    public string OutputFilePath
    {
        get => _outputFilePath;
        set => this.RaiseAndSetIfChanged(ref _outputFilePath, value);
    }

    // ── Country / Mode ─────────────────────────────────────────────────────────
    private string _currentMode = "Export";
    public string CurrentMode
    {
        get => _currentMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentMode, value);
            this.RaisePropertyChanged(nameof(CompanyLabel));
            this.RaisePropertyChanged(nameof(IsExportMode));
            this.RaisePropertyChanged(nameof(IsImportMode));
            if (SelectedCountry is not null)
                UpdateDbSelections();
        }
    }

    public string CompanyLabel => _currentMode == "Export" ? "Export Company" : "Import Company";

    public bool IsExportMode
    {
        get => _currentMode == "Export";
        set { if (value) CurrentMode = "Export"; }
    }

    public bool IsImportMode
    {
        get => _currentMode == "Import";
        set { if (value) CurrentMode = "Import"; }
    }

    private ObservableCollection<CountryDto> _countries = new();
    public ObservableCollection<CountryDto> Countries
    {
        get => _countries;
        set => this.RaiseAndSetIfChanged(ref _countries, value);
    }

    private CountryDto? _selectedCountry;
    public CountryDto? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCountry, value);
            if (value is not null)
                _ = LoadCountryMetaAsync(value.Id);
        }
    }

    // ── DB Objects ─────────────────────────────────────────────────────────────
    private string? _selectedView;
    public string? SelectedView
    {
        get => _selectedView;
        set => this.RaiseAndSetIfChanged(ref _selectedView, value);
    }

    private string? _selectedSp;
    public string? SelectedSP
    {
        get => _selectedSp;
        set => this.RaiseAndSetIfChanged(ref _selectedSp, value);
    }

    private string _currentTableName = string.Empty;

    // ── Filter ─────────────────────────────────────────────────────────────────
    private EtlFilter _filter = new();
    public EtlFilter Filter
    {
        get => _filter;
        set => this.RaiseAndSetIfChanged(ref _filter, value);
    }

    // ── Validation ─────────────────────────────────────────────────────────────
    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _validationMessage, value);
            this.RaisePropertyChanged(nameof(HasValidationError));
        }
    }
    public bool HasValidationError => !string.IsNullOrEmpty(_validationMessage);

    // ── Connection ─────────────────────────────────────────────────────────────
    private ConnectionInfo _connectionInfo = new();
    public ConnectionInfo ConnectionInfo
    {
        get => _connectionInfo;
        set => this.RaiseAndSetIfChanged(ref _connectionInfo, value);
    }

    // ── Execution Summary ──────────────────────────────────────────────────────
    private string _lastSpName = string.Empty;
    public string LastSpName
    {
        get => _lastSpName;
        set => this.RaiseAndSetIfChanged(ref _lastSpName, value);
    }

    private string _lastViewName = string.Empty;
    public string LastViewName
    {
        get => _lastViewName;
        set => this.RaiseAndSetIfChanged(ref _lastViewName, value);
    }

    // ── Status Log ─────────────────────────────────────────────────────────────
    public ObservableCollection<LogEntry> StatusLogs { get; } = new();
}
