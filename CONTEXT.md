# CONTEXT — task-manager

The shared vocabulary for this project. When code, issues, tests, or docs name one of
these concepts, use the term as defined here — don't drift to synonyms. New terms are
added lazily as decisions resolve (see `docs/agents/domain.md`).

The authoritative product definition is [`docs/spec/v1.md`](docs/spec/v1.md); this file
is the glossary that spec and its implementation share.

## Glossary

- **App** — a process the user thinks of as a foreground application. Operationally: a
  process owning at least one **qualifying window**. Shown in the **Apps** view. See
  spec §7.

- **Qualifying window** — a top-level window that is *all* of: visible, not a tool
  window (`WS_EX_TOOLWINDOW`), unowned (no `GetWindow(GW_OWNER)`), and not DWM-cloaked
  (`DWMWA_CLOAKED`). Owning at least one is exactly what makes a process an **App**
  (spec §7); "top-level" is the broader set the rule is evaluated over.

- **Background process** — any process that is not an **App** by the rule above
  (services' host processes, helpers, tray-only apps, suspended/cloaked packaged apps).
  Shown in the **Background processes** view. "Background" is a classification, not a
  Windows priority class.

- **Service** — a Windows service enumerated from the Service Control Manager
  (`EnumServicesStatusEx`), shown in the **Services** view with its display name,
  description, and run **Status**. **View-only** in v1 — no start/stop/restart.

- **View** — one of the three panes selected by the left navigation rail: **Apps**,
  **Background processes**, **Services**. The system graph strip is identical across all
  three.

- **Graph strip** — the pinned pair of cards at the top of the content area showing the
  **system-wide** CPU and memory graphs (rolling 60 s). Stays fixed while the process
  list scrolls beneath it.

- **CPU %** — for the graph: whole-machine CPU busy fraction from `GetSystemTimes`
  deltas. For a process row: that process's share from `GetProcessTimes` deltas over the
  same 1 Hz interval.

- **Memory** — for a process row, the **Private Working Set**
  (`PROCESS_MEMORY_COUNTERS_EX2.PrivateWorkingSetSize`), matching Windows Task Manager's
  Memory column. For the system graph, physical memory in use via `GlobalMemoryStatusEx`
  (commit charge via `GetPerformanceInfo`).

- **Tick** — one iteration of the single 1 Hz polling loop that samples every counter
  (process table, service states, both graphs). All refresh is driven off this one tick.

- **End task** — the only mutating action: terminate the selected process via
  `TerminateProcess` after an unconditional confirm dialog. Deliberate select-then-kill.

- **Blank cell** / **graceful degradation** — when the app can't open a process
  (elevation-gated), its CPU/Memory cells render blank rather than erroring; the row
  still appears. Unqueryable services are omitted from the Services view. The read path
  never requires elevation.

- **Restart as administrator** — the elevate affordance offered when **End task** fails
  with Access Denied: relaunches the whole app elevated (UAC, `runas` verb). The app
  never pre-disables End task; it attempts, then surfaces this.

- **Fluent** — the Windows 11 visual system this app targets: WinUI 3 native controls,
  `MicaBackdrop`, automatic system dark/light. Reference-grade, not simulated.
