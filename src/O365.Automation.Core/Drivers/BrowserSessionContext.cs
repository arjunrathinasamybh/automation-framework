using O365.Automation.Core.Configuration;
using O365.Automation.Core.Enums;

namespace O365.Automation.Core.Drivers;

/// <summary>
/// Per-scope (i.e. per-test) browser choice. Seeded from configuration, then optionally overridden by the
/// test itself before the driver is first resolved — this is what lets one test body run against
/// Chrome/InPrivate and another against Edge/Normal without touching config or the container.
/// </summary>
public sealed class BrowserSessionContext
{
    public BrowserSessionContext(BrowserSettings defaults) => Settings = Clone(defaults);

    /// <summary>The settings the driver will be launched with. Mutate before first driver access.</summary>
    public BrowserSettings Settings { get; }

    /// <summary>
    /// Whether a browser was actually launched in this scope.
    /// <para>
    /// Teardown needs this. A failure hook that captures a screenshot must first ask whether there is
    /// anything to capture — resolving the driver to photograph a test that never opened a browser would
    /// launch one during teardown, purely to take a picture of a blank page.
    /// </para>
    /// </summary>
    public bool DriverLaunched { get; private set; }

    /// <summary>Called by the container when the driver is constructed.</summary>
    public void MarkDriverLaunched() => DriverLaunched = true;

    /// <summary>Overrides the browser and/or privacy mode for this scope.</summary>
    public BrowserSessionContext Use(BrowserType? browser = null, BrowsingMode? mode = null)
    {
        if (browser is not null) Settings.Type = browser.Value;
        if (mode is not null) Settings.Mode = mode.Value;
        return this;
    }

    // Defensive copy: BrowserSettings comes from IOptions and is a shared singleton instance.
    // Mutating it directly would leak one test's browser choice into every subsequent test.
    private static BrowserSettings Clone(BrowserSettings source) => new()
    {
        Type = source.Type,
        Mode = source.Mode,
        Headless = source.Headless,
        StartMaximized = source.StartMaximized,
        WindowWidth = source.WindowWidth,
        WindowHeight = source.WindowHeight,
        UserDataDirectory = source.UserDataDirectory,
        DownloadDirectory = source.DownloadDirectory,
        AdditionalArguments = [.. source.AdditionalArguments]
    };
}
