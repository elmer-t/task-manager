using System.Collections.ObjectModel;

namespace TaskManager.Core.Collections;

/// <summary>
/// Reconciles a bound <see cref="ObservableCollection{T}"/> against the latest tick's
/// samples by stable key, updating existing rows in place and only adding/removing what
/// changed. This is what lets a 1 Hz refresh keep the user's selection and scroll position
/// instead of clearing and rebuilding the list every second.
/// </summary>
public static class CollectionSync
{
    public static void Apply<TSample, TRow, TKey>(
        ObservableCollection<TRow> target,
        IReadOnlyList<TSample> samples,
        Func<TSample, TKey> sampleKey,
        Func<TRow, TKey> rowKey,
        Func<TSample, TRow> create,
        Action<TRow, TSample> update)
        where TKey : notnull
    {
        var incoming = new Dictionary<TKey, TSample>(samples.Count);
        foreach (TSample sample in samples)
        {
            incoming[sampleKey(sample)] = sample;
        }

        // Drop rows whose process/service is gone.
        for (int i = target.Count - 1; i >= 0; i--)
        {
            if (!incoming.ContainsKey(rowKey(target[i])))
            {
                target.RemoveAt(i);
            }
        }

        // Refresh the rows that remain.
        var present = new HashSet<TKey>();
        foreach (TRow row in target)
        {
            TKey key = rowKey(row);
            present.Add(key);
            if (incoming.TryGetValue(key, out TSample? sample))
            {
                update(row, sample);
            }
        }

        // Append newcomers, preserving the sample order.
        foreach (TSample sample in samples)
        {
            if (present.Add(sampleKey(sample)))
            {
                target.Add(create(sample));
            }
        }
    }
}
