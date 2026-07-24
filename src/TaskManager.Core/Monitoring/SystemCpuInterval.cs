namespace TaskManager.Core.Monitoring;

/// <summary>
/// One <b>Tick</b>'s reading of the machine-wide CPU counters: the raw cumulative values,
/// plus what the interval ending at this reading measured. Both consumers of that interval
/// are served by this one reading — <see cref="BusyPercent"/> is the graph strip's CPU
/// card, <see cref="CpuDenominator"/> is what every process row's CPU % is a share of — so
/// the column and the card agree by construction rather than by two samplers happening to
/// read the counters at nearly the same moment.
/// </summary>
/// <param name="Idle">Cumulative idle time (100-ns units), the baseline for the next reading.</param>
/// <param name="Kernel">Cumulative kernel time, which includes idle.</param>
/// <param name="User">Cumulative user time.</param>
/// <param name="BusyPercent">Whole-machine busy fraction over the interval, 0–100.</param>
/// <param name="CpuDenominator">
/// The machine-wide kernel+user delta over the interval (CONTEXT.md). <c>0</c> means there
/// was no interval to measure — the first reading — and every process row therefore reads 0 %.
/// </param>
public readonly record struct SystemCpuInterval(
    ulong Idle,
    ulong Kernel,
    ulong User,
    double BusyPercent,
    ulong CpuDenominator)
{
    /// <summary>
    /// The first reading. There is no previous reading to compare against, so it measures
    /// no interval: both results are 0 until the next <b>Tick</b> folds through
    /// <see cref="Next"/>.
    /// </summary>
    public static SystemCpuInterval Start(ulong idle, ulong kernel, ulong user) =>
        new(idle, kernel, user, BusyPercent: 0.0, CpuDenominator: 0UL);

    /// <summary>
    /// The next reading, one <b>Tick</b> later. Each counter's delta is clamped
    /// individually (<see cref="CpuMath.Delta"/>) before they are added, so a counter that
    /// appears to move backwards reads as idle rather than wrapping into a wild spike.
    /// </summary>
    public SystemCpuInterval Next(ulong idle, ulong kernel, ulong user)
    {
        ulong idleDelta = CpuMath.Delta(Idle, idle);
        ulong kernelDelta = CpuMath.Delta(Kernel, kernel);
        ulong userDelta = CpuMath.Delta(User, user);

        return new SystemCpuInterval(
            idle,
            kernel,
            user,
            CpuMath.SystemBusyPercent(idleDelta, kernelDelta, userDelta),
            kernelDelta + userDelta);
    }
}
