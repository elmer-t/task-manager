using TaskManager.Core.Presentation;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>
/// Pins the usage-heat thresholds (spec §6) at their boundaries, so both numeric columns
/// keep agreeing about what counts as Medium and High.
/// </summary>
public class HeatTests
{
    private const ulong MegaByte = 1024UL * 1024UL;

    [Fact]
    public void A_blank_cell_reads_low_in_both_columns()
    {
        // An inaccessible process renders blank (spec §4) — blank is not "hot".
        Assert.Equal(UsageHeat.Low, Heat.ForCpu(null));
        Assert.Equal(UsageHeat.Low, Heat.ForMemory(null));
    }

    [Theory]
    [InlineData(0.0, UsageHeat.Low)]
    [InlineData(2.9, UsageHeat.Low)]
    [InlineData(3.0, UsageHeat.Medium)]
    [InlineData(7.9, UsageHeat.Medium)]
    [InlineData(8.0, UsageHeat.High)]
    [InlineData(100.0, UsageHeat.High)]
    public void Cpu_buckets_at_3_and_8_percent(double cpuPercent, UsageHeat expected)
    {
        Assert.Equal(expected, Heat.ForCpu(cpuPercent));
    }

    [Theory]
    [InlineData(0UL, UsageHeat.Low)]
    [InlineData(300UL * MegaByte - 1, UsageHeat.Low)]
    [InlineData(300UL * MegaByte, UsageHeat.Medium)]
    [InlineData(800UL * MegaByte - 1, UsageHeat.Medium)]
    [InlineData(800UL * MegaByte, UsageHeat.High)]
    public void Memory_buckets_at_300_and_800_megabytes(ulong bytes, UsageHeat expected)
    {
        Assert.Equal(expected, Heat.ForMemory(bytes));
    }
}
