using Microsoft.Extensions.Logging;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Interactions;
using O365.Automation.Core.Waits;

namespace O365.Automation.Pages.Login.Steps;

/// <summary>"Enter password" â€” supply the password and sign in.</summary>
public sealed class PasswordStepHandler : LoginStepHandlerBase
{
    public PasswordStepHandler(IWaitService wait, IElementInteractor interactor, ILogger<PasswordStepHandler> logger)
        : base(wait, interactor, logger) { }

    public override string Name => "Password";

    public override int Order => LoginStepOrder.Password;

    public override bool IsCurrentScreen(LoginContext context) => AnyDisplayed(MicrosoftLoginLocators.PasswordInput);

    public override void Execute(LoginContext context)
    {
        if (string.IsNullOrEmpty(context.Credentials.Password))
        {
            throw new AuthenticationFailedException(
                "The password screen was reached but no password is configured. Supply it out-of-band: " +
                "`dotnet user-secrets set \"Credentials:Password\" \"<value>\"` locally, or the " +
                "Credentials__Password environment variable in CI. Passwords must never be committed.");
        }

        Logger.LogDebug("Entering password.");

        TypeInto(MicrosoftLoginLocators.PasswordInput, context.Credentials.Password, "password field");
        ClickFirst(MicrosoftLoginLocators.PrimarySubmitButton, "Sign in button");

        WaitForScreenToAdvance(MicrosoftLoginLocators.PasswordInput);
    }
}
