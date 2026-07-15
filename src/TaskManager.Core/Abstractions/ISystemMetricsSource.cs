using TaskManager.Core.Models;

namespace TaskManager.Core.Abstractions;

/// <summary>
/// Supplies the system-wide CPU and memory reading for one tick — the graph strip's data
/// (spec §4 / §6). Stateful: holds the previous GetSystemTimes reading for the CPU delta.
/// Both graphs always work and need no elevation.
/// </summary>
public interface ISystemMetricsSource
{
    SystemSample Sample();
}
