namespace TaskManager.Core.Models;

/// <summary>
/// How a process is bucketed between the <b>Apps</b> and <b>Background processes</b>
/// views. This is a classification, not a Windows priority class (see CONTEXT.md).
/// The rule that assigns it lives in <see cref="Monitoring.ClassificationRule"/> (spec §7).
/// </summary>
public enum ProcessKind
{
    /// <summary>Owns at least one qualifying top-level window — shown in <b>Apps</b>.</summary>
    App,

    /// <summary>Everything else — shown in <b>Background processes</b>.</summary>
    Background,
}
