using Microsoft.UI.Dispatching;
using TaskManager.App.Interop;
using TaskManager.App.Monitoring;
using TaskManager.App.ViewModels;
using TaskManager.Core.Abstractions;
using TaskManager.Core.Models;
using TaskManager.Core.Monitoring;

namespace TaskManager.App;

/// <summary>
/// The app's object graph: every Win32 adapter, the End task flow, the view model, and the
/// 1 Hz engine are constructed here, so the window can be a view.
///
/// Two factories rather than one because the engine needs the window's
/// <see cref="DispatcherQueue"/>, which does not exist until the window does. Splitting them
/// keeps that ordering constraint visible at the call site instead of hiding it behind a
/// lifetime. Nothing here holds state — there is one window, constructed once, and nothing
/// resolves anything later.
/// </summary>
internal static class Composition
{
    /// <summary>
    /// Builds the window's view model, with the End task flow (spec §8) behind it.
    /// </summary>
    /// <param name="interaction">
    /// The dialog adapter. It is constructed by the caller because it holds the window, and
    /// it must be able to reach that window's <c>Content.XamlRoot</c> at call time.
    /// </param>
    public static MainViewModel CreateViewModel(IEndTaskInteraction interaction)
    {
        var endTask = new EndTaskFlow(
            new ProcessTerminator(),
            new ElevationService(),
            interaction);

        return new MainViewModel(endTask, new ProcessIconResolver());
    }

    /// <summary>
    /// Builds the single 1 Hz polling loop (spec §5) over the three Win32 sources. Call once
    /// the window exists: the loop marshals each tick back through its dispatcher.
    /// </summary>
    public static MonitorEngine CreateEngine(DispatcherQueue dispatcher, Action<MonitorSnapshot> onTick) =>
        new(new SystemMetricsSource(),
            new ProcessSource(),
            new ServiceSource(),
            dispatcher,
            onTick);
}
