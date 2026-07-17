using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Automation.Core.Authentication;
using Automation.Core.Configuration;
using Automation.Core.Enums;
using Automation.Core.Interactions;
using Automation.Core.Waits;

namespace Automation.Pages.Login.Steps;

/// <summary>"Enter code" â€” generate the current TOTP code and submit it.</summary>
public sealed class TotpStepHandler : LoginStepHandlerBase
{
    private readonly ITotpProvider _totp;
    private readonly MfaSettings _mfa;

    public TotpStepHandler(
        ITotpProvider totp,
        IOptions<MfaSettings> mfa,
        IWaitService wait,
        IElementInteractor interactor,
        ILogger<TotpStepHandler> logger)
        : base(wait, interactor, logger)
    {
        _totp = totp;
        _mfa = mfa.Value;
    }

    public override string Name => "Totp";

    public override int Order => LoginStepOrder.Totp;

    // Stands down in interactive mode: the operator is answering the challenge by hand, and typing a
    // generated code into the field underneath them would collide with what they are doing.
    public override bool IsCurrentScreen(LoginContext context) =>
        _mfa.Resolve(context.Credentials.HasTotpSecret) == MfaMode.Totp
        && AnyDisplayed(MicrosoftLoginLocators.TotpInput);

    public override void Execute(LoginContext context)
    {
        if (!context.Credentials.HasTotpSecret)
        {
            throw new AuthenticationFailedException(
                "A verification code was requested but no TOTP secret is configured. Set it out-of-band: " +
                "`dotnet user-secrets set \"Credentials:TotpSecret\" \"<base32-secret>\"` locally, or the " +
                "Credentials__TotpSecret environment variable in CI.");
        }

        // Generated as late as possible: a code read early can expire during the form round trip.
        // GenerateCode() additionally rolls over to the next window if the current one is nearly spent.
        var code = _totp.GenerateCode(context.Credentials.TotpSecret!);

        Logger.LogDebug("Submitting TOTP code (valid for a further {Seconds}s).",
            _totp.SecondsUntilExpiry(context.Credentials.TotpSecret!));

        TypeInto(MicrosoftLoginLocators.TotpInput, code, "verification code field");
        ClickFirst(MicrosoftLoginLocators.TotpSubmitButton, "Verify button");

        WaitForScreenToAdvance(MicrosoftLoginLocators.TotpInput);
    }
}
