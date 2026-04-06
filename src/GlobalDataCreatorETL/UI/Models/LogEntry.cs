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

    public IBrush DetailForeground => Level == LogEntryLevel.Error ? s_errorFg    : s_infoFg;
    public IBrush PhaseForeground  => Level == LogEntryLevel.Error ? s_errorTagFg : s_infoTagFg;
    public IBrush PhaseBackground  => Level == LogEntryLevel.Error ? s_errorTagBg : s_infoTagBg;

    // Frozen static brushes — allocated once, reused across all rows
    private static readonly IBrush s_errorFg    = new SolidColorBrush(Color.FromRgb(185,  28,  28));
    private static readonly IBrush s_infoFg     = new SolidColorBrush(Color.FromRgb( 30,  41,  59));
    private static readonly IBrush s_errorTagFg = new SolidColorBrush(Color.FromRgb(185,  28,  28));
    private static readonly IBrush s_infoTagFg  = new SolidColorBrush(Color.FromRgb( 37,  99, 235));
    private static readonly IBrush s_errorTagBg = new SolidColorBrush(Color.FromRgb(254, 226, 226));
    private static readonly IBrush s_infoTagBg  = new SolidColorBrush(Color.FromRgb(239, 246, 255));
}
