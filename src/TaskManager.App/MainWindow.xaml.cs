using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TaskManager.App.Interop;
using TaskManager.App.Monitoring;
using TaskManager.App.ViewModels;
using TaskManager.Core.Product;

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
        ViewModel = new MainViewModel(
            new ProcessTerminator(), new ElevationService(), this, new ProcessIconResolver());

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

    // ---- About dialog (issue #18) ----

    /// <summary>
    /// Shows the About dialog. Unlike End task, this carries no view-model state or business
    /// logic — it's a purely presentational overlay — so it stays here in the view rather than
    /// behind an interaction interface, but reuses the same Fluent <see cref="ContentDialog"/>
    /// + <c>XamlRoot</c> convention (system dark/light, Mica-consistent). The version and
    /// copyright are read from the running assembly's metadata so they always match the build
    /// (the csproj <c>&lt;Version&gt;</c> / <c>&lt;Copyright&gt;</c>); the rest of the content
    /// is the single source of truth in <see cref="AboutInfo"/>.
    /// </summary>
    private async Task ShowAboutAsync()
    {
        Assembly assembly = typeof(MainWindow).Assembly;
        Version version = assembly.GetName().Version ?? new Version(0, 0, 0);
        string copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        var about = new AboutInfo(version, copyright);

        var content = new StackPanel { Spacing = 4, Width = 320 };
        content.Children.Add(new TextBlock
        {
            Text = AboutInfo.Name,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
        });
        content.Children.Add(new TextBlock { Text = about.VersionText });
        content.Children.Add(new TextBlock
        {
            Text = AboutInfo.Tagline,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        });
        content.Children.Add(new TextBlock
        {
            Text = about.CopyrightLine,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Margin = new Thickness(0, 8, 0, 0),
        });
        content.Children.Add(new HyperlinkButton
        {
            Content = AboutInfo.RepositoryUrl,
            NavigateUri = new Uri(AboutInfo.RepositoryUrl),
            Padding = new Thickness(0),
        });

        var dialog = new ContentDialog
        {
            Title = "About",
            Content = content,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }
}
