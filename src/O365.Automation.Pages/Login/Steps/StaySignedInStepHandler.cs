using Microsoft.Extensions.Logging;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// "Stay signed in?" (KMSI). Answered from configuration: "Yes" persists a session cookie, which is only
/// useful when a persistent profile is being reused. An InPrivate run discards it either way, so the
/// default is "No".
/// </summary>
public sealed class StaySignedInStepHandler : LoginStepHandlerBase
{
    public StaySignedInStepHandler(
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<StaySignedInStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "StaySignedIn";

    public override int Order => LoginStepOrder.StaySignedIn;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.StaySignedInPrompt);

    public override void Execute(LoginContext context)
    {
        var stay = context.Credentials.StaySignedIn;

        Logger.LogDebug("Answering 'Stay signed in?' with {Answer}.", stay ? "Yes" : "No");

        if (stay)
        {
            ClickFirst(MicrosoftLoginLocators.PrimarySubmitButton, "'Yes' button");
        }
        else
        {
            ClickFirst(MicrosoftLoginLocators.StaySignedInNoButton, "'No' button");
        }

        WaitForScreenToAdvance(MicrosoftLoginLocators.StaySignedInPrompt);
    }
}
