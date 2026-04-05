using Avalonia.Data.Converters;
using System.Globalization;

namespace GlobalDataCreatorETL.UI.Converters;

/// <summary>
/// Converts between a 1-based month integer (1–12) and a 0-based ComboBox index.
/// </summary>
public sealed class MonthIndexConverter : IValueConverter
{
    public static readonly MonthIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int month && month >= 1 && month <= 12)
            return month - 1;
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
            return index + 1;
        return 1;
    }
}
