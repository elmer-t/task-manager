namespace TaskManager.Core.Models;

/// <summary>
/// One Windows service as enumerated from the Service Control Manager on a tick
/// (spec §4). <b>View-only</b> in v1 — no start/stop/restart (spec §2). A service the
/// caller cannot query is silently omitted upstream rather than shown in error (spec §4).
/// </summary>
/// <param name="ServiceName">SCM key name, e.g. <c>Spooler</c>.</param>
/// <param name="DisplayName">Friendly display name, e.g. <c>Print Spooler</c>.</param>
/// <param name="Description">
/// Static description text, or <see langword="null"/> when it could not be read
/// (blank Description cell — the row still appears).
/// </param>
/// <param name="Status">Running / Stopped pill state (spec §6).</param>
/// <param name="HostProcessId">
/// PID hosting the service, or <see langword="null"/> when not running / unknown.
/// </param>
public sealed record ServiceSample(
    string ServiceName,
    string DisplayName,
    string? Description,
    ServiceStatus Status,
    int? HostProcessId);
