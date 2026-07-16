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
    /// Classifies a process from the windows it owns. <see cref="ProcessKind.App"/> if
    /// any one window qualifies (the "at least one" test naturally dedupes a
    /// multi-window process to a single App row); otherwise
    /// <see cref="ProcessKind.Background"/> — including a process that owns no windows.
    /// </summary>
    public static ProcessKind Classify(IEnumerable<WindowAttributes> ownedWindows)
    {
        foreach (var window in ownedWindows)
        {
            if (IsQualifyingWindow(window))
            {
                return ProcessKind.App;
            }
        }

        return ProcessKind.Background;
    }
}
