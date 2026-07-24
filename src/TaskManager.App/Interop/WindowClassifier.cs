using TaskManager.Core.Monitoring;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TaskManager.App.Interop;

/// <summary>
/// The Win32 half of the App/Background classifier (spec §7): enumerate every top-level
/// window once per tick, read the four attributes, and hand the owning PID of each
/// <b>qualifying window</b> to <see cref="ClassificationRule"/>. Both halves of the
/// decision — the per-window predicate and the "at least one" aggregation — belong to that
/// pure rule; this file only reads Win32 and reports what it saw.
/// </summary>
internal sealed class WindowClassifier
{
    private const uint WsExToolWindow = 0x00000080; // WS_EX_TOOLWINDOW

    // This tick's qualifying-window owners, buffered for ClassificationRule because
    // EnumWindows is callback-driven and cannot be consumed lazily. Reused across ticks for
    // continuity rather than because it is load-bearing: the process sampler allocates a
    // list, a set and a record per process every tick, and spec §5's budget is about
    // syscalls, not GC pressure.
    private readonly List<uint> _qualifyingWindowOwners = new();

    /// <summary>
    /// Returns this tick's §7 verdict for every process. EnumWindows enumerates only
    /// top-level windows, which is exactly the scope the rule is defined over.
    /// </summary>
    public ProcessClassification ClassifyProcesses()
    {
        _qualifyingWindowOwners.Clear();

        PInvoke.EnumWindows(EnumWindow, lParam: default);
        return ClassificationRule.Classify(_qualifyingWindowOwners);
    }

    private BOOL EnumWindow(HWND hwnd, LPARAM _)
    {
        if (IsQualifyingWindow(hwnd, out uint processId))
        {
            _qualifyingWindowOwners.Add(processId);
        }

        return true; // keep enumerating
    }

    private static unsafe bool IsQualifyingWindow(HWND hwnd, out uint processId)
    {
        processId = 0;

        var attributes = new WindowAttributes(
            IsVisible: PInvoke.IsWindowVisible(hwnd),
            IsToolWindow: HasToolWindowStyle(hwnd),
            IsOwned: PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER) != default,
            IsCloaked: IsCloaked(hwnd));

        if (!ClassificationRule.IsQualifyingWindow(attributes))
        {
            return false;
        }

        uint owningProcessId = 0;
        _ = PInvoke.GetWindowThreadProcessId(hwnd, &owningProcessId);
        processId = owningProcessId;

        // A PID left at 0 means the owner query failed — the window died mid-enumeration.
        // Drop it: a read failure is not a §7 clause, and PID 0 is the Idle Process, which
        // a toolhelp snapshot does list and which must never surface in the Apps view.
        return processId != 0;
    }

    private static bool HasToolWindowStyle(HWND hwnd)
    {
        nint exStyle = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        return ((uint)exStyle & WsExToolWindow) != 0;
    }

    private static unsafe bool IsCloaked(HWND hwnd)
    {
        uint cloaked = 0;
        HRESULT hr = PInvoke.DwmGetWindowAttribute(
            hwnd,
            DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
            &cloaked,
            (uint)sizeof(uint));

        // If DWM can't answer (very old shell, etc.) treat as not cloaked.
        return hr.Succeeded && cloaked != 0;
    }
}
