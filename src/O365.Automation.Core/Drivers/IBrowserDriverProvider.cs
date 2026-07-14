using OpenQA.Selenium;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Enums;

namespace O365.Automation.Core.Drivers;

/// <summary>
/// Knows how to launch exactly one browser. Implementations are the Open/Closed seam of the driver layer:
/// supporting a new browser means adding an implementation and registering it, never editing the factory.
/// </summary>
public interface IBrowserDriverProvider
{
    /// <summary>The browser this provider is responsible for.</summary>
    BrowserType Browser { get; }

    /// <summary>Launches a browser configured per <paramref name="settings"/>.</summary>
    IWebDriver Create(BrowserSettings settings);
}
