using TaskManager.Core.Product;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>
/// The build-derived lines shown in the About dialog (issue #18): the "Version 1.0.0"
/// string (three components, no revision) and the "© 2026 REDHEADIT · MIT License" copyright
/// line. Both are fed from assembly metadata, so their formatting is pinned here rather than
/// in view code.
/// </summary>
public class AboutInfoTests
{
    [Fact]
    public void Version_text_shows_major_minor_patch()
    {
        var about = new AboutInfo(new Version(1, 0, 0, 0), "© 2026 REDHEADIT");
        Assert.Equal("Version 1.0.0", about.VersionText);
    }

    [Fact]
    public void Copyright_line_appends_the_license_to_the_assembly_copyright()
    {
        // The holder/year come from assembly metadata (read at runtime, never hardcoded);
        // the license label is appended, yielding the single line the dialog shows.
        var about = new AboutInfo(new Version(1, 0, 0, 0), "© 2026 REDHEADIT");
        Assert.Equal("© 2026 REDHEADIT · MIT License", about.CopyrightLine);
    }

    [Fact]
    public void Version_text_drops_the_revision_component()
    {
        // Assembly versions carry a 4th (revision) field; the dialog shows only three.
        Assert.Equal("Version 2.3.4", AboutInfo.FormatVersion(new Version(2, 3, 4, 99)));
    }

    [Fact]
    public void Version_text_shows_a_zero_patch_when_the_build_is_unspecified()
    {
        // new Version(1, 5) has Build == -1; it must render as a 0 patch, never "-1".
        Assert.Equal("Version 1.5.0", AboutInfo.FormatVersion(new Version(1, 5)));
    }
}
