using System.ComponentModel;
using System.Diagnostics;
using TaskManager.Core.Abstractions;

namespace TaskManager.App.Interop;

/// <summary>
/// The elevation affordance (spec §8). Relaunches the app elevated via the shell's
/// <c>runas</c> verb (the UAC prompt); on a successful relaunch it exits the current,
/// un-elevated instance so the elevated one takes over. That exit is this type's
/// responsibility — it is the only place in the app that ends the process.
/// </summary>
internal sealed class ElevationService : IElevationService
{
    public void RestartElevated()
    {
        string? executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return;
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
                return;
            }
        }
        catch (Win32Exception)
        {
            // User dismissed UAC (or launch failed): stay running un-elevated.
            return;
        }

        Microsoft.UI.Xaml.Application.Current.Exit();
    }
}
