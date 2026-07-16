using System.Runtime.InteropServices;
using TaskManager.Core.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace TaskManager.App.Interop;

/// <summary>
/// End task (spec §8): the only mutating action. It never pre-checks terminate rights —
/// it attempts OpenProcess(PROCESS_TERMINATE) + TerminateProcess and maps the failure, so
/// Access Denied is what drives the "Restart as administrator" flow rather than a
/// pre-disabled button.
/// </summary>
internal sealed class ProcessTerminator : IProcessTerminator
{
    public TerminationOutcome Terminate(int processId)
    {
        using var handle = PInvoke.OpenProcess(
            PROCESS_ACCESS_RIGHTS.PROCESS_TERMINATE,
            bInheritHandle: false,
            (uint)processId);

        if (handle.IsInvalid)
        {
            return MapError((WIN32_ERROR)Marshal.GetLastWin32Error(), openFailed: true);
        }

        if (PInvoke.TerminateProcess(handle, uExitCode: 1))
        {
            return TerminationOutcome.Success;
        }

        return MapError((WIN32_ERROR)Marshal.GetLastWin32Error(), openFailed: false);
    }

    private static TerminationOutcome MapError(WIN32_ERROR error, bool openFailed) => error switch
    {
        WIN32_ERROR.ERROR_ACCESS_DENIED => TerminationOutcome.AccessDenied,
        // OpenProcess reports a vanished PID as an invalid parameter.
        WIN32_ERROR.ERROR_INVALID_PARAMETER when openFailed => TerminationOutcome.NotFound,
        _ => TerminationOutcome.Failed,
    };
}
