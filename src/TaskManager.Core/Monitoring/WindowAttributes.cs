namespace TaskManager.Core.Monitoring;

/// <summary>
/// The four properties of a single top-level window that decide whether it is a
/// <b>qualifying window</b>, and so counts toward classifying its owning process as an
/// <b>App</b> (spec §7). Reading these from Win32 (IsWindowVisible, WS_EX_TOOLWINDOW,
/// GetWindow(GW_OWNER), DWMWA_CLOAKED) is the App project's job; both the predicate and
/// the aggregation it feeds are the pure rule in <see cref="ClassificationRule"/>, so
/// production and tests cross the same seam.
/// </summary>
/// <param name="IsVisible">Result of IsWindowVisible.</param>
/// <param name="IsToolWindow">Has the WS_EX_TOOLWINDOW extended style.</param>
/// <param name="IsOwned">Has an owner window (GetWindow(GW_OWNER) is non-null).</param>
/// <param name="IsCloaked">DWM reports the window cloaked (DWMWA_CLOAKED != 0).</param>
public readonly record struct WindowAttributes(
    bool IsVisible,
    bool IsToolWindow,
    bool IsOwned,
    bool IsCloaked);
