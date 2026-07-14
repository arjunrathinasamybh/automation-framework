using O365.Automation.Core.Enums;

namespace O365.Automation.Core.Configuration;

/// <summary>
/// How the browser session should be launched. Bound from the "Browser" configuration section
/// and overridable per-run via environment variables (e.g. <c>Browser__Type=Edge</c>).
/// </summary>
public sealed class BrowserSettings
{
    public const string SectionName = "Browser";

    /// <summary>Which browser to drive.</summary>
    public BrowserType Type { get; set; } = BrowserType.Edge;

    /// <summary>Normal or InPrivate/incognito browsing.</summary>
    public BrowsingMode Mode { get; set; } = BrowsingMode.InPrivate;

    /// <summary>Run without a visible window. Not compatible with <see cref="UserDataDirectory"/> on some builds.</summary>
    public bool Headless { get; set; }

    /// <summary>Maximise on launch. Ignored when <see cref="WindowWidth"/>/<see cref="WindowHeight"/> are set.</summary>
    public bool StartMaximized { get; set; } = true;

    public int? WindowWidth { get; set; }

    public int? WindowHeight { get; set; }

    /// <summary>
    /// Persistent profile directory, used only in <see cref="BrowsingMode.Normal"/>.
    /// Lets a signed-in session survive across runs. Ignored for InPrivate, which is stateless by definition.
    /// </summary>
    public string? UserDataDirectory { get; set; }

    /// <summary>Directory browser downloads are routed to. Created on demand.</summary>
    public string? DownloadDirectory { get; set; }

    /// <summary>Escape hatch for browser-specific command line switches not modelled above.</summary>
    public IList<string> AdditionalArguments { get; set; } = [];
}
