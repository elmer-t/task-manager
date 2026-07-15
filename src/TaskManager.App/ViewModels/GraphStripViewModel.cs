using SkiaSharp;
using TaskManager.Core.Models;
using TaskManager.Core.Text;

namespace TaskManager.App.ViewModels;

/// <summary>
/// The pinned pair of cards at the top of the content area (spec §6): system-wide CPU and
/// memory, identical across all three views. Fed the per-tick <see cref="SystemSample"/>.
/// </summary>
public sealed class GraphStripViewModel
{
    // Accent colours chosen to read on both Fluent light and dark backdrops.
    private static readonly SKColor CpuStroke = new(0x00, 0x91, 0xD5);
    private static readonly SKColor CpuFill = new(0x00, 0x91, 0xD5, 0x2B);
    private static readonly SKColor MemoryStroke = new(0x9B, 0x74, 0xF0);
    private static readonly SKColor MemoryFill = new(0x9B, 0x74, 0xF0, 0x2B);

    public GraphStripViewModel()
    {
        Cpu = new GraphViewModel("CPU", CpuStroke, CpuFill, yMax: 100);
        Memory = new GraphViewModel("Memory", MemoryStroke, MemoryFill, yMax: 100);
    }

    public GraphViewModel Cpu { get; }

    public GraphViewModel Memory { get; }

    public void Update(SystemSample system)
    {
        Cpu.Push(system.CpuPercent);
        Cpu.ValueText = Humanize.Percent(system.CpuPercent, decimals: 0);
        Cpu.CaptionText = "System-wide";

        double memoryPercent = system.MemoryUsedPercent;
        Memory.Push(memoryPercent);
        Memory.ValueText = Humanize.Bytes(system.MemoryUsedBytes);
        Memory.CaptionText =
            $"of {Humanize.Bytes(system.MemoryTotalBytes)} · {Humanize.Percent(memoryPercent, 0)} used";
    }
}
