using O365.Automation.Core.Configuration;

namespace O365.Automation.Core.Authentication;

public interface IAuthenticationService
{
    /// <summary>
    /// Navigates to the application and completes sign-in, using the configured credentials unless
    /// <paramref name="credentials"/> is supplied. Returns once an authenticated page is reached.
    /// </summary>
    /// <param name="credentials">Overrides the configured account. Mainly for negative scenarios.</param>
    /// <param name="startUrl">
    /// Where to begin, defaulting to <c>Application:SignInUrl</c>. Supply the surface the scenario is
    /// actually about — signing in at the portal and then navigating elsewhere loads a page nobody
    /// asserts on. Whatever it lands on must match <c>Application:AuthenticatedUrlMarkers</c>, or the flow
    /// will not recognise that it has arrived.
    /// </param>
    /// <exception cref="AuthenticationFailedException">Credentials rejected, MFA failed, or the flow stalled.</exception>
    void SignIn(CredentialSettings? credentials = null, string? startUrl = null);

    /// <summary>True when the browser is currently on an authenticated application URL.</summary>
    bool IsAuthenticated { get; }
}
