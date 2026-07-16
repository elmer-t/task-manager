namespace TaskManager.Core.Monitoring;

/// <summary>
/// The fixed cadence and history figures from spec §5. These are not user-facing
/// settings in v1 — there is deliberately no update-speed or history control.
/// </summary>
public static class MonitorConstants
{
    /// <summary>Sampling period: once per second (1 Hz) for every counter.</summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    /// <summary>Rolling graph history: 60 samples at 1 Hz == a 60-second window.</summary>
    public const int HistoryLength = 60;
}
