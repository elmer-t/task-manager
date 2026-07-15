using System.Globalization;

namespace TaskManager.Core.Text;

/// <summary>
/// Number formatting for the process/service tables and graph readouts. Kept in Core so
/// the "842 MB" / "1.8 GB" / "12.4%" presentation is pinned down by tests rather than
/// scattered across XAML converters.
/// </summary>
public static class Humanize
{
    private const double BytesPerMb = 1024.0 * 1024.0;
    private const double MbPerGb = 1024.0;

    /// <summary>
    /// Memory as Task Manager shows it: whole/one-decimal MB, switching to one-decimal GB
    /// at 1024 MB. Small values keep one decimal so a busy row never collapses to "0 MB".
    /// </summary>
    public static string Bytes(ulong bytes)
    {
        double mb = bytes / BytesPerMb;
        if (mb >= MbPerGb)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{mb / MbPerGb:0.0} GB");
        }

        return mb < 10.0
            ? string.Create(CultureInfo.InvariantCulture, $"{mb:0.0} MB")
            : string.Create(CultureInfo.InvariantCulture, $"{Math.Round(mb):0} MB");
    }

    /// <summary>Nullable overload — a blank cell for an inaccessible process (spec §4).</summary>
    public static string BytesOrBlank(ulong? bytes) => bytes is null ? string.Empty : Bytes(bytes.Value);

    /// <summary>A percentage with <paramref name="decimals"/> places, e.g. "12.4%".</summary>
    public static string Percent(double value, int decimals = 1)
    {
        string format = "0." + new string('0', Math.Max(0, decimals));
        if (decimals == 0)
        {
            format = "0";
        }

        return value.ToString(format, CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>Nullable overload — a blank CPU cell for an inaccessible process (spec §4).</summary>
    public static string PercentOrBlank(double? value, int decimals = 1) =>
        value is null ? string.Empty : Percent(value.Value, decimals);
}
