using TaskManager.Core.Text;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>The "842 MB" / "1.8 GB" / "12.4%" presentation shown in the tables (spec §6).</summary>
public class HumanizeTests
{
    private const ulong Mb = 1024UL * 1024UL;
    private const ulong Gb = 1024UL * Mb;

    [Fact]
    public void Bytes_shows_whole_megabytes_below_a_gigabyte()
    {
        Assert.Equal("842 MB", Humanize.Bytes(842 * Mb));
    }

    [Fact]
    public void Bytes_switches_to_gigabytes_at_1024_mb()
    {
        Assert.Equal("1.5 GB", Humanize.Bytes(1536 * Mb));
        Assert.Equal("2.0 GB", Humanize.Bytes(2 * Gb));
    }

    [Fact]
    public void Bytes_keeps_one_decimal_for_small_values()
    {
        Assert.Equal("5.0 MB", Humanize.Bytes(5 * Mb));
    }

    [Fact]
    public void BytesOrBlank_is_empty_for_an_inaccessible_process()
    {
        Assert.Equal(string.Empty, Humanize.BytesOrBlank(null));
        Assert.Equal("842 MB", Humanize.BytesOrBlank(842 * Mb));
    }

    [Fact]
    public void Percent_formats_with_one_decimal_by_default()
    {
        Assert.Equal("12.4%", Humanize.Percent(12.4));
    }

    [Fact]
    public void Percent_can_drop_the_decimal_for_the_system_readout()
    {
        Assert.Equal("21%", Humanize.Percent(21, decimals: 0));
    }

    [Fact]
    public void PercentOrBlank_is_empty_for_an_inaccessible_process()
    {
        Assert.Equal(string.Empty, Humanize.PercentOrBlank(null));
        Assert.Equal("0.0%", Humanize.PercentOrBlank(0));
    }
}
