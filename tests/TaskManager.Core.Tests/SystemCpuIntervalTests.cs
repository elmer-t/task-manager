using TaskManager.Core.Monitoring;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>
/// Pins the machine-wide CPU reading that both the graph card and every process row's
/// CPU % are computed from — one reading per <b>Tick</b>, so the <b>CPU denominator</b>
/// and the card's busy percentage can't drift apart.
/// </summary>
public class SystemCpuIntervalTests
{
    [Fact]
    public void The_first_reading_has_no_interval_to_measure()
    {
        SystemCpuInterval start = SystemCpuInterval.Start(idle: 500, kernel: 800, user: 200);

        Assert.Equal(0.0, start.BusyPercent);
        Assert.Equal(0UL, start.CpuDenominator);
    }

    [Fact]
    public void The_denominator_is_the_kernel_plus_user_delta()
    {
        SystemCpuInterval start = SystemCpuInterval.Start(idle: 500, kernel: 800, user: 200);

        SystemCpuInterval next = start.Next(idle: 560, kernel: 900, user: 300);

        // kernelΔ 100 + userΔ 100.
        Assert.Equal(200UL, next.CpuDenominator);
    }

    [Fact]
    public void The_busy_percentage_matches_CpuMath_for_the_same_deltas()
    {
        SystemCpuInterval start = SystemCpuInterval.Start(idle: 500, kernel: 800, user: 200);

        SystemCpuInterval next = start.Next(idle: 560, kernel: 900, user: 300);

        // idleΔ 60 of a 200 total → 70% busy, the graph card's figure.
        Assert.Equal(CpuMath.SystemBusyPercent(60, 100, 100), next.BusyPercent);
        Assert.Equal(70.0, next.BusyPercent, precision: 10);
    }

    [Fact]
    public void The_raw_counters_carry_forward_as_the_next_baseline()
    {
        SystemCpuInterval second = SystemCpuInterval.Start(500, 800, 200).Next(560, 900, 300);

        SystemCpuInterval third = second.Next(idle: 600, kernel: 1000, user: 400);

        // Measured against the second reading, not the first.
        Assert.Equal(200UL, third.CpuDenominator);
    }

    [Fact]
    public void A_counter_that_moves_backwards_clamps_to_zero()
    {
        SystemCpuInterval start = SystemCpuInterval.Start(idle: 500, kernel: 800, user: 200);

        SystemCpuInterval next = start.Next(idle: 400, kernel: 700, user: 100);

        Assert.Equal(0UL, next.CpuDenominator);
        Assert.Equal(0.0, next.BusyPercent);
    }

    [Fact]
    public void An_idle_delta_larger_than_the_total_clamps_busy_to_zero()
    {
        SystemCpuInterval start = SystemCpuInterval.Start(idle: 500, kernel: 800, user: 200);

        // idleΔ 400 against a kernel+user delta of 100 — nonsense counters read as idle,
        // never as a wild spike.
        SystemCpuInterval next = start.Next(idle: 900, kernel: 900, user: 200);

        Assert.Equal(0.0, next.BusyPercent);
        Assert.Equal(100UL, next.CpuDenominator);
    }
}
