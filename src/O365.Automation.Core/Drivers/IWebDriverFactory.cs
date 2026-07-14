using OpenQA.Selenium;
using O365.Automation.Core.Configuration;

namespace O365.Automation.Core.Drivers;

public interface IWebDriverFactory
{
    /// <summary>
    /// Launches a browser using <paramref name="settings"/>, applying the framework's timeout policy.
    /// </summary>
    IWebDriver Create(BrowserSettings settings);
}
