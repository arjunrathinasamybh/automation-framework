using Microsoft.Extensions.Logging;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>
/// "It looks like this email is used with more than one account" â€” the personal/work chooser.
/// Only appears when the UPN also exists as a Microsoft account; we always take the work-or-school tile.
/// </summary>
public sealed class WorkAccountTileStepHandler : LoginStepHandlerBase
{
    public WorkAccountTileStepHandler(
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<WorkAccountTileStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "WorkAccountTile";

    public override int Order => LoginStepOrder.WorkAccountTile;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.WorkAccountTile);

    public override void Execute(LoginContext context)
    {
        Logger.LogDebug("Account is ambiguous; selecting the work or school account.");

        ClickFirst(MicrosoftLoginLocators.WorkAccountTile, "work or school account tile");
        WaitForScreenToAdvance(MicrosoftLoginLocators.WorkAccountTile);
    }
}
