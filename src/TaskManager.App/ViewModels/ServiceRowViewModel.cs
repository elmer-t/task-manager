using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Core.Models;

namespace TaskManager.App.ViewModels;

/// <summary>
/// One row in the view-only Services list: display name, description, and a Running /
/// Stopped status pill (spec §6). No actions — the Services view mutates nothing (spec §2).
/// </summary>
public sealed partial class ServiceRowViewModel : ObservableObject
{
    public ServiceRowViewModel(ServiceSample sample)
    {
        ServiceName = sample.ServiceName;
        DisplayName = sample.DisplayName;
        Update(sample);
    }

    /// <summary>SCM key name; stable identity for reconciliation.</summary>
    public string ServiceName { get; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public void Update(ServiceSample sample)
    {
        DisplayName = sample.DisplayName;
        Description = sample.Description ?? string.Empty;
        IsRunning = sample.Status == ServiceStatus.Running;
        StatusText = IsRunning ? "Running" : "Stopped";
    }
}
