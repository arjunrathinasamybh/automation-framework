using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Automation.Core.Authentication;
using Automation.Core.Configuration;
using Automation.Core.Enums;
using Automation.Core.Interactions;
using Automation.Core.Waits;

namespace Automation.Pages.Login.Steps;

/// <summary>
/// Steers MFA away from push approval and towards a code prompt.
/// <para>
/// Most tenants default the Authenticator to "Approve a request" with number matching, which no automation
/// can satisfy â€” it needs a physical device. Entra ID offers "I can't use my Microsoft Authenticator app
/// right now", which leads to a method list containing "Use a verification code". This handler walks that
/// escape hatch so a TOTP secret is sufficient even when push is the tenant default.
/// </para>
/// </summary>
public sealed class VerificationMethodStepHandler : LoginStepHandlerBase
{
    private readonly MfaSettings _mfa;

    public VerificationMethodStepHandler(
        IOptions<MfaSettings> mfa,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<VerificationMethodStepHandler> logger)
        : base(wait, interactor, logger) => _mfa = mfa.Value;

    public override string Name => "ChooseVerificationMethod";

    public override int Order => LoginStepOrder.ChooseVerificationMethod;

    // Stands down in interactive mode. Steering a human away from push approval and onto a code prompt is
    // precisely the wrong move: push is the one method they can satisfy most easily, from their phone.
    public override bool IsCurrentScreen(LoginContext context) =>
        _mfa.Resolve(context.Credentials.HasTotpSecret) == MfaMode.Totp
        && (AnyDisplayed(MicrosoftLoginLocators.VerificationMethodList) ||
            AnyDisplayed(MicrosoftLoginLocators.PushApprovalPrompt));

    public override void Execute(LoginContext context)
    {
        if (AnyDisplayed(MicrosoftLoginLocators.VerificationMethodList))
        {
            Logger.LogDebug("Selecting 'Use a verification code' from the verification method list.");
            ClickFirst(MicrosoftLoginLocators.VerificationCodeOption, "'Use a verification code' option");
            WaitForScreenToAdvance(MicrosoftLoginLocators.VerificationMethodList);
            return;
        }

        // Push approval is on screen. Without a TOTP secret there is no way forward, so fail with an
        // instruction rather than idling until the login timeout expires.
        if (!context.Credentials.HasTotpSecret)
        {
            throw new AuthenticationFailedException(
                "The tenant is requesting Authenticator push approval (number matching), which cannot be " +
                "automated. Configure Credentials:TotpSecret with the account's authenticator secret so the " +
                "framework can switch to a verification code instead.");
        }

        if (!AnyDisplayed(MicrosoftLoginLocators.SignInAnotherWayLink))
        {
            throw new AuthenticationFailedException(
                "Push approval is required and the 'sign in another way' escape hatch is not offered. " +
                "Enable a verification-code (TOTP) method for this account in its Entra ID security info.");
        }

        Logger.LogDebug("Push approval requested; switching to another verification method.");
        ClickFirst(MicrosoftLoginLocators.SignInAnotherWayLink, "'sign in another way' link");
        WaitForScreenToAdvance(MicrosoftLoginLocators.PushApprovalPrompt);
    }
}
