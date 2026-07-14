using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Enums;

namespace O365.Automation.Core.Drivers;

/// <summary>
/// Shared launch logic for the Chromium family (Chrome, Edge). Chrome and Edge differ only in their
/// options type, their private-browsing switch, and the driver they instantiate — so those are the
/// only three things subclasses supply. Everything else lives here once (DRY).
/// </summary>
/// <typeparam name="TOptions">The browser-specific options type, e.g. ChromeOptions.</typeparam>
public abstract class ChromiumDriverProviderBase<TOptions> : IBrowserDriverProvider
    where TOptions : ChromiumOptions, new()
{
    private readonly ILogger _logger;

    protected ChromiumDriverProviderBase(ILogger logger) => _logger = logger;

    public abstract BrowserType Browser { get; }

    /// <summary>The command line switch that starts a private session ("--incognito", "--inprivate").</summary>
    protected abstract string PrivateBrowsingArgument { get; }

    /// <summary>Instantiates the concrete driver once options have been assembled.</summary>
    protected abstract IWebDriver CreateDriver(TOptions options);

    public IWebDriver Create(BrowserSettings settings)
    {
        var options = new TOptions();

        if (settings.Mode == BrowsingMode.InPrivate)
        {
            options.AddArgument(PrivateBrowsingArgument);
        }
        else if (!string.IsNullOrWhiteSpace(settings.UserDataDirectory))
        {
            // Only meaningful for a normal session — an InPrivate window deliberately discards profile state.
            var profilePath = Path.GetFullPath(settings.UserDataDirectory);
            Directory.CreateDirectory(profilePath);
            options.AddArgument($"--user-data-dir={profilePath}");
        }

        if (settings.Headless)
        {
            options.AddArgument("--headless=new");
            // Headless Chromium defaults to a small viewport; M365 collapses its sidebar below ~1024px,
            // which would make navigation assertions fail for the wrong reason.
            options.AddArgument("--window-size=1920,1080");
        }
        else if (settings is { StartMaximized: true, WindowWidth: null, WindowHeight: null })
        {
            options.AddArgument("--start-maximized");
        }

        if (settings is { WindowWidth: > 0, WindowHeight: > 0 })
        {
            options.AddArgument($"--window-size={settings.WindowWidth},{settings.WindowHeight}");
        }

        options.AddArgument("--disable-notifications");
        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");

        if (!string.IsNullOrWhiteSpace(settings.DownloadDirectory))
        {
            var downloadPath = Path.GetFullPath(settings.DownloadDirectory);
            Directory.CreateDirectory(downloadPath);
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
        }

        foreach (var argument in settings.AdditionalArguments)
        {
            options.AddArgument(argument);
        }

        _logger.LogInformation(
            "Launching {Browser} ({Mode}{Headless}).",
            Browser,
            settings.Mode,
            settings.Headless ? ", headless" : string.Empty);

        return CreateDriver(options);
    }
}
