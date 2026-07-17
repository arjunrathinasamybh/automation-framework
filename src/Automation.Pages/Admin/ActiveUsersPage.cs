using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Automation.Core.Interactions;
using Automation.Core.Pages;
using Automation.Core.Waits;

namespace Automation.Pages.Admin;

/// <summary>
/// The admin center's Active users list (Users → Active users).
/// </summary>
public sealed class ActiveUsersPage : PageBase
{
    public ActiveUsersPage(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<ActiveUsersPage> logger)
        : base(driver, wait, interactor, logger) { }

    protected override By PageIdentifier => ActiveUsersLocators.UserList[0];

    public override bool IsDisplayed => Wait.FirstDisplayedOrDefault(ActiveUsersLocators.UserList) is not null;

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        // The admin center is a single-page app: the URL changes the instant the entry is clicked, well
        // before the list has fetched anything. Both signals are checked, in that order, so that "we
        // routed but the grid never arrived" is distinguishable from "we never routed at all".
        Wait.UntilUrlContains(ActiveUsersLocators.UrlFragment, timeout);

        if (Wait.FirstDisplayedOrDefault(ActiveUsersLocators.UserList, timeout) is null)
        {
            throw new WebDriverTimeoutException(
                "The Active users view was routed to, but its list never rendered.");
        }

        Logger.LogDebug("Active users is loaded.");
    }

    /// <summary>
    /// How many users the list is showing. The list is virtualised and paged, so this is the count of rows
    /// rendered right now — not the tenant's user count.
    /// </summary>
    public int VisibleUserCount() => VisibleUsers().Count;

    /// <summary>
    /// The display text of each rendered row, once the list has finished populating.
    /// <para>
    /// The wait is the point. The grid element exists before its rows arrive, so asking immediately —
    /// as this did until a run caught it — reports an empty list that is merely early. Waiting for the
    /// first row means an empty result can be trusted to mean the list really is empty.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> VisibleUsers()
    {
        try
        {
            Wait.Until(_ => UserRows().Count > 0);
        }
        catch (WebDriverTimeoutException)
        {
            // A genuinely empty list is a legitimate state to report, not a failure to raise here. The
            // caller asserting "at least one user" is better placed to say why that matters.
            Logger.LogDebug("The Active users list rendered no rows.");
        }

        return UserRows()
            .Select(row => row.Text.Replace('\n', ' ').Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();
    }

    private IReadOnlyList<IWebElement> UserRows()
    {
        foreach (var locator in ActiveUsersLocators.UserRows)
        {
            var rows = Driver.FindElements(locator).Where(row => row.Displayed).ToList();

            if (rows.Count > 0)
            {
                return rows;
            }
        }

        return [];
    }
}
