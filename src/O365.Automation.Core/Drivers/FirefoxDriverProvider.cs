using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Enums;

namespace O365.Automation.Core.Drivers;

/// <summary>
/// Firefox is not Chromium-based, so it does not share the ChromiumOptions surface: it has no
/// "--start-maximized" switch, and downloads/profile are driven by about:config preferences rather
/// than user-profile preferences. Hence a standalone provider rather than a subclass.
/// </summary>
public sealed class FirefoxDriverProvider : IBrowserDriverProvider
{
    private readonly ILogger<FirefoxDriverProvider> _logger;

    public FirefoxDriverProvider(ILogger<FirefoxDriverProvider> logger) => _logger = logger;

    public BrowserType Browser => BrowserType.Firefox;

    public IWebDriver Create(BrowserSettings settings)
    {
        var options = new FirefoxOptions();

        if (settings.Mode == BrowsingMode.InPrivate)
        {
            options.AddArgument("-private");
            options.SetPreference("browser.privatebrowsing.autostart", true);
        }
        else if (!string.IsNullOrWhiteSpace(settings.UserDataDirectory))
        {
            var profilePath = Path.GetFullPath(settings.UserDataDirectory);
            Directory.CreateDirectory(profilePath);
            options.AddArguments("-profile", profilePath);
        }

        if (settings.Headless)
        {
            options.AddArgument("--headless");
        }

        if (settings is { WindowWidth: > 0, WindowHeight: > 0 })
        {
            options.AddArguments("--width", settings.WindowWidth.Value.ToString());
            options.AddArguments("--height", settings.WindowHeight.Value.ToString());
        }

        options.SetPreference("dom.webnotifications.enabled", false);

        if (!string.IsNullOrWhiteSpace(settings.DownloadDirectory))
        {
            var downloadPath = Path.GetFullPath(settings.DownloadDirectory);
            Directory.CreateDirectory(downloadPath);
            options.SetPreference("browser.download.folderList", 2);
            options.SetPreference("browser.download.dir", downloadPath);
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

        var service = FirefoxDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        var driver = new FirefoxDriver(service, options);

        // Firefox has no maximise launch switch, so do it once the window exists.
        if (settings is { StartMaximized: true, Headless: false, WindowWidth: null, WindowHeight: null })
        {
            driver.Manage().Window.Maximize();
        }

        return driver;
    }
}
