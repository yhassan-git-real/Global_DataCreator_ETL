using ReactiveUI;

namespace GlobalDataCreatorETL.UI.Models;

/// <summary>
/// Reactive model holding all user-input filter fields bound to the left panel.
/// </summary>
public sealed class EtlFilter : ReactiveObject
{
    private int _fromYear = DateTime.Now.Year;
    private int _fromMonth = DateTime.Now.Month;
    private int _toYear = DateTime.Now.Year;
    private int _toMonth = DateTime.Now.Month;
    private string _hsCode = string.Empty;
    private string _product = string.Empty;
    private string _iecCode = string.Empty;
    private string _companyName = string.Empty;
    private string _foreignCountry = string.Empty;
    private string _foreignName = string.Empty;
    private string _port = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _userFileName = string.Empty;

    public int FromYear         { get => _fromYear;            set => this.RaiseAndSetIfChanged(ref _fromYear, value); }
    public int FromMonth        { get => _fromMonth;           set => this.RaiseAndSetIfChanged(ref _fromMonth, value); }
    public int ToYear           { get => _toYear;              set => this.RaiseAndSetIfChanged(ref _toYear, value); }
    public int ToMonth          { get => _toMonth;             set => this.RaiseAndSetIfChanged(ref _toMonth, value); }
    public string HsCode        { get => _hsCode;              set => this.RaiseAndSetIfChanged(ref _hsCode, value); }
    public string Product       { get => _product;             set => this.RaiseAndSetIfChanged(ref _product, value); }
    public string IecCode       { get => _iecCode;             set => this.RaiseAndSetIfChanged(ref _iecCode, value); }
    public string CompanyName   { get => _companyName;         set => this.RaiseAndSetIfChanged(ref _companyName, value); }
    public string ForeignCountry { get => _foreignCountry; set => this.RaiseAndSetIfChanged(ref _foreignCountry, value); }
    public string ForeignName   { get => _foreignName;         set => this.RaiseAndSetIfChanged(ref _foreignName, value); }
    public string Port          { get => _port;                set => this.RaiseAndSetIfChanged(ref _port, value); }
    public string OutputDirectory { get => _outputDirectory;   set => this.RaiseAndSetIfChanged(ref _outputDirectory, value); }
    public string UserFileName  { get => _userFileName;        set => this.RaiseAndSetIfChanged(ref _userFileName, value); }

    /// <summary>Returns FromMonth as YYYYMM integer.</summary>
    public int FromMonthInt => FromYear * 100 + FromMonth;

    /// <summary>Returns ToMonth as YYYYMM integer.</summary>
    public int ToMonthInt => ToYear * 100 + ToMonth;

    public void Reset()
    {
        HsCode = Product = IecCode = CompanyName
            = ForeignCountry = ForeignName = Port
            = UserFileName = string.Empty;
    }
}
