using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Pages;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.M365;

/// <summary>
/// The Microsoft 365 landing page reached after sign-in. Composes the navigation rail rather than
/// re-implementing it.
/// </summary>
public sealed class M365HomePage : PageBase
{
    private readonly ApplicationSettings _application;

    public M365HomePage(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        SidebarComponent sidebar,
        IOptions<ApplicationSettings> application,
        ILogger<M365HomePage> logger)
        : base(driver, wait, interactor, logger)
    {
        Sidebar = sidebar;
        _application = application.Value;
    }

    /// <summary>The left navigation rail.</summary>
    public SidebarComponent Sidebar { get; }

    protected override By PageIdentifier => SidebarLocators.Container[0];

    public override bool IsDisplayed => Sidebar.IsDisplayed;

    /// <summary>Navigates straight to the M365 home page. Redirects to sign-in when unauthenticated.</summary>
    public void Open()
    {
        Logger.LogInformation("Opening {Url}.", _application.BaseUrl);
        Driver.Navigate().GoToUrl(_application.BaseUrl);
        WaitUntilLoaded();
    }

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        Wait.UntilDocumentReady(timeout);
        Sidebar.WaitUntilLoaded(timeout);
    }
}
