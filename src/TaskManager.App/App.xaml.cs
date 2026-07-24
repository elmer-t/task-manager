using Microsoft.UI.Xaml;

namespace TaskManager.App;

/// <summary>
/// Application entry point. The heavy lifting — composing the Win32 sources, the 1 Hz
/// engine, and the view model — belongs to <see cref="Composition"/>, which
/// <see cref="MainWindow"/> calls as it comes up; this class just launches the window.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
