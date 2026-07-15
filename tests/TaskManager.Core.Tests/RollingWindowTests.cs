using TaskManager.Core.Monitoring;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>The rolling 60-second graph window behaviour (spec §5), exercised on a List.</summary>
public class RollingWindowTests
{
    [Fact]
    public void Push_appends_newest_at_the_end()
    {
        var history = new List<double> { 1, 2, 3 };
        RollingWindow.Push(history, 4, capacity: 10);
        Assert.Equal(new double[] { 1, 2, 3, 4 }, history);
    }

    [Fact]
    public void Push_drops_the_oldest_once_capacity_is_exceeded()
    {
        var history = new List<int> { 1, 2, 3 };
        RollingWindow.Push(history, 4, capacity: 3);
        Assert.Equal(new[] { 2, 3, 4 }, history);
    }

    [Fact]
    public void Push_keeps_exactly_capacity_entries_over_many_pushes()
    {
        var history = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            RollingWindow.Push(history, i, capacity: MonitorConstants.HistoryLength);
        }

        Assert.Equal(MonitorConstants.HistoryLength, history.Count);
        Assert.Equal(99, history[^1]);                                   // newest kept
        Assert.Equal(100 - MonitorConstants.HistoryLength, history[0]);  // window slid forward
    }

    [Fact]
    public void Push_rejects_a_non_positive_capacity()
    {
        var history = new List<int>();
        Assert.Throws<ArgumentOutOfRangeException>(() => RollingWindow.Push(history, 1, capacity: 0));
    }
}
