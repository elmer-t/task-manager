namespace TaskManager.Core.Models;

/// <summary>
/// The system-wide CPU and memory reading for one tick — the data behind the pinned
/// <b>graph strip</b>, identical across all three views (spec §6). Both graphs always
/// work; they need no elevation (spec §4).
/// </summary>
/// <param name="CpuPercent">Whole-machine CPU busy fraction, 0–100 (from GetSystemTimes deltas).</param>
/// <param name="MemoryUsedBytes">Physical memory in use (GlobalMemoryStatusEx).</param>
/// <param name="MemoryTotalBytes">Total physical memory.</param>
/// <param name="CommitUsedBytes">Commit charge in use (GetPerformanceInfo).</param>
/// <param name="CommitLimitBytes">Commit limit.</param>
public sealed record SystemSample(
    double CpuPercent,
    ulong MemoryUsedBytes,
    ulong MemoryTotalBytes,
    ulong CommitUsedBytes,
    ulong CommitLimitBytes)
{
    /// <summary>Physical memory in use as a percentage of total, 0–100.</summary>
    public double MemoryUsedPercent =>
        MemoryTotalBytes == 0 ? 0 : (double)MemoryUsedBytes / MemoryTotalBytes * 100.0;
}
