using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskManager.Core.Abstractions;

namespace TaskManager.App.Dialogs;

/// <summary>
/// The three End task dialogs (spec §8) as one adapter, so the window is no longer its own
/// <see cref="IEndTaskInteraction"/>. Same Fluent <c>ContentDialog</c> mechanism as before
/// (#18) — system dark/light, Mica-consistent — only constructed here instead of in the view.
/// </summary>
internal sealed class EndTaskDialogs : IEndTaskInteraction
{
    private readonly Window _window;

    public EndTaskDialogs(Window window) => _window = window;

    // Resolved per call, never captured: the adapter is constructed before the window's
    // InitializeComponent, so Content — and therefore its XamlRoot — does not exist yet.
    private XamlRoot CurrentXamlRoot => _window.Content.XamlRoot;

    public async Task<bool> ConfirmEndTaskAsync(string processName)
    {
        var dialog = new ContentDialog
        {
            Title = "End task",
            Content = $"End “{processName}”? Unsaved data may be lost.",
            PrimaryButtonText = "End",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = CurrentXamlRoot,
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
            XamlRoot = CurrentXamlRoot,
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
            XamlRoot = CurrentXamlRoot,
        };

        await dialog.ShowAsync();
    }
}
