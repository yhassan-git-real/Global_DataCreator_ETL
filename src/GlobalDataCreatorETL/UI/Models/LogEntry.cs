using Avalonia.Media;

namespace GlobalDataCreatorETL.UI.Models;

public enum LogEntryLevel { Info, Error }

/// <summary>
/// A single structured row in the on-screen execution log.
/// Brush properties are computed here so the DataTemplate needs no converters.
/// </summary>
public sealed record LogEntry
{
    public required string Time   { get; init; }
    public string          Phase  { get; init; } = string.Empty;
    public required string Detail { get; init; }
    public LogEntryLevel   Level  { get; init; } = LogEntryLevel.Info;

    // ── Computed brushes (UI model — Avalonia dependency is intentional) ──────

    public IBrush DetailForeground => Level == LogEntryLevel.Error ? s_errorFg : s_infoFg;
    public IBrush PhaseForeground  => Level == LogEntryLevel.Error ? s_errorTagFg : PhaseTagFg(Phase);
    public IBrush PhaseBackground  => Level == LogEntryLevel.Error ? s_errorTagBg : PhaseTagBg(Phase);

    // ── Per-phase tag colours (fg / bg pairs per semantic group) ─────────────

    // DB / Batch — steel blue
    private static readonly IBrush s_blueTagFg   = new SolidColorBrush(Color.FromRgb( 90, 170, 240)); // #5AAAF0
    private static readonly IBrush s_blueTagBg   = new SolidColorBrush(Color.FromRgb( 10,  38,  72)); // #0A2648

    // Data / Schema / Counting — cyan-teal
    private static readonly IBrush s_cyanTagFg   = new SolidColorBrush(Color.FromRgb( 48, 192, 200)); // #30C0C8
    private static readonly IBrush s_cyanTagBg   = new SolidColorBrush(Color.FromRgb(  6,  38,  48)); // #062630

    // Preparing / Executing / SP — indigo-purple
    private static readonly IBrush s_indigoTagFg = new SolidColorBrush(Color.FromRgb(148, 128, 248)); // #9480F8
    private static readonly IBrush s_indigoTagBg = new SolidColorBrush(Color.FromRgb( 30,  18,  80)); // #1E1250

    // Done / Excel — emerald green
    private static readonly IBrush s_greenTagFg  = new SolidColorBrush(Color.FromRgb( 56, 200,  96)); // #38C860
    private static readonly IBrush s_greenTagBg  = new SolidColorBrush(Color.FromRgb(  8,  42,  22)); // #082A16

    // Cancelled / No-data — amber
    private static readonly IBrush s_amberTagFg  = new SolidColorBrush(Color.FromRgb(220, 156,  20)); // #DC9C14
    private static readonly IBrush s_amberTagBg  = new SolidColorBrush(Color.FromRgb( 46,  30,   6)); // #2E1E06

    // Error / Validation — red
    private static readonly IBrush s_errorFg     = new SolidColorBrush(Color.FromRgb(255, 145, 145)); // #FF9191
    private static readonly IBrush s_errorTagFg  = new SolidColorBrush(Color.FromRgb(255, 130, 130)); // #FF8282
    private static readonly IBrush s_errorTagBg  = new SolidColorBrush(Color.FromRgb( 64,  14,  14)); // #400E0E

    // Info detail text
    private static readonly IBrush s_infoFg      = new SolidColorBrush(Color.FromRgb(195, 218, 238)); // #C3DAEE

    // Default fallback
    private static readonly IBrush s_defaultTagFg = new SolidColorBrush(Color.FromRgb(176, 200, 224)); // #B0C8E0
    private static readonly IBrush s_defaultTagBg = new SolidColorBrush(Color.FromRgb( 18,  32,  52)); // #122034

    private static IBrush PhaseTagFg(string phase) => phase switch
    {
        "DB" or "BATCH_START" or "COMBINATION" or "BATCH_DONE"   => s_blueTagFg,
        "DATA" or "SCHEMA" or "COUNTING" or "READING_DATA"        => s_cyanTagFg,
        "PREPARING" or "EXECUTING_SP" or "SP_DONE"                => s_indigoTagFg,
        "DONE" or "GENERATING_EXCEL"                              => s_greenTagFg,
        "CANCELLED" or "NO_DATA"                                   => s_amberTagFg,
        _                                                          => s_defaultTagFg,
    };

    private static IBrush PhaseTagBg(string phase) => phase switch
    {
        "DB" or "BATCH_START" or "COMBINATION" or "BATCH_DONE"   => s_blueTagBg,
        "DATA" or "SCHEMA" or "COUNTING" or "READING_DATA"        => s_cyanTagBg,
        "PREPARING" or "EXECUTING_SP" or "SP_DONE"                => s_indigoTagBg,
        "DONE" or "GENERATING_EXCEL"                              => s_greenTagBg,
        "CANCELLED" or "NO_DATA"                                   => s_amberTagBg,
        _                                                          => s_defaultTagBg,
    };
}
