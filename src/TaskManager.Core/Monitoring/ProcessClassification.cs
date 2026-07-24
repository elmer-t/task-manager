using TaskManager.Core.Models;

namespace TaskManager.Core.Monitoring;

/// <summary>
/// One <b>Tick</b>'s §7 verdict for every process: which PIDs own at least one
/// <b>qualifying window</b>, and therefore which rows belong in the <b>Apps</b> view.
/// Produced by <see cref="ClassificationRule.Classify"/> and asked about a PID at a time
/// while the process table is built.
/// </summary>
public readonly struct ProcessClassification
{
    private readonly IReadOnlySet<uint>? _appProcessIds;

    internal ProcessClassification(IReadOnlySet<uint> appProcessIds) => _appProcessIds = appProcessIds;

    /// <summary>
    /// The bucket for one process (spec §7). Total: a PID this tick saw no qualifying
    /// window for is a <b>Background process</b> — including a process that owns no windows
    /// at all, which is why nothing has to enumerate the windowless case separately.
    /// </summary>
    public ProcessKind Kind(uint processId) =>
        _appProcessIds is not null && _appProcessIds.Contains(processId)
            ? ProcessKind.App
            : ProcessKind.Background;
}
