using Microsoft.Extensions.Logging;
using OtpNet;

namespace O365.Automation.Core.Authentication;

/// <summary>
/// RFC 6238 TOTP generator (Otp.NET) matching Microsoft Authenticator's parameters:
/// 30-second step, 6 digits, HMAC-SHA1.
/// </summary>
public sealed class TotpProvider : ITotpProvider
{
    /// <summary>
    /// A code with less than this much life left is not worth submitting: the round trip through the
    /// form plus Entra ID's validation can outlive it, producing a spurious "code expired" failure.
    /// </summary>
    private const int MinimumRemainingSeconds = 5;

    private readonly ILogger<TotpProvider> _logger;

    public TotpProvider(ILogger<TotpProvider> logger) => _logger = logger;

    public string GenerateCode(string base32Secret)
    {
        var totp = CreateTotp(base32Secret);

        if (totp.RemainingSeconds() < MinimumRemainingSeconds)
        {
            var wait = totp.RemainingSeconds() + 1;
            _logger.LogDebug("TOTP code expires in under {Minimum}s; waiting {Wait}s for the next window.",
                MinimumRemainingSeconds, wait);
            Thread.Sleep(TimeSpan.FromSeconds(wait));
        }

        return totp.ComputeTotp();
    }

    public int SecondsUntilExpiry(string base32Secret) => CreateTotp(base32Secret).RemainingSeconds();

    private static Totp CreateTotp(string base32Secret)
    {
        if (string.IsNullOrWhiteSpace(base32Secret))
        {
            throw new AuthenticationFailedException(
                "A TOTP code was requested but no TOTP secret is configured. " +
                "Set Credentials:TotpSecret via user-secrets or the Credentials__TotpSecret environment variable.");
        }

        try
        {
            // Authenticator secrets are shown grouped in fours; strip the formatting users paste in.
            var normalized = base32Secret.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
            return new Totp(Base32Encoding.ToBytes(normalized));
        }
        catch (ArgumentException ex)
        {
            throw new AuthenticationFailedException(
                "The configured TOTP secret is not valid Base32. Use the 'secret key' shown when enrolling " +
                "the authenticator app, not the QR image or the recovery code.", ex);
        }
    }
}
