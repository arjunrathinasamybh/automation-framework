using OpenQA.Selenium;

namespace O365.Automation.Core.Waits;

/// <summary>
/// The single place explicit waits are expressed. Page objects depend on this abstraction rather than
/// constructing WebDriverWait themselves, so timeout policy and ignored-exception policy stay consistent.
/// </summary>
public interface IWaitService
{
    /// <summary>Waits for the element to exist and be displayed. Throws <see cref="WebDriverTimeoutException"/> on timeout.</summary>
    IWebElement UntilVisible(By locator, TimeSpan? timeout = null);

    /// <summary>Waits for the element to be displayed and enabled.</summary>
    IWebElement UntilClickable(By locator, TimeSpan? timeout = null);

    /// <summary>Waits until no element matches the locator (or the match is hidden). Useful for spinners.</summary>
    void UntilGone(By locator, TimeSpan? timeout = null);

    /// <summary>Waits until the current URL contains <paramref name="fragment"/> (case-insensitive).</summary>
    void UntilUrlContains(string fragment, TimeSpan? timeout = null);

    /// <summary>Waits until document.readyState is "complete".</summary>
    void UntilDocumentReady(TimeSpan? timeout = null);

    /// <summary>Generic escape hatch for conditions not covered above.</summary>
    TResult Until<TResult>(Func<IWebDriver, TResult> condition, TimeSpan? timeout = null);

    /// <summary>
    /// Non-blocking existence check — returns immediately. This is the primitive the login step handlers
    /// use to ask "is my screen the one currently rendered?" without paying a timeout per probe.
    /// </summary>
    bool IsDisplayed(By locator);

    /// <summary>
    /// Returns the first locator in <paramref name="locators"/> that matches a visible element, or null.
    /// Backs the resilient multi-locator strategy used against churn-prone M365 markup.
    /// </summary>
    IWebElement? FirstDisplayedOrDefault(IEnumerable<By> locators, TimeSpan? timeout = null);
}
