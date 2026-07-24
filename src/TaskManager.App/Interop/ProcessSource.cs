using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Models;
using TaskManager.Core.Monitoring;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.Threading;

namespace TaskManager.App.Interop;

/// <summary>
/// The full process table for one tick (spec §4): a toolhelp snapshot (needs no handles,
/// so the list is always complete), window-based App/Background classification, and
/// per-process CPU %/Private Working Set for the processes we can open. Processes we can't
/// open keep <see langword="null"/> metrics → blank cells (spec §4). Stateful: remembers
/// each process's previous CPU time to compute the 1 Hz delta.
/// </summary>
internal sealed class ProcessSource : IProcessSource
{
    private readonly WindowClassifier _classifier = new();
    private readonly Dictionary<uint, ProcessCpuState> _previous = new();

    private bool _hasSystemPrevious;
    private ulong _previousSystemTotal;

    public IReadOnlyList<ProcessSample> Sample()
    {
        ProcessClassification classification = _classifier.ClassifyProcesses();
        ulong systemDelta = SampleSystemTotalDelta();

        var samples = new List<ProcessSample>();
        var alive = new HashSet<uint>();

        foreach ((uint processId, string name) in EnumerateProcesses())
        {
            alive.Add(processId);
            (double? cpu, ulong? memory, string? path) = SampleProcessMetrics(processId, systemDelta);
            samples.Add(new ProcessSample(
                (int)processId, name, classification.Kind(processId), cpu, memory, path));
        }

        PruneDeadProcesses(alive);
        return samples;
    }

    /// <summary>
    /// Reads the machine-wide kernel+user CPU delta that is the denominator for every
    /// process's CPU share this tick. Kept here (not shared with the graph's metrics
    /// source) so the two stateful samplers stay independent; the extra GetSystemTimes
    /// call is negligible (spec §5).
    /// </summary>
    private unsafe ulong SampleSystemTotalDelta()
    {
        FILETIME kernel, user;
        if (!PInvoke.GetSystemTimes(null, &kernel, &user))
        {
            return 0;
        }

        ulong total = kernel.ToUInt64() + user.ToUInt64();
        ulong delta = _hasSystemPrevious ? CpuMath.Delta(_previousSystemTotal, total) : 0;
        _previousSystemTotal = total;
        _hasSystemPrevious = true;
        return delta;
    }

    private (double? cpu, ulong? memory, string? path) SampleProcessMetrics(uint processId, ulong systemDelta)
    {
        using var handle = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
            bInheritHandle: false,
            processId);

        if (handle.IsInvalid)
        {
            // Elevation-gated (spec §4): keep the row, blank its metric cells and let the
            // icon fall back to the generic placeholder (no path available).
            return (null, null, null);
        }

        double? cpu = SampleCpuPercent(handle, processId, systemDelta);
        ulong? memory = SamplePrivateWorkingSet(handle);
        string? path = QueryImagePath(handle);
        return (cpu, memory, path);
    }

    /// <summary>
    /// The full Win32 path to the process image (spec §6 icon source). Uses
    /// PROCESS_QUERY_LIMITED_INFORMATION — the handle we already hold — so it needs no extra
    /// rights. A path that doesn't fit MAX_PATH or can't be read yields <see langword="null"/>
    /// → the row's generic placeholder icon (spec §4 degradation).
    /// </summary>
    private static unsafe string? QueryImagePath(SafeHandle handle)
    {
        const int capacity = 260; // MAX_PATH
        char* buffer = stackalloc char[capacity];
        uint size = capacity;

        if (!PInvoke.QueryFullProcessImageName(
                handle,
                PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32,
                new PWSTR(buffer),
                ref size))
        {
            return null;
        }

        return size > 0 ? new string(buffer, 0, (int)size) : null;
    }

    private double? SampleCpuPercent(SafeHandle handle, uint processId, ulong systemDelta)
    {
        if (!PInvoke.GetProcessTimes(handle, out FILETIME creation, out _, out FILETIME kernel, out FILETIME user))
        {
            return null;
        }

        ulong createdAt = creation.ToUInt64();
        ulong busy = kernel.ToUInt64() + user.ToUInt64();

        // A reused PID (same number, new process) resets the baseline so we never report a
        // bogus spike from subtracting a different process's counter.
        double percent = 0.0;
        if (_previous.TryGetValue(processId, out ProcessCpuState prior) &&
            prior.CreatedAt == createdAt &&
            systemDelta > 0)
        {
            percent = CpuMath.ProcessPercent(CpuMath.Delta(prior.Busy, busy), systemDelta);
        }

        _previous[processId] = new ProcessCpuState(createdAt, busy);
        return percent;
    }

    private static unsafe ulong? SamplePrivateWorkingSet(SafeHandle handle)
    {
        var counters = new PROCESS_MEMORY_COUNTERS_EX2
        {
            cb = (uint)sizeof(PROCESS_MEMORY_COUNTERS_EX2),
        };

        // Use the pointer-based extern so we can pass the EX2 layout (its
        // PrivateWorkingSetSize is the Memory column source, spec §4). The SafeHandle keeps
        // the handle alive for the duration of this call.
        bool ok = PInvoke.GetProcessMemoryInfo(
            new HANDLE(handle.DangerousGetHandle()),
            (PROCESS_MEMORY_COUNTERS*)&counters,
            counters.cb);

        return ok ? (ulong)counters.PrivateWorkingSetSize : null;
    }

    private static IEnumerable<(uint ProcessId, string Name)> EnumerateProcesses()
    {
        using var snapshot = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
        if (snapshot.IsInvalid)
        {
            yield break;
        }

        var entry = new PROCESSENTRY32W { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>() };
        if (!PInvoke.Process32FirstW(snapshot, ref entry))
        {
            yield break;
        }

        do
        {
            yield return (entry.th32ProcessID, entry.szExeFile.ToString());
        }
        while (PInvoke.Process32NextW(snapshot, ref entry));
    }

    private void PruneDeadProcesses(HashSet<uint> alive)
    {
        if (_previous.Count == 0)
        {
            return;
        }

        var dead = _previous.Keys.Where(pid => !alive.Contains(pid)).ToList();
        foreach (uint pid in dead)
        {
            _previous.Remove(pid);
        }
    }

    private readonly record struct ProcessCpuState(ulong CreatedAt, ulong Busy);
}
