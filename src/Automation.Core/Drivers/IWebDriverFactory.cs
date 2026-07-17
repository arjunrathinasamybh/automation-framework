using OpenQA.Selenium;
using Automation.Core.Configuration;

namespace Automation.Core.Drivers;

public interface IWebDriverFactory
{
    /// <summary>
    /// Launches a browser using <paramref name="settings"/>, applying the framework's timeout policy.
    /// </summary>
    IWebDriver Create(BrowserSettings settings);
}
