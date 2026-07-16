using TaskManager.Core.Monitoring;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TaskManager.App.Interop;

/// <summary>
/// Implements the Win32 half of the App/Background classifier (spec §7): enumerate every
/// top-level window once per tick, read the four attributes the rule needs, and collect
/// the PIDs that own at least one qualifying window. The <em>decision</em> is delegated to
/// <see cref="ClassificationRule"/> so it stays pure and tested; this file only reads Win32.
/// </summary>
internal sealed class WindowClassifier
{
    private const uint WsExToolWindow = 0x00000080; // WS_EX_TOOLWINDOW

    // Reused across ticks to avoid per-tick allocation churn on the hot path (spec §5).
    private readonly HashSet<uint> _appProcessIds = new();

    /// <summary>
    /// Returns the set of PIDs classified as <b>App</b> this tick. A PID absent from the
    /// set is a <b>Background process</b> (spec §7). EnumWindows enumerates only top-level
    /// windows, which is exactly the scope the rule is defined over.
    /// </summary>
    public IReadOnlySet<uint> CollectAppProcessIds()
    {
        _appProcessIds.Clear();

        PInvoke.EnumWindows(EnumWindow, lParam: default);
        return _appProcessIds;
    }

    private BOOL EnumWindow(HWND hwnd, LPARAM _)
    {
        if (IsQualifyingWindow(hwnd, out uint processId))
        {
            _appProcessIds.Add(processId);
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
