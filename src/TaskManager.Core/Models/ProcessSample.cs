namespace TaskManager.Core.Models;

/// <summary>
/// One process as observed on a single <b>tick</b> (spec §5). The process list is always
/// complete because it comes from a toolhelp snapshot that needs no per-process handle;
/// when the app cannot open a process (elevation-gated), its metric fields are
/// <see langword="null"/> and render as <b>blank cells</b> rather than erroring (spec §4).
/// </summary>
/// <param name="ProcessId">The PID from the toolhelp snapshot.</param>
/// <param name="Name">Image name, e.g. <c>chrome.exe</c>.</param>
/// <param name="Kind">Apps vs Background bucket (spec §7).</param>
/// <param name="CpuPercent">
/// Share of total machine CPU capacity over the last interval, or <see langword="null"/>
/// if the process handle could not be opened.
/// </param>
/// <param name="PrivateWorkingSetBytes">
/// Private Working Set (spec §4 / CONTEXT.md), or <see langword="null"/> when inaccessible.
/// </param>
public sealed record ProcessSample(
    int ProcessId,
    string Name,
    ProcessKind Kind,
    double? CpuPercent,
    ulong? PrivateWorkingSetBytes)
{
    /// <summary>True when this row's CPU and memory cells have real values to show.</summary>
    public bool MetricsAvailable => CpuPercent is not null && PrivateWorkingSetBytes is not null;
}
