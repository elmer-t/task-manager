using TaskManager.Core.Models;

namespace TaskManager.Core.Monitoring;

/// <summary>
/// The App-vs-Background classifier (spec §7), kept deliberately as a small, pure,
/// heavily-tested rule. "Simple heuristic, imperfections accepted": a process is an
/// <b>App</b> iff it owns at least one qualifying top-level window; otherwise it is a
/// <b>Background process</b>. No AUMID / packaged-app special-casing in v1.
/// </summary>
public static class ClassificationRule
{
    /// <summary>
    /// A window qualifies (spec §7) when it is <em>all</em> of: visible, not a tool
    /// window, unowned, and not DWM-cloaked. A suspended/cloaked packaged app window
    /// fails on cloaking; a tray-only helper has no such window at all.
    /// </summary>
    public static bool IsQualifyingWindow(WindowAttributes window) =>
        window.IsVisible &&
        !window.IsToolWindow &&
        !window.IsOwned &&
        !window.IsCloaked;

    /// <summary>
    /// Aggregates one <b>Tick</b>'s qualifying windows into a verdict for every process:
    /// a PID that owns at least one is an <see cref="ProcessKind.App"/>, everything else is
    /// a <see cref="ProcessKind.Background"/> process.
    /// </summary>
    /// <param name="qualifyingWindowOwners">
    /// The owning PID of every window that already passed <see cref="IsQualifyingWindow"/> —
    /// the caller reading Win32 pre-filters, so nothing here re-tests the four attributes.
    /// A multi-window process appears once per window; the set collapses it to one App.
    /// </param>
    public static ProcessClassification Classify(IEnumerable<uint> qualifyingWindowOwners)
    {
        var appProcessIds = new HashSet<uint>();
        foreach (uint processId in qualifyingWindowOwners)
        {
            appProcessIds.Add(processId);
        }

        return new ProcessClassification(appProcessIds);
    }
}
