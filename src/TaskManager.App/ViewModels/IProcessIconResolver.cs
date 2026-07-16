using Microsoft.UI.Xaml.Media;

namespace TaskManager.App.ViewModels;

/// <summary>
/// Resolves a process executable path to its icon as a WinUI <see cref="ImageSource"/>
/// (spec §6 "Name (icon + label)"). Implementations cache by path so the 1 Hz tick stays
/// cheap (spec §5) and return <see langword="null"/> when the icon can't be read, so the row
/// falls back to a generic placeholder rather than erroring (spec §4).
/// </summary>
public interface IProcessIconResolver
{
    /// <summary>
    /// Returns the icon for <paramref name="imagePath"/>, or <see langword="null"/> if it
    /// can't be resolved. Must be called on the UI thread — the result is a UI-thread object.
    /// </summary>
    Task<ImageSource?> ResolveAsync(string imagePath);
}
