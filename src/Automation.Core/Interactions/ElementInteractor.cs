using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace Automation.Core.Interactions;

public sealed class ElementInteractor : IElementInteractor
{
    private readonly IWebDriver _driver;
    private readonly ILogger<ElementInteractor> _logger;

    public ElementInteractor(IWebDriver driver, ILogger<ElementInteractor> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public void Click(IWebElement element)
    {
        ScrollIntoView(element);

        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // M365 layers teaching callouts, "what's new" flyouts and loading shims over its own chrome.
            // An intercepted click is routine here, so dispatch the event directly rather than fail.
            _logger.LogDebug("Click intercepted; dispatching via JavaScript instead.");
            Script.ExecuteScript("arguments[0].click();", element);
        }
    }

    public void Type(IWebElement element, string text)
    {
        ScrollIntoView(element);
        element.Clear();

        // The AAD inputs are React-controlled: Clear() can leave the component's internal state populated,
        // which then re-renders the old value. Verify and blank it out through the DOM if that happens.
        if (!string.IsNullOrEmpty(element.GetAttribute("value")))
        {
            Script.ExecuteScript(
                "arguments[0].value = ''; arguments[0].dispatchEvent(new Event('input', { bubbles: true }));",
                element);
        }

        element.SendKeys(text);
    }

    private void ScrollIntoView(IWebElement element) =>
        Script.ExecuteScript("arguments[0].scrollIntoView({ block: 'center' });", element);

    private IJavaScriptExecutor Script => (IJavaScriptExecutor)_driver;
}
