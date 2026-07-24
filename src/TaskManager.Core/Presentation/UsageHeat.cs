namespace TaskManager.Core.Presentation;

/// <summary>
/// Three-step "usage-heat emphasis" for the numeric columns (spec §6). A converter maps
/// these to Fluent text brushes so a hot process reads at a glance.
/// </summary>
public enum UsageHeat
{
    Low,
    Medium,
    High,
}

/// <summary>Thresholds for the heat buckets, kept in one place so both columns agree.</summary>
public static class Heat
{
    private const ulong MegaByte = 1024UL * 1024UL;

    public static UsageHeat ForCpu(double? cpuPercent) => cpuPercent switch
    {
        null => UsageHeat.Low,
        >= 8.0 => UsageHeat.High,
        >= 3.0 => UsageHeat.Medium,
        _ => UsageHeat.Low,
    };

    public static UsageHeat ForMemory(ulong? bytes) => bytes switch
    {
        null => UsageHeat.Low,
        >= 800 * MegaByte => UsageHeat.High,
        >= 300 * MegaByte => UsageHeat.Medium,
        _ => UsageHeat.Low,
    };
}
