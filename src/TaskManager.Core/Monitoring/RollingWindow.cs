namespace TaskManager.Core.Monitoring;

/// <summary>
/// Maintains a fixed-length rolling history in place — the graph strip's 60-second
/// window (spec §5). Works on any <see cref="IList{T}"/>, so the graph view model can
/// pass its bound <c>ObservableCollection&lt;double&gt;</c> (LiveCharts2 animates the
/// scroll from the resulting Add/RemoveAt) while tests pass a plain <c>List</c>.
/// </summary>
public static class RollingWindow
{
    /// <summary>
    /// Appends <paramref name="value"/> and drops the oldest entries until the list is
    /// no longer than <paramref name="capacity"/>. Oldest is index 0, newest is last.
    /// </summary>
    public static void Push<T>(IList<T> history, T value, int capacity)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");
        }

        history.Add(value);
        while (history.Count > capacity)
        {
            history.RemoveAt(0);
        }
    }
}
