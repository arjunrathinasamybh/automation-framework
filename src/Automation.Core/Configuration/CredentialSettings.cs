namespace Automation.Core.Configuration;

/// <summary>
/// Test account credentials.
/// <para>
/// NEVER commit values for <see cref="Password"/> or <see cref="TotpSecret"/>. Supply them at run time via
/// dotnet user-secrets (local) or environment variables / a secret store (CI):
/// <c>Credentials__Password</c>, <c>Credentials__TotpSecret</c>.
/// </para>
/// </summary>
public sealed class CredentialSettings
{
    public const string SectionName = "Credentials";

    /// <summary>UPN of the automation account, e.g. svc.automation@contoso.onmicrosoft.com</summary>
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Base32 TOTP shared secret captured when the authenticator app was enrolled for this account.
    /// Leave empty if the account is excluded from MFA — the TOTP step is then simply never triggered.
    /// </summary>
    public string? TotpSecret { get; set; }

    /// <summary>Answer to the "Stay signed in?" prompt. Keep false for InPrivate runs.</summary>
    public bool StaySignedIn { get; set; }

    public bool HasTotpSecret => !string.IsNullOrWhiteSpace(TotpSecret);
}
