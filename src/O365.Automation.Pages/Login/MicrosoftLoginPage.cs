using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Pages;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login;

/// <summary>
/// The Entra ID sign-in surface.
/// <para>
/// Performing a sign-in belongs to <see cref="Core.Authentication.IAuthenticationService"/> and its step
/// handlers — the flow is a state machine, not a page. This page object exists for the assertions around
/// it: "were we redirected to sign-in?", "is this session really unauthenticated?".
/// </para>
/// <para>
/// The surface has two possible entry screens, and which one appears is a property of the environment
/// rather than the product: a private session gets the email prompt, while a normal session on a
/// domain- or Entra-joined machine gets "Pick an account" offering the Windows identity. Both mean
/// "sign-in is being requested", so both satisfy <see cref="IsDisplayed"/>.
/// </para>
/// </summary>
public sealed class MicrosoftLoginPage : PageBase
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    public MicrosoftLoginPage(
        IWebDriver driver,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<MicrosoftLoginPage> logger)
        : base(driver, wait, interactor, logger) { }

    protected override By PageIdentifier => MicrosoftLoginLocators.UsernameInput[0];

    /// <summary>True when either sign-in entry screen is rendered.</summary>
    public override bool IsDisplayed => UsernameFieldIsDisplayed || AccountPickerIsDisplayed;

    /// <summary>The email prompt — the entry screen for a private session.</summary>
    public bool UsernameFieldIsDisplayed =>
        Wait.FirstDisplayedOrDefault(MicrosoftLoginLocators.UsernameInput, ProbeTimeout) is not null;

    /// <summary>"Pick an account" — the entry screen when the browser can offer an SSO identity.</summary>
    public bool AccountPickerIsDisplayed =>
        Wait.FirstDisplayedOrDefault(MicrosoftLoginLocators.AccountPicker, ProbeTimeout) is not null;

    public override void WaitUntilLoaded(TimeSpan? timeout = null)
    {
        var screens = MicrosoftLoginLocators.UsernameInput
            .Concat(MicrosoftLoginLocators.AccountPicker)
            .ToList();

        if (Wait.FirstDisplayedOrDefault(screens, timeout) is null)
        {
            throw new WebDriverTimeoutException(
                $"The Microsoft sign-in page rendered neither an email prompt nor an account picker. " +
                $"Current URL: {Driver.Url}");
        }
    }
}
