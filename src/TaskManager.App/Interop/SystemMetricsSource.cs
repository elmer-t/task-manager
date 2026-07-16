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
    private bool _hasPrevious;
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;

    public SystemSample Sample()
    {
        double cpuPercent = SampleCpuPercent();
        (ulong memoryUsed, ulong memoryTotal) = SamplePhysicalMemory();
        (ulong commitUsed, ulong commitLimit) = SampleCommitCharge();
        return new SystemSample(cpuPercent, memoryUsed, memoryTotal, commitUsed, commitLimit);
    }

    private unsafe double SampleCpuPercent()
    {
        FILETIME idle, kernel, user;
        if (!PInvoke.GetSystemTimes(&idle, &kernel, &user))
        {
            return 0.0;
        }

        ulong idleTicks = idle.ToUInt64();
        ulong kernelTicks = kernel.ToUInt64();
        ulong userTicks = user.ToUInt64();

        if (!_hasPrevious)
        {
            Remember(idleTicks, kernelTicks, userTicks);
            return 0.0; // First reading has no interval to compare against.
        }

        ulong idleDelta = CpuMath.Delta(_previousIdle, idleTicks);
        ulong kernelDelta = CpuMath.Delta(_previousKernel, kernelTicks);
        ulong userDelta = CpuMath.Delta(_previousUser, userTicks);
        Remember(idleTicks, kernelTicks, userTicks);

        return CpuMath.SystemBusyPercent(idleDelta, kernelDelta, userDelta);
    }

    private void Remember(ulong idle, ulong kernel, ulong user)
    {
        _previousIdle = idle;
        _previousKernel = kernel;
        _previousUser = user;
        _hasPrevious = true;
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
