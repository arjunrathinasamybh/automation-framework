using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Pages;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Admin;

/// <summary>
/// The Microsoft 365 admin center's left navigation.
/// <para>
/// Deliberately not <c>SidebarComponent</c>, despite the family resemblance. That rail is flat — every
/// entry is always present and one click away. Here, entries such as "Active users" live inside a
/// collapsible group ("Users") and are rendered but hidden until the group is expanded. Modelling that is
/// the component's whole job, so folding the two together would put nested behaviour behind a flat
/// interface.
/// </para>
/// </summary>
public sealed class AdminNavigationComponent : PageBase
{
    /// <summary>
    /// How long to wait for an entry that ought to be on screen already. Long enough to absorb a re-render,
    /// short enough that a genuinely absent entry reports quickly rather than burning the full timeout.
    /// </summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    public AdminNavigationComponent(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<AdminNavigationComponent> logger)
        : base(driver, wait, interactor, logger) { }

    protected override By PageIdentifier => AdminCenterLocators.NavigationContainer[0];

    public override bool IsDisplayed =>
        Wait.FirstDisplayedOrDefault(AdminCenterLocators.NavigationContainer) is not null;

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        if (Wait.FirstDisplayedOrDefault(AdminCenterLocators.NavigationContainer, timeout) is null)
        {
            throw new WebDriverTimeoutException(
                "The Microsoft 365 admin center navigation did not render. If the browser window is narrow " +
                "the navigation collapses — check the configured window size.");
        }

        Logger.LogDebug("Admin center navigation is loaded.");
    }

    /// <summary>
    /// Expands a collapsible group, if it is not expanded already, so that its entries exist in the DOM.
    /// Safe to call when the group is already open — it does nothing rather than collapsing it.
    /// </summary>
    public void ExpandGroup(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var group = Find(displayName);

        if (IsExpanded(group) is true)
        {
            Logger.LogDebug("Navigation group '{Group}' is already expanded.", displayName);
            return;
        }

        Logger.LogInformation("Expanding navigation group '{Group}'.", displayName);
        Interactor.Click(group);

        // The click is what makes the children exist, so wait for the group to report itself open rather
        // than letting the next NavigateTo race the re-render.
        try
        {
            Wait.Until(_ => IsExpanded(Find(displayName)) is not false, ProbeTimeout);
        }
        catch (WebDriverTimeoutException)
        {
            // Not fatal in itself: the group may simply not publish aria-expanded. Let the child lookup be
            // the judge — it produces a far more useful message than a timeout here would.
            Logger.LogDebug("Group '{Group}' never reported itself expanded; continuing.", displayName);
        }
    }

    /// <summary>Clicks a navigation entry by its display label.</summary>
    public void NavigateTo(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Logger.LogInformation("Navigating to '{Item}'.", displayName);

        Interactor.Click(Find(displayName));
        Wait.UntilDocumentReady();
    }

    /// <summary>Whether an entry with this label is currently visible in the navigation.</summary>
    public bool IsItemDisplayed(string displayName) =>
        Wait.FirstDisplayedOrDefault(AdminCenterLocators.Item(displayName), ProbeTimeout) is not null;

    /// <summary>The labels of every entry currently in the navigation. Primarily a diagnostic aid.</summary>
    public IReadOnlyList<string> GetItemNames()
    {
        foreach (var locator in AdminCenterLocators.AllItems)
        {
            var names = Driver.FindElements(locator)
                .Where(element => element.Displayed)
                .Select(NameOf)
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

    private IWebElement Find(string displayName) =>
        Wait.FirstDisplayedOrDefault(AdminCenterLocators.Item(displayName), ProbeTimeout)
            ?? throw new NoSuchElementException(
                $"No navigation entry labelled '{displayName}' is present in the admin center. " +
                $"On this surface that most often means the signed-in account does not hold the " +
                $"administrative role that reveals it, rather than that the page changed. " +
                $"Entries currently available: {string.Join(", ", GetItemNames())}.");

    /// <summary>
    /// Whether a group is open. Null means the element does not say — which is not the same as "closed",
    /// and callers treat it accordingly.
    /// </summary>
    private static bool? IsExpanded(IWebElement element)
    {
        // The clickable node and the node carrying the state are not always the same: Fluent commonly puts
        // aria-expanded on the wrapping tree item rather than the button inside it.
        var expanded = element.GetAttribute("aria-expanded")
            ?? Ancestor(element)?.GetAttribute("aria-expanded");

        return expanded is null ? null : expanded.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NameOf(IWebElement element) =>
        element.GetAttribute(AdminCenterLocators.NameAttribute)?.Trim() is { Length: > 0 } name
            ? name
            : element.GetAttribute("aria-label")?.Trim() is { Length: > 0 } label
                ? label
                : element.Text.Trim() is { Length: > 0 } text
                    ? text
                    : null;

    private static IWebElement? Ancestor(IWebElement element)
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
