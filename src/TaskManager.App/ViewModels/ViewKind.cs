namespace TaskManager.App.ViewModels;

/// <summary>The three views selected by the left navigation rail (spec §6 / CONTEXT.md).</summary>
public enum ViewKind
{
    Apps,
    Background,
    Services,
}

/// <summary>
/// The per-view presentation facts shared by every place that would otherwise switch on
/// <see cref="ViewKind"/> — the toolbar header, and whether the view is a process list
/// (End task applies) or the view-only Services list. Consolidating them here replaces the
/// parallel conditionals that were scattered across <see cref="MainViewModel"/>.
/// </summary>
/// <param name="Kind">The view this describes.</param>
/// <param name="Tag">The <c>NavigationViewItem.Tag</c> that selects this view in the rail.</param>
/// <param name="Header">The title shown in the toolbar (spec §6).</param>
/// <param name="IsProcessView">
/// True for the Apps / Background process lists; false for the view-only Services list.
/// </param>
public sealed record ViewDescriptor(ViewKind Kind, string Tag, string Header, bool IsProcessView)
{
    private static readonly IReadOnlyDictionary<ViewKind, ViewDescriptor> ByKind =
        new Dictionary<ViewKind, ViewDescriptor>
        {
            [ViewKind.Apps] = new(ViewKind.Apps, "Apps", "Apps", IsProcessView: true),
            [ViewKind.Background] = new(ViewKind.Background, "Background", "Background processes", IsProcessView: true),
            [ViewKind.Services] = new(ViewKind.Services, "Services", "Services", IsProcessView: false),
        };

    /// <summary>The descriptor for a view. Every <see cref="ViewKind"/> has exactly one.</summary>
    public static ViewDescriptor For(ViewKind kind) => ByKind[kind];

    /// <summary>
    /// Resolves a <c>NavigationViewItem.Tag</c> back to its view — the one place the rail's
    /// tags are mapped, so the nav-selection handler shares this table too. Returns
    /// <see langword="false"/> for an unknown tag.
    /// </summary>
    public static bool TryFromTag(string tag, out ViewKind kind)
    {
        foreach (ViewDescriptor descriptor in ByKind.Values)
        {
            if (string.Equals(descriptor.Tag, tag, StringComparison.Ordinal))
            {
                kind = descriptor.Kind;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
