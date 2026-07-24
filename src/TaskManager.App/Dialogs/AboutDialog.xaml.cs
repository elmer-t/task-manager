using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using TaskManager.Core.Product;

namespace TaskManager.App.Dialogs;

/// <summary>
/// The About overlay (issue #18). It carries no view-model state and no business logic, so
/// it stays in the view rather than behind an interaction interface (PR #20) — but its
/// layout is markup, so it lives under XAML Hot Reload with the rest of the shell.
/// The caller sets <c>XamlRoot</c> and calls <c>ShowAsync()</c>.
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog()
    {
        // Read before InitializeComponent so x:Bind sees a populated Info. The version and
        // copyright come from the running assembly's metadata (the csproj <Version> /
        // <Copyright>), so neither can drift from the build.
        Assembly assembly = typeof(AboutDialog).Assembly;
        Version version = assembly.GetName().Version ?? new Version(0, 0, 0);
        string copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        Info = new AboutInfo(version, copyright);

        InitializeComponent();
    }

    /// <summary>Everything the dialog shows, in one object for the markup to bind.</summary>
    public AboutInfo Info { get; }
}
