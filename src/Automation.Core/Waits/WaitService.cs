using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Automation.Core.Configuration;

namespace Automation.Core.Waits;

public sealed class WaitService : IWaitService
{
    private readonly IWebDriver _driver;
    private readonly TimeoutSettings _timeouts;

    public WaitService(IWebDriver driver, IOptions<TimeoutSettings> timeouts)
    {
        _driver = driver;
        _timeouts = timeouts.Value;
    }

    public IWebElement UntilVisible(By locator, TimeSpan? timeout = null) =>
        Until(driver =>
        {
            var element = FindOrNull(driver, locator);
            return element is { Displayed: true } ? element : null;
        }, timeout)
        ?? throw new WebDriverTimeoutException($"Element '{locator}' was not visible within the timeout.");

    public IWebElement UntilClickable(By locator, TimeSpan? timeout = null) =>
        Until(driver =>
        {
            var element = FindOrNull(driver, locator);
            return element is { Displayed: true, Enabled: true } ? element : null;
        }, timeout)
        ?? throw new WebDriverTimeoutException($"Element '{locator}' was not clickable within the timeout.");

    public void UntilGone(By locator, TimeSpan? timeout = null) =>
        Until(driver =>
        {
            var element = FindOrNull(driver, locator);
            return element is null || !element.Displayed;
        }, timeout);

    public void UntilUrlContains(string fragment, TimeSpan? timeout = null) =>
        Until(driver => driver.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase), timeout);

    public void UntilDocumentReady(TimeSpan? timeout = null) =>
        Until(driver =>
            ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState")?.ToString() == "complete",
            timeout ?? _timeouts.PageLoad);

    public TResult Until<TResult>(Func<IWebDriver, TResult> condition, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(_driver, timeout ?? _timeouts.Element)
        {
            PollingInterval = _timeouts.Polling
        };

        // A page mid-render throws NoSuchElement and, on M365's SPA re-renders, StaleElementReference.
        // Both mean "not ready yet", not "failed" — so they are polled through rather than propagated.
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait.Until(condition);
    }

    public bool IsDisplayed(By locator)
    {
        try
        {
            return FindOrNull(_driver, locator) is { Displayed: true };
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    public IWebElement? FirstDisplayedOrDefault(IEnumerable<By> locators, TimeSpan? timeout = null)
    {
        var candidates = locators as IReadOnlyList<By> ?? [.. locators];

        try
        {
            return Until(driver => candidates
                .Select(locator => FindOrNull(driver, locator))
                .FirstOrDefault(element => element is { Displayed: true }),
                timeout);
        }
        catch (WebDriverTimeoutException)
        {
            // "None of the candidates matched" is a legitimate answer here, not a failure.
            return null;
        }
    }

    private static IWebElement? FindOrNull(ISearchContext context, By locator)
    {
        var matches = context.FindElements(locator);
        return matches.Count > 0 ? matches[0] : null;
    }
}
