namespace TaskManager.Core.Abstractions;

/// <summary>
/// The elevation affordance from spec §8: report whether we are already elevated, and
/// relaunch the whole app elevated (UAC via the <c>runas</c> verb) when End task hits
/// Access Denied. The app never requests admin up front (spec §4).
/// </summary>
public interface IElevationService
{
    /// <summary>True when the current process is already running elevated.</summary>
    bool IsElevated { get; }

    /// <summary>
    /// Relaunches the app elevated and asks the current instance to exit. Returns
    /// <see langword="false"/> if the user dismissed the UAC prompt (stay as we are).
    /// </summary>
    bool RestartElevated();
}
