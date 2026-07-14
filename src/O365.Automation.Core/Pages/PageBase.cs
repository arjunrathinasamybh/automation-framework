using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Core.Pages;

/// <summary>
/// Base for every page object and page component. Owns the driver plumbing so that concrete pages contain
/// only locators and domain-meaningful actions — never waits, retries or JavaScript workarounds.
/// </summary>
public abstract class PageBase
{
    protected PageBase(IWebDriver driver, IWaitService wait, IElementInteractor interactor, ILogger logger)
    {
        Driver = driver;
        Wait = wait;
        Interactor = interactor;
        Logger = logger;
    }

    protected IWebDriver Driver { get; }
    protected IWaitService Wait { get; }
    protected IElementInteractor Interactor { get; }
    protected ILogger Logger { get; }

    /// <summary>An element whose presence proves this page is the one currently rendered.</summary>
    protected abstract By PageIdentifier { get; }

    /// <summary>Cheap, non-blocking check of whether this page is currently rendered.</summary>
    public virtual bool IsDisplayed => Wait.IsDisplayed(PageIdentifier);

    /// <summary>Blocks until this page is rendered; throws <see cref="WebDriverTimeoutException"/> if it never is.</summary>
    public virtual void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        Wait.UntilVisible(PageIdentifier, timeout);
        Logger.LogDebug("{Page} is loaded.", GetType().Name);
    }

    protected void Click(By locator, TimeSpan? timeout = null) =>
        Interactor.Click(Wait.UntilClickable(locator, timeout));

    protected void Type(By locator, string text, TimeSpan? timeout = null) =>
        Interactor.Type(Wait.UntilVisible(locator, timeout), text);

    protected string TextOf(By locator, TimeSpan? timeout = null) =>
        Wait.UntilVisible(locator, timeout).Text.Trim();
}
