# ADR-0001: v1 tech stack — WinUI 3 (C#) + LiveCharts2

- **Status:** Accepted
- **Date:** 2026-07-15
- **Context ticket:** [Decide the tech stack (#4)](https://github.com/elmer-t/task-manager/issues/4), a decision ticket on [Wayfinder map: Windows task manager v1 spec & stack (#1)](https://github.com/elmer-t/task-manager/issues/1)

## Context

v1 is a personal, Fluent-styled Windows task manager (Apps / Background / Services
views, system-wide CPU & memory graphs, per-process CPU/mem columns, kill-process
as the only action). Per the map, it is both a daily-use tool and **a vehicle for
learning the chosen stack**, and *pragmatic choices win* — distribution barely matters.

Two research tickets fed this decision:

- [Compare candidate UI stacks (#2)](https://github.com/elmer-t/task-manager/issues/2)
  found all five candidates (WinUI 3, WPF, Avalonia, Tauri, Electron) viable. The
  differentiator is structural: the .NET trio draws **native** Fluent controls
  in-process with the lowest Windows-API friction (WinUI 3 = reference-grade,
  indistinguishable from system apps; WPF = maturing first-party theme; Avalonia =
  independent re-implementation), while Tauri/Electron get a real Mica **window**
  but rebuild Fluent **content** in HTML/CSS and pay a permanent backend↔UI IPC hop
  per metric stream. Charting at 1 Hz is far below every candidate's ceiling and
  does not differentiate.
- [Map the Windows data sources (#3)](https://github.com/elmer-t/task-manager/issues/3)
  found plain Win32 is the right, lowest-overhead path for every data need, with no
  elevation required for the read path.

## Decision

Build v1 with:

- **Language:** C#
- **UI framework:** WinUI 3 / Windows App SDK
- **Charting:** LiveCharts2

### Why WinUI 3

The learning goal is **modern native Windows**. WinUI 3 is the first-party modern
native stack and renders reference-grade Fluent (real WinUI controls, `MicaBackdrop`
one-liner, automatic system dark/light and automatic Mica fallback when the OS
suppresses it). It also has the lowest Windows-API friction — process, service, and
performance data are read in a single process with no IPC boundary.

Accepted trade-offs: XAML Hot Reload but **no visual designer**, and community
concerns about the pace/openness of the Windows App SDK. WPF was the considered
fallback (same low-friction .NET data access, best tooling, most documented) but its
Fluent theme is still `[Experimental]` with partial coverage and its core is old
tech — a weaker fit for the "modern native" goal.

### Why LiveCharts2

The CPU/memory graphs are simple scrolling filled-line series sampled at ~1 Hz —
trivial for any option. LiveCharts2 is chosen for **fit**, not performance: it is
SkiaSharp-based, MVVM/XAML-idiomatic (binds to observable collections, animates, and
themes to match Fluent dark/light), which reinforces the WinUI data-binding style
this project exists to learn. ScottPlot 5 (simpler, more utilitarian) and a
hand-rolled Win2D/Canvas approach were considered and set aside.

## Consequences

- Single-process architecture: perf counters are polled and charts painted in the
  same process — no IPC channel to design or learn.
- Mica is Windows 11-only and OS-suppressible; WinUI 3 handles the fallback
  automatically, so no manual fallback code is required.
- Tooling: XAML Hot Reload only — no visual designer. Layout is authored in XAML by hand.
- **Data access (pointer, not decided here):** the raw-Win32 recommendation from #3
  is reached on C#/WinUI via **CsWin32** source-generated P/Invoke for the APIs that
  have no clean managed wrapper (e.g. `GetProcessMemoryInfo` /
  `PROCESS_MEMORY_COUNTERS_EX2.PrivateWorkingSetSize`, `EnumServicesStatusEx`,
  `GetSystemTimes`). The precise binding surface is left to the v1 spec ([#6](https://github.com/elmer-t/task-manager/issues/6)).
