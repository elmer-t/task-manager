using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.Dialogs;
using TaskManager.App.Monitoring;
using TaskManager.App.ViewModels;
using TaskManager.Core.Presentation;

namespace TaskManager.App;

/// <summary>
/// The main window: it owns its Fluent chrome (Mica backdrop + system dark/light, spec §3),
/// routes the rail and row gestures to the <see cref="MainViewModel"/>, and opens the About
/// overlay. The object graph behind it — Win32 sources, the End task flow, the 1 Hz
/// <see cref="MonitorEngine"/> — is built by <see cref="Composition"/>.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MonitorEngine _engine;

    public MainWindow()
    {
        // Construct the view model before InitializeComponent so x:Bind and the initial
        // NavigationView selection (which fires SelectionChanged during load) see it.
        ViewModel = Composition.CreateViewModel(new EndTaskDialogs(this));

        InitializeComponent();

        SystemBackdrop = new MicaBackdrop();
        Title = "Task Manager";

        _engine = Composition.CreateEngine(DispatcherQueue, ViewModel.Apply);
        _engine.Start();

        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    // x:Bind function binding: converters resolved via StaticResource don't work in a
    // Window-rooted x:Bind (the generated lookup needs a FrameworkElement root).
    internal Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await _engine.DisposeAsync();
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag } &&
            ViewDescriptor.TryFromTag(tag, out ViewKind kind))
        {
            ViewModel.SelectedView = kind;
        }
    }

    private async void OnNavItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // The footer About item is SelectsOnInvoked="False", so it arrives here without
        // touching the selected view. The primary rail items still route through
        // OnNavSelectionChanged, so they need no handling on this path.
        if (args.InvokedItemContainer is NavigationViewItem { Tag: "About" })
        {
            await ShowAboutAsync();
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

    /// <summary>
    /// Opens the About overlay (issue #18). The layout is <see cref="AboutDialog"/>'s markup;
    /// all this does is give it a XamlRoot and show it.
    /// </summary>
    private async Task ShowAboutAsync()
    {
        var dialog = new AboutDialog { XamlRoot = Content.XamlRoot };
        await dialog.ShowAsync();
    }
}
