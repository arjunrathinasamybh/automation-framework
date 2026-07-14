namespace O365.Automation.Core.Authentication;

/// <summary>
/// Generates the time-based one-time codes an authenticator app would otherwise produce.
/// Abstracted so tests can substitute a deterministic code and so the TOTP algorithm is not
/// welded into the login page object.
/// </summary>
public interface ITotpProvider
{
    /// <summary>Current 6-digit code for the given Base32 shared secret.</summary>
    string GenerateCode(string base32Secret);

    /// <summary>
    /// Seconds the current code remains valid. Callers should let a nearly-expired code roll over
    /// before submitting, otherwise Entra ID rejects a code that was valid when it was read.
    /// </summary>
    int SecondsUntilExpiry(string base32Secret);
}
