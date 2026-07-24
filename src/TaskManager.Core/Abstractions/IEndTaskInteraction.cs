namespace TaskManager.Core.Abstractions;

/// <summary>
/// The dialog side of the kill-process UX (spec §8). It sits in Core because it is the
/// platform line: <see cref="Monitoring.EndTaskFlow"/> owns the §8 ordering here, while the
/// actual Fluent <c>ContentDialog</c>s live in the WinUI head (they need a XamlRoot), which
/// Core cannot reference. Every End task confirms first; Access Denied offers elevation.
/// </summary>
public interface IEndTaskInteraction
{
    /// <summary>
    /// The unconditional confirm dialog — "End &lt;process&gt;? Unsaved data may be lost."
    /// Returns true if the user chose <b>End</b>.
    /// </summary>
    Task<bool> ConfirmEndTaskAsync(string processName);

    /// <summary>
    /// The Access-Denied error dialog offering "Restart as administrator". Returns true if
    /// the user chose to relaunch elevated.
    /// </summary>
    Task<bool> ShowAccessDeniedAsync(string processName);

    /// <summary>A generic failure dialog for a terminate that failed for another reason.</summary>
    Task ShowFailedAsync(string processName);
}
