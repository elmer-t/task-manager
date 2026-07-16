using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TaskManager.Core.Monitoring;

namespace TaskManager.App.ViewModels;

/// <summary>
/// One card in the pinned graph strip (spec §6): a scrolling filled-line series over the
/// rolling 60-second window (spec §5), plus the big current-value readout and a caption.
/// The series binds to <see cref="ObservableCollection{T}"/> so LiveCharts2 animates the
/// scroll as <see cref="Push"/> adds a point and drops the oldest.
/// </summary>
public sealed partial class GraphViewModel : ObservableObject
{
    private readonly ObservableCollection<double> _values;

    public GraphViewModel(string title, SKColor stroke, SKColor fill, double yMax)
    {
        Title = title;

        // Prime a full window of zeros so the plot area is the fixed 60-wide scrolling
        // window from the first frame rather than growing in.
        _values = new ObservableCollection<double>(
            Enumerable.Repeat(0.0, MonitorConstants.HistoryLength));

        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _values,
                Stroke = new SolidColorPaint(stroke) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(fill),
                GeometrySize = 0,          // no per-point markers
                LineSmoothness = 0.4,
                IsHoverable = false,
            },
        };

        XAxes = new ICartesianAxis[] { HiddenAxis(0, MonitorConstants.HistoryLength - 1) };
        YAxes = new ICartesianAxis[] { HiddenAxis(0, yMax) };
    }

    public string Title { get; }

    public ISeries[] Series { get; }

    public ICartesianAxis[] XAxes { get; }

    public ICartesianAxis[] YAxes { get; }

    /// <summary>Big value shown top-right of the card, e.g. "21%" or "12.4 GB".</summary>
    [ObservableProperty]
    private string _valueText = string.Empty;

    /// <summary>Small caption under the title, e.g. "of 32 GB · 39% used".</summary>
    [ObservableProperty]
    private string _captionText = string.Empty;

    /// <summary>Appends the newest sample and drops the oldest (rolling 60 s window).</summary>
    public void Push(double value) => RollingWindow.Push(_values, value, MonitorConstants.HistoryLength);

    private static Axis HiddenAxis(double min, double max) => new()
    {
        IsVisible = false,
        MinLimit = min,
        MaxLimit = max,
        SeparatorsPaint = null,
    };
}
