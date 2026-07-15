using TaskManager.Core.Monitoring;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>The CPU delta arithmetic behind the graph and the per-process column (spec §4).</summary>
public class CpuMathTests
{
    [Fact]
    public void Delta_is_the_forward_difference()
    {
        Assert.Equal(40UL, CpuMath.Delta(100, 140));
    }

    [Fact]
    public void Delta_clamps_a_backwards_counter_to_zero()
    {
        // PID reuse / counter reset must not produce a wild negative-turned-huge delta.
        Assert.Equal(0UL, CpuMath.Delta(140, 100));
    }

    [Theory]
    [InlineData(100, 100, 0, 0.0)]     // all idle → 0% busy (kernel includes idle)
    [InlineData(0, 100, 0, 100.0)]     // no idle → 100% busy
    [InlineData(50, 80, 20, 50.0)]     // half of the 100 total ticks were idle → 50%
    public void SystemBusyPercent_reflects_the_non_idle_share(
        ulong idle, ulong kernel, ulong user, double expected)
    {
        Assert.Equal(expected, CpuMath.SystemBusyPercent(idle, kernel, user), precision: 6);
    }

    [Fact]
    public void SystemBusyPercent_is_zero_when_no_time_elapsed()
    {
        Assert.Equal(0.0, CpuMath.SystemBusyPercent(0, 0, 0));
    }

    [Fact]
    public void ProcessPercent_is_the_share_of_machine_capacity()
    {
        // 25 of the machine's 200 total ticks belonged to the process → 12.5%.
        Assert.Equal(12.5, CpuMath.ProcessPercent(25, 200), precision: 6);
    }

    [Fact]
    public void ProcessPercent_is_zero_when_the_machine_had_no_ticks()
    {
        Assert.Equal(0.0, CpuMath.ProcessPercent(10, 0));
    }

    [Fact]
    public void ProcessPercent_never_exceeds_one_hundred()
    {
        Assert.Equal(100.0, CpuMath.ProcessPercent(500, 200));
    }
}
