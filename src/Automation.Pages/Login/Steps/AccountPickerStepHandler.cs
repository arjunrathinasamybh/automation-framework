using Microsoft.Extensions.Logging;
using Automation.Core.Authentication;
using Automation.Core.Interactions;
using Automation.Core.Waits;

namespace Automation.Pages.Login.Steps;

/// <summary>
/// "Pick an account" â€” always chooses "Use another account".
/// <para>
/// A normal (non-private) browser on a domain- or Entra-joined machine offers the signed-in Windows
/// identity through seamless SSO. Accepting it would silently run the suite as whoever is logged into the
/// build agent â€” a different account, different licences, different permissions â€” and the failures that
/// causes are near-impossible to diagnose. Automation must always authenticate as the account it was
/// configured with, so this handler unconditionally declines the offer.
/// </para>
/// <para>InPrivate sessions never reach this screen, which is the main reason InPrivate is the default.</para>
/// </summary>
public sealed class AccountPickerStepHandler : LoginStepHandlerBase
{
    public AccountPickerStepHandler(
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<AccountPickerStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "AccountPicker";

    public override int Order => LoginStepOrder.AccountPicker;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.AccountPicker);

    public override void Execute(LoginContext context)
    {
        Logger.LogDebug("Account picker shown; declining the cached accounts and using the configured one.");

        ClickFirst(MicrosoftLoginLocators.UseAnotherAccountTile, "'Use another account' tile");
        WaitForScreenToAdvance(MicrosoftLoginLocators.AccountPicker);
    }
}
