using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.App.Monitoring;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Models;

namespace TaskManager.App.ViewModels;

/// <summary>
/// The window's root view model. Owns the three lists and the pinned graph strip, applies
/// each tick's snapshot by in-place reconciliation, and drives the End task flow (spec §8).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IProcessTerminator _terminator;
    private readonly IElevationService _elevation;
    private readonly IEndTaskInteraction _interaction;

    public MainViewModel(
        IProcessTerminator terminator,
        IElevationService elevation,
        IEndTaskInteraction interaction)
    {
        _terminator = terminator;
        _elevation = elevation;
        _interaction = interaction;
    }

    public GraphStripViewModel GraphStrip { get; } = new();

    public ObservableCollection<ProcessRowViewModel> Apps { get; } = new();

    public ObservableCollection<ProcessRowViewModel> Background { get; } = new();

    public ObservableCollection<ServiceRowViewModel> Services { get; } = new();

    /// <summary>The process collection behind the currently selected view (Apps or Background).</summary>
    public ObservableCollection<ProcessRowViewModel> CurrentProcesses =>
        SelectedView == ViewKind.Background ? Background : Apps;

    public bool IsProcessView => SelectedView != ViewKind.Services;

    public bool IsServicesView => SelectedView == ViewKind.Services;

    public string HeaderText => SelectedView switch
    {
        ViewKind.Apps => "Apps",
        ViewKind.Background => "Background processes",
        ViewKind.Services => "Services",
        _ => string.Empty,
    };

    [ObservableProperty]
    private int _appsCount;

    [ObservableProperty]
    private int _backgroundCount;

    [ObservableProperty]
    private int _servicesCount;

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
            static s => new ProcessRowViewModel(s), static (r, s) => r.Update(s));

        CollectionSync.Apply(Background, background,
            static s => s.ProcessId, static r => r.ProcessId,
            static s => new ProcessRowViewModel(s), static (r, s) => r.Update(s));

        CollectionSync.Apply(Services, snapshot.Services,
            static s => s.ServiceName, static r => r.ServiceName,
            static s => new ServiceRowViewModel(s), static (r, s) => r.Update(s));

        AppsCount = Apps.Count;
        BackgroundCount = Background.Count;
        ServicesCount = Services.Count;

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

        // Unconditional confirm first (spec §8).
        if (!await _interaction.ConfirmEndTaskAsync(target.Name))
        {
            return;
        }

        TerminationOutcome outcome = _terminator.Terminate(target.ProcessId);
        switch (outcome)
        {
            case TerminationOutcome.Success:
            case TerminationOutcome.NotFound:
                // The row disappears on the next tick.
                SelectedProcess = null;
                break;

            case TerminationOutcome.AccessDenied:
                // Surface the elevate affordance — never pre-disabled (spec §8).
                if (await _interaction.ShowAccessDeniedAsync(target.Name))
                {
                    _elevation.RestartElevated();
                }

                break;

            default:
                await _interaction.ShowFailedAsync(target.Name);
                break;
        }
    }
}
