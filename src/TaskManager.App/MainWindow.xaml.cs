using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.Interop;
using TaskManager.App.Monitoring;
using TaskManager.App.ViewModels;

namespace TaskManager.App;

/// <summary>
/// The main window and the app's composition root: it wires the Win32 sources into the
/// 1 Hz <see cref="MonitorEngine"/>, owns the <see cref="MainViewModel"/>, and implements
/// <see cref="IEndTaskInteraction"/> — the Fluent confirm/error dialogs (spec §8) that need
/// a XamlRoot. Mica backdrop + system dark/light come from WinUI (spec §3).
/// </summary>
public sealed partial class MainWindow : Window, IEndTaskInteraction
{
    private readonly MonitorEngine _engine;

    public MainWindow()
    {
        // Construct the view model before InitializeComponent so x:Bind and the initial
        // NavigationView selection (which fires SelectionChanged during load) see it.
        ViewModel = new MainViewModel(new ProcessTerminator(), new ElevationService(), this);

        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        Title = "Task Manager";

        _engine = new MonitorEngine(
            new SystemMetricsSource(),
            new ProcessSource(),
            new ServiceSource(),
            DispatcherQueue,
            ViewModel.Apply);
        _engine.Start();

        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await _engine.DisposeAsync();
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag } &&
            Enum.TryParse(tag, out ViewKind kind))
        {
            ViewModel.SelectedView = kind;
        }
    }

    private void OnRowRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Right-click doesn't select by default; make the context menu act on the row it opened over.
        if (sender is FrameworkElement { DataContext: ProcessRowViewModel row })
        {
            ViewModel.SelectedProcess = row;
        }
    }

    private void OnEndTaskMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ProcessRowViewModel row })
        {
            ViewModel.SelectedProcess = row;
        }

        if (ViewModel.EndTaskCommand.CanExecute(null))
        {
            ViewModel.EndTaskCommand.Execute(null);
        }
    }

    // ---- IEndTaskInteraction: the kill-process dialogs (spec §8) ----

    public async Task<bool> ConfirmEndTaskAsync(string processName)
    {
        var dialog = new ContentDialog
        {
            Title = "End task",
            Content = $"End “{processName}”? Unsaved data may be lost.",
            PrimaryButtonText = "End",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task<bool> ShowAccessDeniedAsync(string processName)
    {
        var dialog = new ContentDialog
        {
            Title = "Administrator required",
            Content =
                $"Ending “{processName}” needs administrator rights — it belongs to " +
                "another user or to Windows. Restart Task Manager as administrator to end it.",
            PrimaryButtonText = "Restart as administrator",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task ShowFailedAsync(string processName)
    {
        var dialog = new ContentDialog
        {
            Title = "Couldn't end task",
            Content = $"Task Manager couldn't end “{processName}”. It may have already exited.",
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }
}
