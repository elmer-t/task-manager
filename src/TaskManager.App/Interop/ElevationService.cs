using System.Security.Principal;
using TaskManager.Core.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TaskManager.App.Interop;

/// <summary>
/// The elevation affordance (spec §8). Reports current elevation, and relaunches the app
/// elevated via ShellExecute with the <c>runas</c> verb (the UAC prompt). On a successful
/// relaunch it exits the current, un-elevated instance so the elevated one takes over.
/// </summary>
internal sealed class ElevationService : IElevationService
{
    public bool IsElevated
    {
        get
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public bool RestartElevated()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return false;
        }

        // ShellExecute returns an HINSTANCE; a value > 32 means the launch succeeded.
        HINSTANCE result = PInvoke.ShellExecuteW(
            hwnd: default,
            lpOperation: "runas",
            lpFile: executablePath,
            lpParameters: null,
            lpDirectory: null,
            nShowCmd: SHOW_WINDOW_CMD.SW_SHOWNORMAL);

        // HINSTANCE > 32 means the launch succeeded (classic ShellExecute contract).
        if ((nint)result.Value <= 32)
        {
            // User dismissed UAC (or launch failed): stay running un-elevated.
            return false;
        }

        Microsoft.UI.Xaml.Application.Current.Exit();
        return true;
    }
}
