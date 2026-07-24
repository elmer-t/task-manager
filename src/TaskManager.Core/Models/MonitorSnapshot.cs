namespace TaskManager.Core.Models;

/// <summary>
/// Everything one <b>tick</b> produced (spec §5): the system reading behind the graph
/// strip plus the full process and service tables. Assembled off the UI thread, then
/// handed to the view models on the UI thread.
/// </summary>
/// <param name="System">System-wide CPU/memory for the graph strip.</param>
/// <param name="Processes">Every process (complete list; some rows may lack metrics).</param>
/// <param name="Services">Enumerable services with states.</param>
public sealed record MonitorSnapshot(
    SystemSample System,
    IReadOnlyList<ProcessSample> Processes,
    IReadOnlyList<ServiceSample> Services);
