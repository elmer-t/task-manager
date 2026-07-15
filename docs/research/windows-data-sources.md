# Windows data sources for processes, services, and CPU/memory counters

Research for issue [#3](https://github.com/elmer-t/task-manager/issues/3). Sources are official
Microsoft Learn documentation unless marked otherwise; all claims link to the page that owns them.

## Summary of recommendations

| Need | Recommended API | Elevation needed? |
| --- | --- | --- |
| 1. Enumerate processes + app/background split | `CreateToolhelp32Snapshot` (or `EnumProcesses`) + `EnumWindows`/`IsWindowVisible`/`GetWindowThreadProcessId` + `DWMWA_CLOAKED` filter | No |
| 2. List services and states | SCM: `OpenSCManager` + `EnumServicesStatusEx` | No (read); admin for start/stop of most services |
| 3. System-wide CPU% + memory at ~1 Hz | `GetSystemTimes` (CPU) + `GlobalMemoryStatusEx` (memory), optionally `GetPerformanceInfo` for commit charge | No |
| 4. Per-process CPU% and memory columns | `GetProcessTimes` + `GetProcessMemoryInfo` per PID via `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` | No, but some rows degrade (see below) |
| 5. Kill a process | `OpenProcess(PROCESS_TERMINATE)` + `TerminateProcess` | No for own processes; admin/SeDebugPrivilege for other users' processes |

Everything a Task Manager clone needs at ~1 Hz is served by cheap, unprivileged Win32 calls.
PDH is a reasonable alternative for counter-style data but adds localization and
instance-name-collision pitfalls; WMI/CIM is the heaviest option and is best reserved for
one-shot queries; ETW is not warranted at this sampling rate.

---

## 1. Enumerating processes and classifying "app" vs background

### Candidate APIs

- **Toolhelp** — `CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS)` + `Process32First/Next` returns a
  read-only snapshot of all processes with PID, parent PID, thread count, and exe name per entry.
  ([CreateToolhelp32Snapshot](https://learn.microsoft.com/windows/win32/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot))
- **PSAPI** — `EnumProcesses` returns only PIDs; you must then `OpenProcess` each PID to get names
  via `EnumProcessModules`/`GetModuleBaseName`, and that open fails for protected/system processes
  (Idle, CSRSS), so names come back `<unknown>` for those.
  ([EnumProcesses](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-enumprocesses),
  [Enumerating All Processes](https://learn.microsoft.com/windows/win32/psapi/enumerating-all-processes))
- **NtQuerySystemInformation(SystemProcessInformation)** — one call returns per-process resource
  usage (threads, handles, memory, per-process kernel/user times) for every process without opening
  handles. Microsoft's own docs warn it "may be altered or unavailable in future versions" and to
  prefer the documented alternatives, and it must be loaded via `GetProcAddress` from ntdll.
  ([NtQuerySystemInformation](https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation))
  Windows 11 26100.4770 adds a cheaper `SystemBasicProcessInformation` class (name + PID + sequence
  number, "faster, consumes less memory") — a signal that even Microsoft treats the full class as
  heavy. (Same page.)
- **WMI `Win32_Process`** — full process inventory over COM/CIM; ergonomic from scripts and .NET
  but each query goes through the WMI service. Fine for one-shots, wasteful at 1 Hz (see §6).

**Recommendation: Toolhelp snapshot** for the 1 Hz process list. It is documented for exactly this
purpose, needs no per-process handle just to get names/PIDs, and *"all users have read access to
the list of processes in the system"* — process enumeration itself never needs elevation.
([Process Enumeration](https://learn.microsoft.com/windows/win32/procthread/process-enumeration))
For the full image path of a process, `QueryFullProcessImageName` works with a
`PROCESS_QUERY_LIMITED_INFORMATION` handle, which succeeds across user boundaries far more often
than `PROCESS_QUERY_INFORMATION`.
([Process Security and Access Rights](https://learn.microsoft.com/windows/win32/procthread/process-security-and-access-rights))

### Classifying "app with visible window" vs background

There is no per-process "is an app" flag; Task Manager derives it from window ownership. The
documented recipe:

1. `EnumWindows` enumerates all top-level windows (on Windows 8+ it only enumerates desktop-app
   top-level windows).
   ([EnumWindows](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-enumwindows))
2. Map each HWND to its PID with `GetWindowThreadProcessId`.
   ([Window functions](https://learn.microsoft.com/windows/win32/winmsg/window-functions))
3. Keep only windows that pass `IsWindowVisible` (WS_VISIBLE on the window and its ancestors —
   note it can be nonzero even if fully obscured).
   ([IsWindowVisible](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-iswindowvisible))
4. Exclude owned windows and tool windows: taskbar buttons are created only for unowned windows,
   and `WS_EX_TOOLWINDOW` windows are deliberately kept off the taskbar, so a "has a taskbar-style
   window" test should skip windows with an owner (`GW_OWNER != NULL`) unless they carry
   `WS_EX_APPWINDOW`.
   ([The Taskbar — Managing Taskbar Buttons](https://learn.microsoft.com/windows/win32/shell/taskbar#about-the-taskbar),
   [About Windows — owner windows](https://learn.microsoft.com/windows/win32/winmsg/about-windows#window-attributes))
5. Exclude *cloaked* windows: suspended UWP apps keep a WS_VISIBLE top-level window that DWM has
   cloaked. Query `DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, ...)`; nonzero means cloaked by the
   app, the Shell, or inherited.
   ([DWMWINDOWATTRIBUTE](https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute))

A process whose PID owns at least one window surviving these filters is an "App"; everything else
is background. This is a UI-session heuristic: it requires running in the interactive session
(window handles are per-session) but no elevation.

## 2. Listing services and their states

**Recommendation: Service Control Manager directly** — `OpenSCManager(..., SC_MANAGER_CONNECT |
SC_MANAGER_ENUMERATE_SERVICE)` then `EnumServicesStatusEx` with `SC_ENUM_PROCESS_INFO`, which
returns one `ENUM_SERVICE_STATUS_PROCESS` per service: service name, display name, current state
(running/stopped/paused/pending), and — usefully for joining against the process list — the PID
hosting the service.
([EnumServicesStatusEx](https://learn.microsoft.com/windows/win32/api/winsvc/nf-winsvc-enumservicesstatusexa))

Elevation facts, from [Service Security and Access Rights](https://learn.microsoft.com/windows/win32/services/service-security-and-access-rights):

- **Local authenticated users are granted `SC_MANAGER_CONNECT`, `SC_MANAGER_ENUMERATE_SERVICE`,
  `SC_MANAGER_QUERY_LOCK_STATUS`, and `STANDARD_RIGHTS_READ`** — enumerating services and states
  needs no elevation.
- If the caller lacks `SERVICE_QUERY_STATUS` on a particular service, that service is *silently
  omitted* from `EnumServicesStatusEx` results — the graceful-degradation mode is "a few rows
  missing," not an error.
- Only administrators get `SC_MANAGER_ALL_ACCESS`; creating services or locking the database is
  admin-only. Starting/stopping a service requires `SERVICE_START`/`SERVICE_STOP` on that service's
  own DACL, which for most system services means elevation.

WMI's `Win32_Service` returns the same inventory plus config (start mode, account) with more
per-query overhead; PowerShell's `Get-Service`/`Get-CimInstance` are fine interactive equivalents.

## 3. System-wide CPU% and memory at ~1 Hz

### CPU

**Recommendation: `GetSystemTimes`.** One unprivileged kernel32 call returns cumulative idle,
kernel, and user FILETIMEs summed across all processors (kernel time *includes* idle time).
CPU% for an interval = `1 - Δidle / (Δkernel + Δuser)` between two samples. Caveat: on systems
with more than 64 logical processors it only covers the calling thread's primary processor group.
([GetSystemTimes](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes))
The kernel's own docs for `SystemProcessorPerformanceInformation` point callers at
`GetSystemTimes` as the supported way to get this data.
([NtQuerySystemInformation — SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION](https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntquerysysteminformation#parameters))

PDH alternative: `\Processor(_Total)\% Processor Time` (or the newer `Processor Information`
counterset) via `PdhOpenQuery`/`PdhAddEnglishCounter`/`PdhCollectQueryData`. PDH's own workflow is
built around exactly our cadence — collect, wait "a minimum of one second," collect again, format —
so 1 Hz is its designed operating point and overhead is modest.
([Collecting Performance Data](https://learn.microsoft.com/windows/win32/perfctrs/collecting-performance-data))
Two ergonomics warnings: counter paths are localized, so always use `PdhAddEnglishCounter` rather
than hard-coding English paths into `PdhAddCounter`
([PdhAddEnglishCounter](https://learn.microsoft.com/windows/win32/api/pdh/nf-pdh-pdhaddenglishcountera#remarks));
and `\Processor(_Total)\% Processor Time` is the *average* across processors (range 0–100), while
`\Process(X)\% Processor Time` ranges 0–100×ProcessorCount
([Understanding Multiple Processor Counters](https://learn.microsoft.com/windows/win32/perfctrs/collecting-performance-data#understanding-multiple-processor-counters)).

Accuracy note: Task Manager's CPU figure includes interrupt/DPC time ("System Interrupts"), which
neither `GetSystemTimes`-derived process sums nor `GetProcessTimes` attribute to a process, so
small discrepancies vs Task Manager are expected and documented.
([Microsoft Q&A — CPU usage difference between GetProcessTimes and Task Manager](https://learn.microsoft.com/answers/a/12257733))

### Memory

**Recommendation: `GlobalMemoryStatusEx`.** One call fills `MEMORYSTATUSEX` with `dwMemoryLoad`
(0–100 "% physical memory in use" — Task Manager's headline number), `ullTotalPhys`, and
`ullAvailPhys` (standby + free + zeroed lists). No elevation.
([GlobalMemoryStatusEx](https://learn.microsoft.com/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex),
[MEMORYSTATUSEX](https://learn.microsoft.com/windows/win32/api/sysinfoapi/ns-sysinfoapi-memorystatusex#members))
For a commit-charge graph, `GetPerformanceInfo` supplies system-wide `CommitTotal`/`CommitLimit` —
the MEMORYSTATUSEX docs themselves defer to it for system-wide commit.
(Same MEMORYSTATUSEX page, `ullTotalPageFile`/`ullAvailPageFile` remarks.)

## 4. Per-process CPU% and memory columns

**Recommendation: per-PID `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` once per process
lifetime, then each tick:**

- **CPU:** `GetProcessTimes` returns cumulative kernel and user FILETIMEs for the process; the
  required access is `PROCESS_QUERY_INFORMATION` *or* `PROCESS_QUERY_LIMITED_INFORMATION`.
  Per-process CPU% = Δ(kernel+user) / Δwall-clock (divide by core count for a 0–100 scale).
  ([GetProcessTimes](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes))
- **Memory:** `GetProcessMemoryInfo` (same limited access right on Vista+) fills
  `PROCESS_MEMORY_COUNTERS_EX`: `WorkingSetSize`, and `PrivateUsage` = commit charge, which is what
  Task Manager shows as "Commit Size"; Private Bytes ≡ `PrivateUsage`.
  ([GetProcessMemoryInfo](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-getprocessmemoryinfo),
  [PROCESS_MEMORY_COUNTERS_EX](https://learn.microsoft.com/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex),
  [Memory Performance Information](https://learn.microsoft.com/windows/win32/memory/memory-performance-information#process-memory-performance-information))
  On Windows 10/11 22H2 (Sept 2023 update)+, `PROCESS_MEMORY_COUNTERS_EX2` adds
  `PrivateWorkingSetSize` — the "Private Working Set" column modern Task Manager displays —
  without needing PDH.
  ([PROCESS_MEMORY_COUNTERS_EX2](https://learn.microsoft.com/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex2))

Degradation without elevation: `OpenProcess` fails with `ERROR_ACCESS_DENIED` for the System
process, CSRSS, and protected processes regardless of privileges, and may fail for other users'
processes without `SeDebugPrivilege`; the documented pattern is to show the row with blank
columns rather than fail the listing.
([OpenProcess](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess),
[Enumerating All Processes](https://learn.microsoft.com/windows/win32/psapi/enumerating-all-processes))
`PROCESS_QUERY_LIMITED_INFORMATION` is specifically the right that ordinary callers are most
likely to be granted; requesting the stronger `PROCESS_QUERY_INFORMATION` gratuitously loses rows.
([Process Security and Access Rights](https://learn.microsoft.com/windows/win32/procthread/process-security-and-access-rights))

PDH alternative (`\Process(name)\% Processor Time`, `Working Set - Private`): works, but PDH
matches process instances *by name with an index suffix*, and Microsoft documents that when
same-named processes exit, PDH can pair a sample from a dead process with one from the process
that slid into its slot, producing wrong formatted values — a real hazard for a per-process table
refreshed every second.
([Understanding Multiple Processor Counters](https://learn.microsoft.com/windows/win32/perfctrs/collecting-performance-data#understanding-multiple-processor-counters))
The handle-based approach keyed by PID avoids this class of bug entirely.

## 5. Killing a process

**Recommendation: `OpenProcess(PROCESS_TERMINATE, FALSE, pid)` + `TerminateProcess(h, code)`**,
then optionally `WaitForSingleObject` since termination is asynchronous.
([TerminateProcess](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-terminateprocess))

Elevation semantics:

- The open is checked against the target's DACL, so killing your own processes needs no elevation.
- With `SeDebugPrivilege` enabled, the requested access is granted regardless of the DACL — this
  privilege is held (disabled by default, enable via `AdjustTokenPrivileges`) by administrators.
  So "End task on another user's / a service's process" is the admin-only operation.
  ([OpenProcess](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess),
  [Debug Privilege](https://learn.microsoft.com/windows-hardware/drivers/debugger/debug-privilege))
- Protected processes and System/CSRSS cannot be opened for terminate even by admins.
  ([OpenProcess parameters](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess))
- Graceful degradation: attempt the open; on `ERROR_ACCESS_DENIED` surface "Access denied — try
  running elevated" instead of hiding the action.

WMI's `Win32_Process.Terminate` is an equivalent remote-capable path with the same
`SeDebugPrivilege` requirement for processes you do not own.
([Win32_Process.Terminate](https://learn.microsoft.com/windows/win32/cimwin32prov/terminate-method-in-class-win32-process#remarks))

## 6. Cross-cutting comparison at ~1 Hz

| API family | Accuracy | Overhead at 1 Hz | Ergonomics | Elevation |
| --- | --- | --- | --- | --- |
| Win32/toolhelp/PSAPI/`GetSystemTimes`/`GlobalMemoryStatusEx` | Exact raw counters; you own the delta math | Lowest — direct syscalls, one snapshot + N cheap handle queries per tick | Manual bookkeeping (previous-sample cache keyed by PID), but simple, PID-keyed, and unambiguous | None for read; SeDebugPrivilege only widens terminate/open coverage |
| PDH | Same underlying counters, pre-cooked formulas | Low; designed for ≥1 s collection intervals | Counter-path strings; localization requires `PdhAddEnglishCounter`; per-process instance-name aliasing bug documented by Microsoft | None for the counters we need (some countersets are ACL-protected — Get-Counter docs note admin reveals all sets) |
| WMI/CIM (`Win32_Process`, `Win32_Service`, `Win32_PerfFormattedData_*`) | Same data, formatted classes pre-calculate | Highest — COM to the WMI service per query; perf classes additionally require refresher objects and a throwaway first sample | Best for scripts/one-shots and remote machines; verbose from native code | None for reads shown here; `SeDebugPrivilege` for `Terminate` on others' processes |
| `NtQuerySystemInformation` | Richest single-call snapshot (per-process times without handles) | Very low per call | Undocumented-ish: struct layouts subject to change, GetProcAddress-only, explicit MS warning | None |
| ETW | Event-level precision (context switches, exact scheduling) | Continuous kernel session + real-time consumer — far more machinery than sampling needs | Controller/provider/consumer model; heavy | Controlling sessions requires admin or Performance Log Users; NT Kernel Logger requires admin/LocalSystem ([StartTrace](https://learn.microsoft.com/windows/win32/api/evntrace/nf-evntrace-starttracea#return-value)) |

**ETW is not warranted** for a 1 Hz task-manager UI: it exists for tracing/profiling, its session
control is privilege-gated, and sampling APIs already match Task Manager's own fidelity.

## 7. Per-stack access paths

The recommended set is plain Win32 (kernel32/user32/advapi32/psapi/dwmapi), so every stack reaches
it the same way; only the binding differs.

- **.NET:** `System.Diagnostics.Process` wraps most of this (`GetProcesses`,
  `TotalProcessorTime`, `WorkingSet64`, `PrivateMemorySize64` — documented as equivalent to the
  Private Bytes counter — `MainWindowHandle`, `Kill`), and `System.ServiceProcess.ServiceController`
  wraps the SCM. Gaps (cloaked-window check, `GetSystemTimes`, `PROCESS_MEMORY_COUNTERS_EX2`) are
  small P/Invokes. `PerformanceCounter` covers the PDH route.
  ([Process.PrivateMemorySize64](https://learn.microsoft.com/dotnet/api/system.diagnostics.process.privatememorysize64))
- **Rust:** the `windows`/`windows-sys` crates expose all the listed functions
  (`Win32::System::Diagnostics::ToolHelp`, `ProcessStatus`, `Threading`, `Services`,
  `Win32::UI::WindowsAndMessaging`, `Win32::Graphics::Dwm`) as direct bindings; the `sysinfo`
  crate is a common cross-platform convenience layer over the same sources.
- **Node/Electron:** no built-in binding; use a native addon (N-API/`koffi`) over the same Win32
  calls, or an existing module (e.g. `systeminformation`, which shells out/uses PowerShell —
  noticeably heavier at 1 Hz). For an Electron task-manager UI, a small native module doing the
  snapshot + delta math in C/C++/Rust and posting one JSON blob per tick is the pattern that
  keeps overhead near the raw-Win32 floor.
- **C/C++:** direct calls; everything above links against kernel32/advapi32/user32/dwmapi (PSAPI
  functions live in kernel32 as `K32*` since Windows 7).
  ([GetProcessMemoryInfo remarks](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-getprocessmemoryinfo#remarks))

## 8. Explicit answer: what needs admin, what degrades

**Needs no elevation at all:**
process enumeration (all users have read access to the process list), the window-based
app/background classification, service enumeration with states and PIDs, system-wide CPU
(`GetSystemTimes`), system memory (`GlobalMemoryStatusEx`/`GetPerformanceInfo`), per-process
CPU/memory for your own and most visible processes, and killing your own processes.

**Needs elevation (admin, which enables `SeDebugPrivilege`):**
per-process CPU/memory and terminate for *other users'* and service processes; starting/stopping
most services; controlling ETW sessions (admin or Performance Log Users group).

**Never works, even elevated:**
opening System, CSRSS, or protected processes for terminate/full query.

**Graceful degradation story for a non-elevated run:**
the process list is complete (names/PIDs from the toolhelp snapshot never require handles); rows
whose `OpenProcess` fails simply show blank CPU/memory cells; the services list is complete except
services whose DACL denies `SERVICE_QUERY_STATUS`, which are silently omitted; both system graphs
are fully functional; End Task works for the user's own processes and returns Access Denied for
others — mirror Task Manager by offering an "elevate" affordance rather than pre-blocking.
