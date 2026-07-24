using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Collections;
using TaskManager.Core.Models;
using TaskManager.Core.Monitoring;
using TaskManager.Core.Presentation;

namespace TaskManager.App.ViewModels;

/// <summary>
/// The window's root view model. Owns the three lists and the pinned graph strip, applies
/// each tick's snapshot by in-place reconciliation, and owns the <em>selection</em> the End
/// task flow (spec §8) acts on — the flow's ordering itself belongs to
/// <see cref="EndTaskFlow"/>.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly EndTaskFlow _endTask;
    private readonly IProcessIconResolver _iconResolver;

    public MainViewModel(EndTaskFlow endTask, IProcessIconResolver iconResolver)
    {
        _endTask = endTask;
        _iconResolver = iconResolver;
    }

    public GraphStripViewModel GraphStrip { get; } = new();

    public ObservableCollection<ProcessRowViewModel> Apps { get; } = new();

    public ObservableCollection<ProcessRowViewModel> Background { get; } = new();

    public ObservableCollection<ServiceRowViewModel> Services { get; } = new();

    private ViewDescriptor CurrentView => ViewDescriptor.For(SelectedView);

    /// <summary>The process collection behind the currently selected view (Apps or Background).</summary>
    public ObservableCollection<ProcessRowViewModel> CurrentProcesses =>
        SelectedView == ViewKind.Background ? Background : Apps;

    public bool IsProcessView => CurrentView.IsProcessView;

    public bool IsServicesView => !CurrentView.IsProcessView;

    public string HeaderText => CurrentView.Header;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProcesses))]
    [NotifyPropertyChangedFor(nameof(IsProcessView))]
    [NotifyPropertyChangedFor(nameof(IsServicesView))]
    [NotifyPropertyChangedFor(nameof(HeaderText))]
    [NotifyCanExecuteChangedFor(nameof(EndTaskCommand))]
    private ViewKind _selectedView = ViewKind.Apps;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndTaskCommand))]
    private ProcessRowViewModel? _selectedProcess;

    partial void OnSelectedViewChanged(ViewKind value)
    {
        // Switching views is a fresh context — drop the previous view's selection.
        SelectedProcess = null;
    }

    /// <summary>Applies one tick's data to the UI (called on the UI thread).</summary>
    public void Apply(MonitorSnapshot snapshot)
    {
        GraphStrip.Update(snapshot.System);

        var apps = new List<ProcessSample>();
        var background = new List<ProcessSample>();
        foreach (ProcessSample process in snapshot.Processes)
        {
            (process.Kind == ProcessKind.App ? apps : background).Add(process);
        }

        CollectionSync.Apply(Apps, apps,
            static s => s.ProcessId, static r => r.ProcessId,
            s => new ProcessRowViewModel(s, _iconResolver), static (r, s) => r.Update(s));

        CollectionSync.Apply(Background, background,
            static s => s.ProcessId, static r => r.ProcessId,
            s => new ProcessRowViewModel(s, _iconResolver), static (r, s) => r.Update(s));

        CollectionSync.Apply(Services, snapshot.Services,
            static s => s.ServiceName, static r => r.ServiceName,
            static s => new ServiceRowViewModel(s), static (r, s) => r.Update(s));

        // If the selected process was killed / vanished, clear the stale selection.
        if (SelectedProcess is not null && !CurrentProcesses.Contains(SelectedProcess))
        {
            SelectedProcess = null;
        }
    }

    private bool CanEndTask() => IsProcessView && SelectedProcess is not null;

    [RelayCommand(CanExecute = nameof(CanEndTask))]
    private async Task EndTaskAsync()
    {
        ProcessRowViewModel? target = SelectedProcess;
        if (target is null)
        {
            return;
        }

        TerminationOutcome? outcome = await _endTask.EndAsync(target.ProcessId, target.Name);

        // The process is gone either way, so the selection goes with it; every other
        // outcome (declined confirm included) leaves the row selected to try again.
        if (outcome is TerminationOutcome.Success or TerminationOutcome.NotFound)
        {
            SelectedProcess = null;
        }
    }
}
