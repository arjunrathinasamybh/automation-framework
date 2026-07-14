using Microsoft.Extensions.Logging;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// Detects an Entra ID error banner and aborts sign-in with Microsoft's own message
/// ("Your account or password is incorrect", "That code didn't work", "Your account is locked", â€¦).
/// <para>
/// Runs before every other handler. The error is rendered on the same screen as the input that caused it,
/// so without this the flow would just retype the bad credential until the loop guard tripped, and report
/// a stall instead of the real reason.
/// </para>
/// </summary>
public sealed class LoginErrorStepHandler : LoginStepHandlerBase
{
    public LoginErrorStepHandler(
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<LoginErrorStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "LoginError";

    public override int Order => LoginStepOrder.Error;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.ErrorMessage);

    public override void Execute(LoginContext context)
    {
        var message = Wait.FirstDisplayedOrDefault(MicrosoftLoginLocators.ErrorMessage)?.Text.Trim();

        throw new AuthenticationFailedException(
            string.IsNullOrWhiteSpace(message)
                ? "Sign-in was rejected by Microsoft, but the error message could not be read."
                : $"Sign-in was rejected by Microsoft: \"{message}\"");
    }
}
