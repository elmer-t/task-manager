using System.Runtime.InteropServices.ComTypes;

namespace TaskManager.App.Interop;

/// <summary>
/// Turns the FILETIME pairs that GetSystemTimes / GetProcessTimes report into a single
/// 64-bit tick count (100-ns units) for the delta math in
/// <see cref="TaskManager.Core.Monitoring.CpuMath"/>.
/// </summary>
internal static class FileTimeExtensions
{
    // FILETIME's fields are signed int32; go through uint so the low half's sign bit
    // never bleeds into the combined value.
    public static ulong ToUInt64(this FILETIME value) =>
        ((ulong)(uint)value.dwHighDateTime << 32) | (uint)value.dwLowDateTime;
}
