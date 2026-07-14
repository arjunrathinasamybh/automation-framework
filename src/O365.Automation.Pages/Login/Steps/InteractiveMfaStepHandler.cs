using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Drivers;
using O365.Automation.Core.Enums;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// Hands the live browser to the operator to complete MFA, then resumes automatically.
/// <para>
/// Deliberately types nothing. The operator satisfies the challenge in the real Microsoft page, exactly as
/// they would signing in by hand — an Authenticator code, a push approval with number matching, an SMS, or
/// a hardware key. That is why this one handler supports every authenticator type without modelling any of
/// them: the human, not the framework, answers whatever the tenant asks.
/// </para>
/// <para>
/// A console prompt was considered and rejected: under <c>dotnet test</c> stdin is redirected, so
/// <c>Console.ReadLine()</c> returns null rather than waiting for input. Watching the browser works
/// regardless of how the suite was launched.
/// </para>
/// </summary>
public sealed class InteractiveMfaStepHandler : LoginStepHandlerBase
{
    private readonly IWebDriver _driver;
    private readonly BrowserSessionContext _session;
    private readonly MfaSettings _mfa;

    public InteractiveMfaStepHandler(
        IWebDriver driver,
        BrowserSessionContext session,
        IOptions<MfaSettings> mfa,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<InteractiveMfaStepHandler> logger)
        : base(wait, interactor, logger)
    {
        _driver = driver;
        _session = session;
        _mfa = mfa.Value;
    }

    public override string Name => "InteractiveMfa";

    /// <summary>
    /// Outranks both the TOTP step and the verification-method chooser. In interactive mode we must not
    /// steer the flow towards a code prompt — the operator may be about to approve a push, and reshaping
    /// the screen underneath them would be worse than useless.
    /// </summary>
    public override int Order => LoginStepOrder.InteractiveMfa;

    public override bool IsCurrentScreen(LoginContext context) =>
        _mfa.Resolve(context.Credentials.HasTotpSecret) == MfaMode.Interactive
        && AnyDisplayed(MicrosoftLoginLocators.AnyMfaChallenge);

    public override void Execute(LoginContext context)
    {
        if (_session.Settings.Headless)
        {
            throw new AuthenticationFailedException(
                "Multi-factor authentication needs to be completed by hand, but the browser is headless, so " +
                "there is nothing for you to complete it in. Either turn off Browser:Headless, or configure " +
                "Credentials:TotpSecret so MFA can be answered automatically.");
        }

        Prompt(context);

        WaitForOperator();
    }

    private void Prompt(LoginContext context)
    {
        var numberToMatch = Wait
            .FirstDisplayedOrDefault(MicrosoftLoginLocators.NumberMatchingDigits, TimeSpan.FromSeconds(2))
            ?.Text.Trim();

        var instruction = string.IsNullOrWhiteSpace(numberToMatch)
            ? "Complete the verification in the browser window."
            : $"Approve the sign-in request and enter {numberToMatch} in your Authenticator app.";

        // Logged at Warning so it survives the default log filters and is impossible to miss in the output:
        // the run is now blocked on a person, and a message they never see is a message that never worked.
        Logger.LogWarning(
            "\n" +
            "==================================================================\n" +
            "  ACTION REQUIRED — multi-factor authentication\n" +
            "  Account : {Username}\n" +
            "  {Instruction}\n" +
            "  Waiting up to {Timeout:g} for you to finish.\n" +
            "==================================================================",
            context.Credentials.Username,
            instruction,
            _mfa.InteractiveTimeout);
    }

    private void WaitForOperator()
    {
        try
        {
            // Done when every MFA screen has cleared — no matter which one the tenant chose to show, and
            // no matter how the operator satisfied it.
            Wait.Until(
                _ => !AnyDisplayed(MicrosoftLoginLocators.AnyMfaChallenge),
                _mfa.InteractiveTimeout);
        }
        catch (WebDriverTimeoutException exception)
        {
            throw new AuthenticationFailedException(
                $"Multi-factor authentication was not completed within {_mfa.InteractiveTimeoutSeconds}s. " +
                $"Raise Mfa:InteractiveTimeoutSeconds if you need longer. Current URL: {_driver.Url}",
                exception);
        }

        Logger.LogInformation("Multi-factor authentication completed; resuming.");
    }
}
