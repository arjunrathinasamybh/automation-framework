using Microsoft.Extensions.Logging;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>"Sign in" â€” enter the account UPN and press Next.</summary>
public sealed class UsernameStepHandler : LoginStepHandlerBase
{
    public UsernameStepHandler(IWaitService wait, IElementInteractor interactor, ILogger<UsernameStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "Username";

    public override int Order => LoginStepOrder.Username;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.UsernameInput);

    public override void Execute(LoginContext context)
    {
        Logger.LogDebug("Entering username {Username}.", context.Credentials.Username);

        TypeInto(MicrosoftLoginLocators.UsernameInput, context.Credentials.Username, "username field");
        ClickFirst(MicrosoftLoginLocators.PrimarySubmitButton, "Next button");

        // The field lingers for a moment while AAD resolves the tenant. Waiting for it to go means the
        // engine's next poll sees the *following* screen rather than handling this one twice.
        WaitForScreenToAdvance(MicrosoftLoginLocators.UsernameInput);
    }
}
