using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace TaskManager.App.Converters;

/// <summary>
/// Shared base for the one-way brush-picking converters (status pill, usage-heat emphasis).
/// Their brush inputs are supplied from XAML as theme resources, so they must be dependency
/// properties — {ThemeResource} cannot assign to plain CLR properties and fails at parse
/// time with XamlParseException; hence the <see cref="DependencyObject"/> base and the
/// <see cref="RegisterBrush"/> helper. These converters are display-only, so
/// <see cref="ConvertBack"/> is unsupported.
/// </summary>
public abstract class BrushConverterBase : DependencyObject, IValueConverter
{
    public abstract object? Convert(object value, Type targetType, object parameter, string language);

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    /// <summary>Registers a nullable <see cref="Brush"/> dependency property on the given converter.</summary>
    protected static DependencyProperty RegisterBrush(string name, Type ownerType) =>
        DependencyProperty.Register(name, typeof(Brush), ownerType, new PropertyMetadata(null));
}
