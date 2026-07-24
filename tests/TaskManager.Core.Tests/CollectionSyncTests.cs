using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TaskManager.Core.Collections;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>
/// Locks down the reconcile-by-key behaviour that lets a 1 Hz refresh keep the user's
/// selection and scroll position: rows survive as instances, only what changed moves.
/// </summary>
public class CollectionSyncTests
{
    private sealed record Sample(int Id, string Name);

    private sealed class Row
    {
        public Row(Sample sample)
        {
            Id = sample.Id;
            Name = sample.Name;
        }

        public int Id { get; }

        public string Name { get; set; }
    }

    private static void Sync(ObservableCollection<Row> target, params Sample[] samples) =>
        CollectionSync.Apply(
            target,
            samples,
            static s => s.Id,
            static r => r.Id,
            static s => new Row(s),
            static (r, s) => r.Name = s.Name);

    [Fact]
    public void An_existing_row_is_updated_in_place()
    {
        var target = new ObservableCollection<Row>();
        Sync(target, new Sample(1, "chrome.exe"));
        Row first = target[0];

        Sync(target, new Sample(1, "chrome.exe (renamed)"));

        Assert.Same(first, Assert.Single(target));
        Assert.Equal("chrome.exe (renamed)", first.Name);
    }

    [Fact]
    public void A_vanished_key_is_removed()
    {
        var target = new ObservableCollection<Row>();
        Sync(target, new Sample(1, "a"), new Sample(2, "b"));

        Sync(target, new Sample(1, "a"));

        Assert.Equal(new[] { 1 }, target.Select(r => r.Id));
    }

    [Fact]
    public void A_new_key_is_appended_after_the_survivors()
    {
        var target = new ObservableCollection<Row>();
        Sync(target, new Sample(1, "a"), new Sample(2, "b"));
        Row survivor = target[1];

        // 1 is gone, 2 stays, 3 and 4 are new — newcomers land after what survived.
        Sync(target, new Sample(3, "c"), new Sample(2, "b"), new Sample(4, "d"));

        Assert.Equal(new[] { 2, 3, 4 }, target.Select(r => r.Id));
        Assert.Same(survivor, target[0]);
    }

    [Fact]
    public void Repeated_syncs_with_the_same_data_cause_no_churn()
    {
        var target = new ObservableCollection<Row>();
        Sync(target, new Sample(1, "a"), new Sample(2, "b"));

        int changes = 0;
        ((INotifyCollectionChanged)target).CollectionChanged += (_, _) => changes++;

        Sync(target, new Sample(1, "a"), new Sample(2, "b"));
        Sync(target, new Sample(1, "a"), new Sample(2, "b"));

        Assert.Equal(0, changes);
    }
}
