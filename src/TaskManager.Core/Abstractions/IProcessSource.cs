using TaskManager.Core.Models;

namespace TaskManager.Core.Abstractions;

/// <summary>
/// Supplies the full process table for one tick — toolhelp snapshot, App/Background
/// classification, and per-process CPU/memory (spec §4). Implementations hold the
/// previous tick's CPU times to compute deltas, so a source is stateful and single-use
/// per monitor. The Win32 implementation lives in the App project.
/// </summary>
public interface IProcessSource
{
    /// <summary>
    /// Samples every process. The list is always complete (toolhelp needs no handles);
    /// rows the caller cannot open carry <see langword="null"/> metrics (spec §4).
    /// </summary>
    IReadOnlyList<ProcessSample> Sample();
}
