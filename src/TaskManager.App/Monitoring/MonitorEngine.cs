using Microsoft.UI.Dispatching;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Monitoring;

namespace TaskManager.App.Monitoring;

/// <summary>
/// The single 1 Hz polling loop that drives all data (spec §5). Sampling runs on a
/// background thread so the toolhelp snapshot + per-process handle queries never block the
/// UI; each finished <see cref="MonitorSnapshot"/> is marshaled back to the UI thread via
/// the <see cref="DispatcherQueue"/> to update the view models. The three sources are
/// stateful (they hold previous-tick counters), so they are only ever touched from this
/// one loop thread.
/// </summary>
internal sealed class MonitorEngine : IAsyncDisposable
{
    private readonly ISystemMetricsSource _systemMetrics;
    private readonly IProcessSource _processes;
    private readonly IServiceSource _services;
    private readonly DispatcherQueue _dispatcher;
    private readonly Action<MonitorSnapshot> _onTick;

    private CancellationTokenSource? _cancellation;
    private Task? _loop;

    public MonitorEngine(
        ISystemMetricsSource systemMetrics,
        IProcessSource processes,
        IServiceSource services,
        DispatcherQueue dispatcher,
        Action<MonitorSnapshot> onTick)
    {
        _systemMetrics = systemMetrics;
        _processes = processes;
        _services = services;
        _dispatcher = dispatcher;
        _onTick = onTick;
    }

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cancellation.Token));
    }

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(MonitorConstants.TickInterval);

        try
        {
            // Sample immediately, then once per second. (CPU deltas need two samples, so
            // the first tick reads 0% — the second tick onward is live.)
            do
            {
                MonitorSnapshot? snapshot = TrySample();
                if (snapshot is not null)
                {
                    _dispatcher.TryEnqueue(() => _onTick(snapshot));
                }
            }
            while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private MonitorSnapshot? TrySample()
    {
        try
        {
            // System first so the process source's CPU denominator lines up with the graph.
            var system = _systemMetrics.Sample();
            var processes = _processes.Sample();
            var services = _services.Sample();
            return new MonitorSnapshot(system, processes, services);
        }
        catch
        {
            // A transient Win32 hiccup on one tick should never tear down the loop; the
            // next tick tries again.
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation?.Cancel();

        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cancellation?.Dispose();
    }
}
