using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TaskManager.App.Converters;

/// <summary>
/// Maps a bool to <see cref="Visibility"/>. Pass ConverterParameter="Invert" to flip it —
/// used to show the process area vs the services area from the same <c>IsProcessView</c> flag.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        if (parameter is string s && string.Equals(s, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility v && v == Visibility.Visible;
}
