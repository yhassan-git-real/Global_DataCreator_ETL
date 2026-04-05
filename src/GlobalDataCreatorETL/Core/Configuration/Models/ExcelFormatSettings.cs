namespace GlobalDataCreatorETL.Core.Configuration.Models;

public sealed class ExcelFormatSettings
{
    public string FontName { get; set; } = null!;
    public float FontSize { get; set; }
    public string FontColor { get; set; } = null!;
    public string HeaderFontName { get; set; } = null!;
    public float HeaderFontSize { get; set; }
    public string HeaderBackgroundColor { get; set; } = null!;
    public string HeaderFontColor { get; set; } = null!;
    public string BorderStyle { get; set; } = null!;
    public int AutoFitSampleRows { get; set; }
    public int AutoFitSampleRowsLarge { get; set; }
    public bool AutoFitColumns { get; set; }
    public int LargeDatasetThreshold { get; set; }
    public string DateFormat { get; set; } = null!;
    public bool WrapText { get; set; }
    public bool FreezeTopRow { get; set; }
    public double HeaderRowHeight { get; set; }
    public string HeaderHorizontalAlignment { get; set; } = null!;
    public string HeaderVerticalAlignment { get; set; } = null!;
}
