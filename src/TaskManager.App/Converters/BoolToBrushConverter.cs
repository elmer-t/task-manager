using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace TaskManager.App.Converters;

/// <summary>
/// Picks one of two brushes from a bool — used for the Services status pill (Running vs
/// Stopped). See <see cref="BrushConverterBase"/> for why the brushes are dependency
/// properties.
/// </summary>
public sealed class BoolToBrushConverter : BrushConverterBase
{
    public static readonly DependencyProperty TrueBrushProperty =
        RegisterBrush(nameof(TrueBrush), typeof(BoolToBrushConverter));

    public static readonly DependencyProperty FalseBrushProperty =
        RegisterBrush(nameof(FalseBrush), typeof(BoolToBrushConverter));

    public Brush? TrueBrush
    {
        get => (Brush?)GetValue(TrueBrushProperty);
        set => SetValue(TrueBrushProperty, value);
    }

    public Brush? FalseBrush
    {
        get => (Brush?)GetValue(FalseBrushProperty);
        set => SetValue(FalseBrushProperty, value);
    }

    public override object? Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && b ? TrueBrush : FalseBrush;
}
