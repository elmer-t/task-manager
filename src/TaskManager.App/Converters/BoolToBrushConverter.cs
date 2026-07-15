using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace TaskManager.App.Converters;

/// <summary>
/// Picks one of two brushes from a bool — used for the Services status pill (Running vs
/// Stopped). Both brushes are set in XAML, so they can reference theme resources.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public Brush? TrueBrush { get; set; }

    public Brush? FalseBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && b ? TrueBrush : FalseBrush;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
