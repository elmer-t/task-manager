using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.ViewModels;

namespace TaskManager.App.Converters;

/// <summary>
/// Maps the three <see cref="UsageHeat"/> buckets to text brushes for the "usage-heat
/// emphasis" on numeric columns (spec §6). Brushes are supplied from XAML (theme resources),
/// so they must be dependency properties — {ThemeResource} cannot assign to plain CLR
/// properties and fails at parse time with XamlParseException.
/// </summary>
public sealed class UsageHeatToBrushConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty LowBrushProperty = DependencyProperty.Register(
        nameof(LowBrush), typeof(Brush), typeof(UsageHeatToBrushConverter), new PropertyMetadata(null));

    public static readonly DependencyProperty MediumBrushProperty = DependencyProperty.Register(
        nameof(MediumBrush), typeof(Brush), typeof(UsageHeatToBrushConverter), new PropertyMetadata(null));

    public static readonly DependencyProperty HighBrushProperty = DependencyProperty.Register(
        nameof(HighBrush), typeof(Brush), typeof(UsageHeatToBrushConverter), new PropertyMetadata(null));

    public Brush? LowBrush
    {
        get => (Brush?)GetValue(LowBrushProperty);
        set => SetValue(LowBrushProperty, value);
    }

    public Brush? MediumBrush
    {
        get => (Brush?)GetValue(MediumBrushProperty);
        set => SetValue(MediumBrushProperty, value);
    }

    public Brush? HighBrush
    {
        get => (Brush?)GetValue(HighBrushProperty);
        set => SetValue(HighBrushProperty, value);
    }

    public object? Convert(object value, Type targetType, object parameter, string language) =>
        value is UsageHeat heat
            ? heat switch
            {
                UsageHeat.High => HighBrush,
                UsageHeat.Medium => MediumBrush,
                _ => LowBrush,
            }
            : LowBrush;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
