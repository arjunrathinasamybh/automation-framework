using O365.Automation.Core.Configuration;

namespace O365.Automation.Core.Authentication;

public interface IAuthenticationService
{
    /// <summary>
    /// Navigates to the application and completes sign-in, using the configured credentials unless
    /// <paramref name="credentials"/> is supplied. Returns once the authenticated landing page is reached.
    /// </summary>
    /// <exception cref="AuthenticationFailedException">Credentials rejected, MFA failed, or the flow stalled.</exception>
    void SignIn(CredentialSettings? credentials = null);

    /// <summary>True when the browser is currently on an authenticated application URL.</summary>
    bool IsAuthenticated { get; }
}
