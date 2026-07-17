using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Automation.Core.Enums;

namespace Automation.Core.Drivers;

public sealed class ChromeDriverProvider : ChromiumDriverProviderBase<ChromeOptions>
{
    public ChromeDriverProvider(ILogger<ChromeDriverProvider> logger) : base(logger) { }

    public override BrowserType Browser => BrowserType.Chrome;

    protected override string PrivateBrowsingArgument => "--incognito";

    protected override IWebDriver CreateDriver(ChromeOptions options)
    {
        // Selenium Manager (4.6+) downloads and pins the matching chromedriver automatically.
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        return new ChromeDriver(service, options);
    }
}
