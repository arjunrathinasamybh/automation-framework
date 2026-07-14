using OpenQA.Selenium;

namespace O365.Automation.Core.Interactions;

/// <summary>
/// Low-level element interactions with the resilience quirks that real browsers demand
/// (intercepted clicks, inputs that ignore Clear()). Shared by page objects and login step handlers so
/// the workarounds exist exactly once.
/// </summary>
public interface IElementInteractor
{
    /// <summary>Clicks, scrolling into view first and falling back to a JavaScript click if intercepted.</summary>
    void Click(IWebElement element);

    /// <summary>Clears the field and types <paramref name="text"/>.</summary>
    void Type(IWebElement element, string text);
}
