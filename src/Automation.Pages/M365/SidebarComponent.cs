using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Automation.Core.Interactions;
using Automation.Core.Pages;
using Automation.Core.Waits;

namespace Automation.Pages.M365;

/// <summary>
/// The Microsoft 365 left navigation rail. Modelled as a component rather than a page because the same
/// rail is rendered across several M365 surfaces — composing it into each page object keeps the
/// navigation logic in one place.
/// </summary>
public sealed class SidebarComponent : PageBase
{
    public SidebarComponent(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<SidebarComponent> logger)
        : base(driver, wait, interactor, logger) { }

    protected override By PageIdentifier => SidebarLocators.Container[0];

    public override bool IsDisplayed => Wait.FirstDisplayedOrDefault(SidebarLocators.Container) is not null;

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        if (Wait.FirstDisplayedOrDefault(SidebarLocators.Container, timeout) is null)
        {
            throw new WebDriverTimeoutException(
                "The Microsoft 365 navigation rail did not render. If the browser window is narrow the rail " +
                "collapses — check the configured window size.");
        }

        Logger.LogDebug("Navigation rail is loaded.");
    }

    /// <summary>Clicks a known rail entry.</summary>
    public void NavigateTo(M365NavigationItem item) =>
        NavigateTo(NavigationItemCatalog.DisplayNameOf(item));

    /// <summary>
    /// Clicks a rail entry by its display label. Use this for tenant- or licence-specific entries that
    /// <see cref="M365NavigationItem"/> does not model.
    /// </summary>
    public void NavigateTo(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var element = Wait.FirstDisplayedOrDefault(SidebarLocators.Item(displayName))
            ?? throw new NoSuchElementException(
                $"No navigation item labelled '{displayName}' is present in the rail. " +
                $"Items currently available: {string.Join(", ", GetItemNames())}.");

        Logger.LogInformation("Navigating to '{Item}'.", displayName);

        Interactor.Click(element);
        Wait.UntilDocumentReady();
    }

    /// <summary>Whether a rail entry with this label exists — a licence check, effectively.</summary>
    public bool IsItemDisplayed(M365NavigationItem item) =>
        IsItemDisplayed(NavigationItemCatalog.DisplayNameOf(item));

    public bool IsItemDisplayed(string displayName) =>
        Wait.FirstDisplayedOrDefault(SidebarLocators.Item(displayName), TimeSpan.FromSeconds(2)) is not null;

    /// <summary>
    /// Whether the entry is the one currently active. M365 marks the active rail entry with
    /// aria-current or aria-selected, so this reads the same signal assistive technology does.
    /// </summary>
    public bool IsItemSelected(M365NavigationItem item) =>
        IsItemSelected(NavigationItemCatalog.DisplayNameOf(item));

    public bool IsItemSelected(string displayName)
    {
        var element = Wait.FirstDisplayedOrDefault(SidebarLocators.Item(displayName), TimeSpan.FromSeconds(2));

        if (element is null)
        {
            return false;
        }

        // The clickable element and the element carrying the state are not always the same node, so check
        // the ancestor chain too — Fluent UI commonly puts aria-selected on the wrapping list item.
        return IsMarkedCurrent(element) || IsMarkedCurrent(Parent(element));
    }

    /// <summary>The labels of every entry currently in the rail. Primarily a diagnostic aid.</summary>
    public IReadOnlyList<string> GetItemNames()
    {
        foreach (var locator in SidebarLocators.AllItems)
        {
            var names = Driver.FindElements(locator)
                .Where(element => element.Displayed)
                .Select(element => element.GetAttribute("aria-label")?.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count > 0)
            {
                return names;
            }
        }

        return [];
    }

    private static bool IsMarkedCurrent(IWebElement? element)
    {
        if (element is null)
        {
            return false;
        }

        var current = element.GetAttribute("aria-current");
        var selected = element.GetAttribute("aria-selected");

        return (!string.IsNullOrEmpty(current) && !current.Equals("false", StringComparison.OrdinalIgnoreCase))
            || string.Equals(selected, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static IWebElement? Parent(IWebElement element)
    {
        try
        {
            return element.FindElement(By.XPath("./.."));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }
}
