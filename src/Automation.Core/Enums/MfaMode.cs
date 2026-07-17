namespace Automation.Core.Enums;

/// <summary>
/// How the framework answers a multi-factor authentication challenge.
/// </summary>
public enum MfaMode
{
    /// <summary>
    /// Use <see cref="Totp"/> when a TOTP secret is configured, otherwise <see cref="Interactive"/>.
    /// The default: unattended in CI, hands-on locally, with no configuration change between them.
    /// </summary>
    Automatic,

    /// <summary>
    /// Generate the verification code from the stored Base32 secret. Fully unattended — the only mode
    /// that can run in CI or headless.
    /// </summary>
    Totp,

    /// <summary>
    /// Pause and hand the live browser to the operator, who completes the challenge however the tenant
    /// asks — Authenticator code, push approval with number matching, SMS, or a hardware key — after which
    /// the run continues automatically.
    /// <para>
    /// Needs no TOTP secret and supports every authenticator type, because a human is doing it in the real
    /// page. Requires a visible browser, so it cannot run headless or in CI.
    /// </para>
    /// </summary>
    Interactive
}
