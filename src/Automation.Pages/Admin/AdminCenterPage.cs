using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using Automation.Core.Interactions;
using Automation.Core.Pages;
using Automation.Core.Waits;
using Automation.Pages.M365;

namespace Automation.Pages.Admin;

/// <summary>
/// The Microsoft 365 admin center shell. Composes its navigation rather than re-implementing it.
/// <para>
/// Reached by URL rather than through the app launcher's "Admin" tile. The session cookie from sign-in
/// carries across, and going direct keeps the scenario independent of the launcher's markup — a surface
/// with nothing to do with the behaviour being specified.
/// </para>
/// </summary>
public sealed class AdminCenterPage : PageBase
{
    private readonly M365Settings _m365;

    public AdminCenterPage(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        AdminNavigationComponent navigation,
        IOptions<M365Settings> m365,
        ILogger<AdminCenterPage> logger)
        : base(driver, wait, interactor, logger)
    {
        Navigation = navigation;
        _m365 = m365.Value;
    }

    /// <summary>The left navigation.</summary>
    public AdminNavigationComponent Navigation { get; }

    /// <summary>
    /// Where the admin center lives. Exposed so a scenario can sign in *at* the admin center rather than
    /// signing in elsewhere and navigating here afterwards — unlike the M365 portal, this surface has no
    /// anonymous landing page, so it redirects straight to Entra and needs none of the portal's
    /// <c>?auth=2</c> persuasion.
    /// </summary>
    public string Url => _m365.AdminCenterUrl;

    protected override By PageIdentifier => AdminCenterLocators.NavigationContainer[0];

    public override bool IsDisplayed => Navigation.IsDisplayed;

    /// <summary>
    /// Navigates to the admin center. Requires an already-authenticated session: unauthenticated, this
    /// redirects to sign-in, and an account without an administrative role is refused by Microsoft.
    /// </summary>
    public void Open()
    {
        Logger.LogInformation("Opening {Url}.", _m365.AdminCenterUrl);
        Driver.Navigate().GoToUrl(_m365.AdminCenterUrl);
        WaitUntilLoaded();
    }

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        Wait.UntilDocumentReady(timeout);
        Navigation.WaitUntilLoaded(timeout);
    }
}
