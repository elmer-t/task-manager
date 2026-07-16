using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.ViewModels;

namespace TaskManager.App.Converters;

/// <summary>
/// Maps the three <see cref="UsageHeat"/> buckets to text brushes for the "usage-heat
/// emphasis" on numeric columns (spec §6). Brushes are supplied from XAML (theme resources).
/// </summary>
public sealed class UsageHeatToBrushConverter : IValueConverter
{
    public Brush? LowBrush { get; set; }

    public Brush? MediumBrush { get; set; }

    public Brush? HighBrush { get; set; }

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
