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
/// <remarks>
/// Commit charge (<see cref="CommitUsedBytes"/> / <see cref="CommitLimitBytes"/>) is sampled
/// but not yet surfaced — the memory card shows only physical memory today. It is retained
/// deliberately (rather than deleted per the §15 cleanup) as the foundation for a near-term
/// commit line on the memory card; deleting it would also drop the §9-checklist
/// <c>GetPerformanceInfo</c> binding. Wire it into a UI surface or revisit if that surface
/// doesn't land.
/// </remarks>
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
