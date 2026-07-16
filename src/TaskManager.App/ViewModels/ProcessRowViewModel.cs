using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Core.Models;
using TaskManager.Core.Text;

namespace TaskManager.App.ViewModels;

/// <summary>
/// One row in an Apps or Background process list (spec §6). Updated in place each tick so
/// selection and scroll position survive refreshes; when a process is inaccessible its
/// CPU/Memory cells go blank (spec §4).
/// </summary>
public sealed partial class ProcessRowViewModel : ObservableObject
{
    public ProcessRowViewModel(ProcessSample sample)
    {
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

    /// <summary>Applies a fresh sample. The PID never changes for a given row.</summary>
    public void Update(ProcessSample sample)
    {
        Name = sample.Name;
        CpuText = Humanize.PercentOrBlank(sample.CpuPercent);
        MemoryText = Humanize.BytesOrBlank(sample.PrivateWorkingSetBytes);
        CpuHeat = Heat.ForCpu(sample.CpuPercent);
        MemoryHeat = Heat.ForMemory(sample.PrivateWorkingSetBytes);
    }
}
