using Automation.Core.Configuration;

namespace Automation.Core.Authentication;

/// <summary>
/// State passed to each login step handler for a single sign-in attempt.
/// </summary>
public sealed class LoginContext
{
    public LoginContext(CredentialSettings credentials) => Credentials = credentials;

    public CredentialSettings Credentials { get; }

    /// <summary>Number of screens handled so far. Useful for diagnostics.</summary>
    public int StepsCompleted { get; internal set; }
}
