using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using Automation.Core.Enums;

namespace Automation.Core.Drivers;

public sealed class EdgeDriverProvider : ChromiumDriverProviderBase<EdgeOptions>
{
    public EdgeDriverProvider(ILogger<EdgeDriverProvider> logger) : base(logger) { }

    public override BrowserType Browser => BrowserType.Edge;

    protected override string PrivateBrowsingArgument => "--inprivate";

    protected override IWebDriver CreateDriver(EdgeOptions options)
    {
        var service = EdgeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        return new EdgeDriver(service, options);
    }
}
