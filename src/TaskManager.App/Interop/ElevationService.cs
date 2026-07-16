using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using TaskManager.Core.Abstractions;

namespace TaskManager.App.Interop;

/// <summary>
/// The elevation affordance (spec §8). Reports current elevation, and relaunches the app
/// elevated via the shell's <c>runas</c> verb (the UAC prompt). On a successful relaunch
/// it exits the current, un-elevated instance so the elevated one takes over.
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

        // UseShellExecute + "runas" is ShellExecuteEx under the hood: it shows the UAC
        // prompt, and reports a declined prompt as ERROR_CANCELLED (Win32Exception).
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = true,
            Verb = "runas",
        };

        try
        {
            if (Process.Start(startInfo) is null)
            {
                return false;
            }
        }
        catch (Win32Exception)
        {
            // User dismissed UAC (or launch failed): stay running un-elevated.
            return false;
        }

        Microsoft.UI.Xaml.Application.Current.Exit();
        return true;
    }
}
