namespace TaskManager.Core.Models;

/// <summary>
/// The two-state run status shown as a pill in the <b>Services</b> view (spec §6).
/// v1 collapses every non-running SCM state (stopped, start/stop-pending, paused) to
/// <see cref="Stopped"/> — the view is read-only, so finer states earn no UI.
/// </summary>
public enum ServiceStatus
{
    Running,
    Stopped,
}
