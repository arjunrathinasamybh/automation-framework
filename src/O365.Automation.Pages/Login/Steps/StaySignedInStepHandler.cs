using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// "Stay signed in?" (KMSI). "Yes" issues a persistent cookie that outlives the browser process; "No"
/// issues one that dies with it.
/// <para>
/// Answered from <c>Credentials:StaySignedIn</c>, except that reusing a session profile *requires* Yes and
/// therefore implies it. One scenario is still one browser process — only the profile on disk is shared —
/// so a cookie that dies with the process would leave every scenario signing in again, reusing the
/// profile faithfully and gaining nothing. Deriving the answer here means the two settings cannot be set
/// to contradict each other.
/// </para>
/// </summary>
public sealed class StaySignedInStepHandler : LoginStepHandlerBase
{
    private readonly SessionSettings _session;

    public StaySignedInStepHandler(
        IWaitService wait,
        IElementInteractor interactor,
        IOptions<SessionSettings> session,
        ILogger<StaySignedInStepHandler> logger)
        : base(wait, interactor, logger)
        => _session = session.Value;

    public override string Name => "StaySignedIn";

    public override int Order => LoginStepOrder.StaySignedIn;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.StaySignedInPrompt);

    public override void Execute(LoginContext context)
    {
        var stay = context.Credentials.StaySignedIn || _session.IsReused;

        Logger.LogDebug(
            "Answering 'Stay signed in?' with {Answer}{Reason}.",
            stay ? "Yes" : "No",
            !context.Credentials.StaySignedIn && _session.IsReused
                ? " (session reuse needs a cookie that outlives the browser)"
                : string.Empty);

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
