namespace TaskManager.Core.Abstractions;

/// <summary>
/// The result of attempting <b>End task</b> (spec §8). The app never probes
/// terminate-rights up front; it attempts, then maps the outcome — <see cref="AccessDenied"/>
/// is what surfaces the "Restart as administrator" affordance.
/// </summary>
public enum TerminationOutcome
{
    /// <summary>TerminateProcess succeeded; the row disappears on the next tick.</summary>
    Success,

    /// <summary>Access Denied — protected / other-user / service process. Offer elevation.</summary>
    AccessDenied,

    /// <summary>The process was already gone (nothing to kill).</summary>
    NotFound,

    /// <summary>Any other failure.</summary>
    Failed,
}

/// <summary>
/// Terminates a process by PID (spec §8). The only mutating action in v1.
/// The Win32 implementation lives in the App project.
/// </summary>
public interface IProcessTerminator
{
    TerminationOutcome Terminate(int processId);
}
