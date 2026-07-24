namespace TaskManager.Core.Abstractions;

/// <summary>
/// The elevation affordance from spec §8: relaunch the whole app elevated (UAC via the
/// <c>runas</c> verb) when End task hits Access Denied. The app never requests admin up
/// front (spec §4).
/// </summary>
public interface IElevationService
{
    /// <summary>
    /// Relaunches the app elevated and, on success, asks the application to exit so the
    /// elevated instance takes over — the two are halves of one operation, and the
    /// implementation owns the exit because only the WinUI head can talk to the application
    /// object. A dismissed UAC prompt is a no-op: this instance stays running, un-elevated.
    /// </summary>
    void RestartElevated();
}
