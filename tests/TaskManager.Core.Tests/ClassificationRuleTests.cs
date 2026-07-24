using TaskManager.Core.Models;
using TaskManager.Core.Monitoring;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>Locks down the App/Background rule from spec §7.</summary>
public class ClassificationRuleTests
{
    private static WindowAttributes Qualifying() => new(
        IsVisible: true,
        IsToolWindow: false,
        IsOwned: false,
        IsCloaked: false);

    [Fact]
    public void A_visible_unowned_untooled_uncloaked_window_qualifies()
    {
        Assert.True(ClassificationRule.IsQualifyingWindow(Qualifying()));
    }

    [Theory]
    // Flip exactly one attribute away from the qualifying case; each disqualifies.
    [InlineData(false, false, false, false)] // not visible
    [InlineData(true, true, false, false)]   // tool window
    [InlineData(true, false, true, false)]   // owned
    [InlineData(true, false, false, true)]   // cloaked (e.g. suspended packaged app)
    public void Any_single_failing_attribute_disqualifies(bool visible, bool tool, bool owned, bool cloaked)
    {
        var window = new WindowAttributes(visible, tool, owned, cloaked);
        Assert.False(ClassificationRule.IsQualifyingWindow(window));
    }

    // ---- The "at least one qualifying window" aggregation (spec §7) ----
    //
    // The stream Classify consumes is the owning PID of every window that already passed
    // IsQualifyingWindow — the Win32 reader pre-filters, so anything arriving here qualifies.

    [Fact]
    public void A_process_owning_one_qualifying_window_is_an_app()
    {
        ProcessClassification classification = ClassificationRule.Classify(new uint[] { 4242 });

        Assert.Equal(ProcessKind.App, classification.Kind(4242));
    }

    [Fact]
    public void A_multi_window_process_is_one_app_not_several()
    {
        // "At least one" — the same PID arriving once per window dedupes to a single verdict.
        ProcessClassification classification = ClassificationRule.Classify(new uint[] { 7, 7, 7 });

        Assert.Equal(ProcessKind.App, classification.Kind(7));
    }

    [Fact]
    public void A_process_absent_from_the_stream_is_background()
    {
        // Covers the windowless process: it owns nothing, so it never reaches the stream.
        ProcessClassification classification = ClassificationRule.Classify(new uint[] { 4242 });

        Assert.Equal(ProcessKind.Background, classification.Kind(99));
    }

    [Fact]
    public void An_empty_stream_makes_every_process_background()
    {
        ProcessClassification classification = ClassificationRule.Classify(Array.Empty<uint>());

        Assert.Equal(ProcessKind.Background, classification.Kind(4242));
    }
}
