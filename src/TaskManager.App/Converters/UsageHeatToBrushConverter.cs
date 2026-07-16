using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.ViewModels;

namespace TaskManager.App.Converters;

/// <summary>
/// Maps the three <see cref="UsageHeat"/> buckets to text brushes for the "usage-heat
/// emphasis" on numeric columns (spec §6). See <see cref="BrushConverterBase"/> for why the
/// brushes are dependency properties.
/// </summary>
public sealed class UsageHeatToBrushConverter : BrushConverterBase
{
    public static readonly DependencyProperty LowBrushProperty =
        RegisterBrush(nameof(LowBrush), typeof(UsageHeatToBrushConverter));

    public static readonly DependencyProperty MediumBrushProperty =
        RegisterBrush(nameof(MediumBrush), typeof(UsageHeatToBrushConverter));

    public static readonly DependencyProperty HighBrushProperty =
        RegisterBrush(nameof(HighBrush), typeof(UsageHeatToBrushConverter));

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

    public override object? Convert(object value, Type targetType, object parameter, string language) =>
        value is UsageHeat heat
            ? heat switch
            {
                UsageHeat.High => HighBrush,
                UsageHeat.Medium => MediumBrush,
                _ => LowBrush,
            }
            : LowBrush;
}
