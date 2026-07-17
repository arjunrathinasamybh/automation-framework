using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using Automation.Core.Configuration;
using Automation.Core.Enums;

namespace Automation.Core.Drivers;

/// <summary>
/// Selects the provider registered for the requested browser and applies cross-cutting driver
/// configuration (timeouts). It has no knowledge of any specific browser — that is the providers' job.
/// </summary>
public sealed class WebDriverFactory : IWebDriverFactory
{
    private readonly IReadOnlyDictionary<BrowserType, IBrowserDriverProvider> _providers;
    private readonly TimeoutSettings _timeouts;

    public WebDriverFactory(IEnumerable<IBrowserDriverProvider> providers, IOptions<TimeoutSettings> timeouts)
    {
        _providers = providers.ToDictionary(provider => provider.Browser);
        _timeouts = timeouts.Value;
    }

    public IWebDriver Create(BrowserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_providers.TryGetValue(settings.Type, out var provider))
        {
            throw new NotSupportedException(
                $"No {nameof(IBrowserDriverProvider)} is registered for browser '{settings.Type}'. " +
                $"Registered: {string.Join(", ", _providers.Keys)}.");
        }

        var driver = provider.Create(settings);

        // Page load timeout only. Implicit waits are deliberately left at zero: mixing them with the
        // explicit waits in WaitService produces unpredictable, compounding timeouts.
        driver.Manage().Timeouts().PageLoad = _timeouts.PageLoad;

        return driver;
    }
}
