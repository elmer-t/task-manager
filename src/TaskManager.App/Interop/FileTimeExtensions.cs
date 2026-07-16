using Windows.Win32.Foundation;

namespace TaskManager.App.Interop;

/// <summary>
/// Turns the FILETIME pairs that GetSystemTimes / GetProcessTimes report into a single
/// 64-bit tick count (100-ns units) for the delta math in
/// <see cref="TaskManager.Core.Monitoring.CpuMath"/>.
/// </summary>
internal static class FileTimeExtensions
{
    public static ulong ToUInt64(this FILETIME value) =>
        ((ulong)value.dwHighDateTime << 32) | value.dwLowDateTime;
}
