using Microsoft.Extensions.Options;
using NUnit.Framework;
using O365.Automation.Core.Configuration;

namespace O365.Automation.Specs.Support;

/// <summary>
/// The configured test account, and the one question every scenario that signs in has to ask first:
/// is there an account to sign in with?
/// <para>
/// Shared rather than repeated per step class, because the answer must be identical everywhere. A guard
/// that skips in one feature and fails in another turns a fresh clone from "green with scenarios skipped"
/// into "red for a reason you have to go and read".
/// </para>
/// </summary>
public sealed class TestAccount
{
    private readonly CredentialSettings _credentials;

    public TestAccount(IOptions<CredentialSettings> credentials) => _credentials = credentials.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_credentials.Username) && !string.IsNullOrWhiteSpace(_credentials.Password);

    /// <summary>
    /// Skips the scenario — rather than failing it — when no test account is configured. A fresh clone has
    /// no secrets by design, and a red suite there would be noise, not signal.
    /// </summary>
    public void RequireConfigured()
    {
        if (!IsConfigured)
        {
            Assert.Ignore(
                "No test account is configured, so this scenario cannot run. Set Credentials:Username in " +
                "Configuration/credentials.json, then supply the secrets out-of-band:\n" +
                "  dotnet user-secrets set \"Credentials:Password\"   \"<password>\"\n" +
                "  dotnet user-secrets set \"Credentials:TotpSecret\" \"<base32-secret>\"");
        }
    }
}
