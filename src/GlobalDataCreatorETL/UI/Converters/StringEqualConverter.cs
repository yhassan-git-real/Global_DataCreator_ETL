using Avalonia.Data.Converters;
using System.Globalization;

namespace GlobalDataCreatorETL.UI.Converters;

/// <summary>
/// Returns true when the bound string equals the ConverterParameter string.
/// Used for mode radio buttons: IsChecked="{Binding CurrentMode, ConverterParameter=Export}".
/// </summary>
public sealed class StringEqualConverter : IValueConverter
{
    public static readonly StringEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString()?.Equals(parameter?.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter?.ToString() ?? string.Empty;
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
