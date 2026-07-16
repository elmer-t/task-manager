namespace TaskManager.Core.Product;

/// <summary>
/// The app's identity as shown in the About dialog (issue #18): a fixed name, tagline,
/// license, and repository link, plus the running build's <see cref="VersionText"/> and its
/// <see cref="CopyrightLine"/>.
///
/// Kept in Core — alongside <see cref="Text.Humanize"/> — so the dialog's content is one
/// source of truth pinned by tests. The two facts that must track the build rather than be
/// baked-in literals — the version and the copyright holder — are supplied by the App from
/// assembly metadata (the csproj <c>&lt;Version&gt;</c> / <c>&lt;Copyright&gt;</c>); this type
/// formats the version and appends the fixed <see cref="License"/> to the copyright. The name,
/// tagline, license, and URL are constants the view reads directly.
/// </summary>
public sealed class AboutInfo
{
    /// <summary>The product name shown as the dialog's heading.</summary>
    public const string Name = "Task Manager";

    /// <summary>The one-line description under the version.</summary>
    public const string Tagline =
        "A personal, Fluent-styled Windows task manager — a research & learning project.";

    /// <summary>The license the app ships under, shown after the copyright and matching <c>LICENSE</c>.</summary>
    public const string License = "MIT License";

    /// <summary>The repository the About dialog's link opens in the default browser.</summary>
    public const string RepositoryUrl = "https://github.com/elmer-t/task-manager";

    /// <param name="version">The running assembly's version (read at runtime, never hardcoded).</param>
    /// <param name="copyright">
    /// The copyright notice from assembly metadata (e.g. "© 2026 REDHEADIT") — the conventional
    /// holder/year only, read from the csproj <c>&lt;Copyright&gt;</c> so it stays in step with
    /// the build and the repo's <c>LICENSE</c> holder. The <see cref="License"/> is appended here.
    /// </param>
    public AboutInfo(Version version, string copyright)
    {
        VersionText = FormatVersion(version);
        CopyrightLine = $"{copyright} · {License}";
    }

    /// <summary>The running build's version, formatted for display — e.g. "Version 1.0.0".</summary>
    public string VersionText { get; }

    /// <summary>
    /// The single copyright/license line for the dialog, e.g. "© 2026 REDHEADIT · MIT License" —
    /// the assembly's copyright notice followed by the app's <see cref="License"/>.
    /// </summary>
    public string CopyrightLine { get; }

    /// <summary>
    /// Formats an assembly <see cref="Version"/> as "Version major.minor.patch". The 4th
    /// (revision) field is dropped, and an unspecified build component (e.g.
    /// <c>new Version(1, 5)</c>, whose <see cref="Version.Build"/> is -1) renders as a 0 patch.
    /// </summary>
    public static string FormatVersion(Version version)
    {
        int patch = version.Build < 0 ? 0 : version.Build;
        return $"Version {version.Major}.{version.Minor}.{patch}";
    }
}
