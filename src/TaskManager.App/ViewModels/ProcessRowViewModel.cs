using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using TaskManager.Core.Models;
using TaskManager.Core.Presentation;
using TaskManager.Core.Text;

namespace TaskManager.App.ViewModels;

/// <summary>
/// One row in an Apps or Background process list (spec §6). Updated in place each tick so
/// selection and scroll position survive refreshes; when a process is inaccessible its
/// CPU/Memory cells go blank (spec §4). The row's icon (spec §6 "icon + label") is resolved
/// once, asynchronously, from the process image path via <see cref="IProcessIconResolver"/>.
/// </summary>
public sealed partial class ProcessRowViewModel : ObservableObject
{
    private readonly IProcessIconResolver _iconResolver;
    private bool _iconRequested;

    public ProcessRowViewModel(ProcessSample sample, IProcessIconResolver iconResolver)
    {
        _iconResolver = iconResolver;
        ProcessId = sample.ProcessId;
        Name = sample.Name;
        Update(sample);
    }

    public int ProcessId { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _cpuText = string.Empty;

    [ObservableProperty]
    private string _memoryText = string.Empty;

    [ObservableProperty]
    private UsageHeat _cpuHeat = UsageHeat.Low;

    [ObservableProperty]
    private UsageHeat _memoryHeat = UsageHeat.Low;

    /// <summary>The process icon, or <see langword="null"/> until resolved / when unavailable
    /// (the row shows a generic placeholder in that case).</summary>
    [ObservableProperty]
    private ImageSource? _iconSource;

    /// <summary>Applies a fresh sample. The PID never changes for a given row.</summary>
    public void Update(ProcessSample sample)
    {
        Name = sample.Name;
        CpuText = Humanize.PercentOrBlank(sample.CpuPercent);
        MemoryText = Humanize.BytesOrBlank(sample.PrivateWorkingSetBytes);
        CpuHeat = Heat.ForCpu(sample.CpuPercent);
        MemoryHeat = Heat.ForMemory(sample.PrivateWorkingSetBytes);
        ResolveIcon(sample.ImagePath);
    }

    /// <summary>
    /// Kicks off a one-time icon resolution once a path is known. A row that starts without a
    /// path (process not yet openable) will try again on the first tick that supplies one.
    /// </summary>
    private void ResolveIcon(string? imagePath)
    {
        if (_iconRequested || string.IsNullOrEmpty(imagePath))
        {
            return;
        }

        _iconRequested = true;
        _ = LoadIconAsync(imagePath);
    }

    private async Task LoadIconAsync(string imagePath)
    {
        IconSource = await _iconResolver.ResolveAsync(imagePath);
    }
}
