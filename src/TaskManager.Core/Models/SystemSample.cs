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
/// <param name="CpuDenominator">
/// The <b>CPU denominator</b> for this tick (CONTEXT.md): the machine-wide kernel+user
/// CPU-time delta the same reading produced <see cref="CpuPercent"/> from. Sampled and
/// consumed, but never displayed — the graph card shows only <see cref="CpuPercent"/>;
/// the process source divides every row's CPU time by this so the column and the card
/// share one reading. <c>0</c> on the first tick, where there is no interval yet.
/// </param>
/// <remarks>
/// Commit charge (<see cref="CommitUsedBytes"/> / <see cref="CommitLimitBytes"/>) is sampled
/// but not yet surfaced — the memory card shows only physical memory today. It is retained
/// deliberately (rather than deleted per the #15 cleanup) as the foundation for a near-term
/// commit line on the memory card; deleting it would also drop the §9-checklist
/// <c>GetPerformanceInfo</c> binding. Wire it into a UI surface or revisit if that surface
/// doesn't land.
/// </remarks>
public sealed record SystemSample(
    double CpuPercent,
    ulong MemoryUsedBytes,
    ulong MemoryTotalBytes,
    ulong CommitUsedBytes,
    ulong CommitLimitBytes,
    ulong CpuDenominator)
{
    /// <summary>Physical memory in use as a percentage of total, 0–100.</summary>
    public double MemoryUsedPercent =>
        MemoryTotalBytes == 0 ? 0 : (double)MemoryUsedBytes / MemoryTotalBytes * 100.0;
}
