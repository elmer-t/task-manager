namespace TaskManager.Core.Monitoring;

/// <summary>
/// The CPU-percentage arithmetic behind both the system graph and the per-process CPU %
/// column (spec §4 / CONTEXT.md), factored out of the Win32 layer so the delta math is
/// unit-testable. All inputs are cumulative CPU time in 100-ns units (the FILETIME unit
/// that GetSystemTimes and GetProcessTimes report); the caller converts FILETIME→ulong.
/// </summary>
public static class CpuMath
{
    /// <summary>
    /// Difference between two cumulative counters, clamped at zero so a counter that
    /// appears to move backwards (process replaced under a reused PID, wrap) reads as
    /// idle rather than a wild spike.
    /// </summary>
    public static ulong Delta(ulong previous, ulong current) =>
        current >= previous ? current - previous : 0UL;

    /// <summary>
    /// Whole-machine busy fraction for the CPU graph, 0–100. GetSystemTimes' kernel time
    /// <em>includes</em> idle, so total elapsed CPU time across all cores is
    /// <paramref name="kernelDelta"/> + <paramref name="userDelta"/>, and the busy part
    /// is that total minus <paramref name="idleDelta"/>.
    /// </summary>
    public static double SystemBusyPercent(ulong idleDelta, ulong kernelDelta, ulong userDelta)
    {
        ulong total = kernelDelta + userDelta;
        if (total == 0)
        {
            return 0.0;
        }

        double busy = (double)(total - Math.Min(idleDelta, total)) / total;
        return Clamp(busy * 100.0);
    }

    /// <summary>
    /// A process's CPU % as a share of total machine capacity (matching Task Manager's
    /// per-process column). <paramref name="processDelta"/> is the process's kernel+user
    /// delta; <paramref name="systemTotalDelta"/> is the machine's kernel+user delta over
    /// the same interval.
    /// </summary>
    public static double ProcessPercent(ulong processDelta, ulong systemTotalDelta)
    {
        if (systemTotalDelta == 0)
        {
            return 0.0;
        }

        return Clamp((double)processDelta / systemTotalDelta * 100.0);
    }

    private static double Clamp(double value) => value < 0 ? 0 : value > 100 ? 100 : value;
}
