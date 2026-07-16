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

    [Fact]
    public void A_process_with_one_qualifying_window_is_an_app()
    {
        var windows = new[]
        {
            new WindowAttributes(true, false, true, false),  // owned dialog — no
            Qualifying(),                                     // main window — yes
        };

        Assert.Equal(ProcessKind.App, ClassificationRule.Classify(windows));
    }

    [Fact]
    public void A_process_with_no_qualifying_window_is_background()
    {
        var windows = new[]
        {
            new WindowAttributes(true, true, false, false),  // tool window
            new WindowAttributes(true, false, false, true),  // cloaked
        };

        Assert.Equal(ProcessKind.Background, ClassificationRule.Classify(windows));
    }

    [Fact]
    public void A_process_that_owns_no_windows_is_background()
    {
        Assert.Equal(ProcessKind.Background, ClassificationRule.Classify(Array.Empty<WindowAttributes>()));
    }
}
