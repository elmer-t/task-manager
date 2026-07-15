using TaskManager.Core.Models;

namespace TaskManager.Core.Abstractions;

/// <summary>
/// Enumerates Windows services with their states for one tick (spec §4). A service the
/// caller cannot query is silently omitted (spec §4); descriptions are read once and
/// cached to keep per-tick overhead negligible (spec §5).
/// </summary>
public interface IServiceSource
{
    IReadOnlyList<ServiceSample> Sample();
}
