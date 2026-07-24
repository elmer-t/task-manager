using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Models;
using TaskManager.Core.Monitoring;
using Windows.Win32;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.SystemInformation;

namespace TaskManager.App.Interop;

/// <summary>
/// System-wide CPU and memory for the graph strip (spec §4 / §6), all from plain Win32:
/// GetSystemTimes for the CPU busy fraction, GlobalMemoryStatusEx for physical memory,
/// GetPerformanceInfo for the commit charge. No elevation needed — both graphs always work.
/// </summary>
internal sealed class SystemMetricsSource : ISystemMetricsSource
{
    // The one machine-wide CPU reading in the tree: it yields both the graph card's busy
    // percentage and the CPU denominator the process source divides by. The delta
    // arithmetic lives in SystemCpuInterval, so this field is the only state kept here.
    private SystemCpuInterval? _cpu;

    public SystemSample Sample()
    {
        SystemCpuInterval cpu = SampleCpu();
        (ulong memoryUsed, ulong memoryTotal) = SamplePhysicalMemory();
        (ulong commitUsed, ulong commitLimit) = SampleCommitCharge();
        return new SystemSample(
            cpu.BusyPercent, memoryUsed, memoryTotal, commitUsed, commitLimit, cpu.CpuDenominator);
    }

    private unsafe SystemCpuInterval SampleCpu()
    {
        FILETIME idle, kernel, user;
        if (!PInvoke.GetSystemTimes(&idle, &kernel, &user))
        {
            // A failed read measures nothing: 0 % on the card, a 0 denominator so every row
            // reads 0 %. The baseline is left alone, so the next successful read measures
            // across the gap rather than restarting.
            return default;
        }

        SystemCpuInterval reading = _cpu is { } previous
            ? previous.Next(idle.ToUInt64(), kernel.ToUInt64(), user.ToUInt64())
            : SystemCpuInterval.Start(idle.ToUInt64(), kernel.ToUInt64(), user.ToUInt64());

        _cpu = reading;
        return reading;
    }

    private static (ulong used, ulong total) SamplePhysicalMemory()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!PInvoke.GlobalMemoryStatusEx(ref status))
        {
            return (0, 0);
        }

        ulong total = status.ullTotalPhys;
        ulong used = total >= status.ullAvailPhys ? total - status.ullAvailPhys : 0;
        return (used, total);
    }

    private static (ulong used, ulong limit) SampleCommitCharge()
    {
        var info = new PERFORMANCE_INFORMATION { cb = (uint)Marshal.SizeOf<PERFORMANCE_INFORMATION>() };
        if (!PInvoke.GetPerformanceInfo(ref info, info.cb))
        {
            return (0, 0);
        }

        ulong pageSize = (ulong)info.PageSize;
        return ((ulong)info.CommitTotal * pageSize, (ulong)info.CommitLimit * pageSize);
    }
}
