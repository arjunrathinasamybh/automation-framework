using Automation.Core.Enums;

namespace Automation.Core.Configuration;

/// <summary>
/// How multi-factor authentication is satisfied.
/// </summary>
public sealed class MfaSettings
{
    public const string SectionName = "Mfa";

    /// <summary>How to answer an MFA challenge. See <see cref="MfaMode"/>.</summary>
    public MfaMode Mode { get; set; } = MfaMode.Automatic;

    /// <summary>
    /// How long to wait for a human to complete MFA in the browser when running interactively.
    /// Generous by default: it has to cover finding a phone and unlocking it.
    /// </summary>
    public int InteractiveTimeoutSeconds { get; set; } = 300;

    public TimeSpan InteractiveTimeout => TimeSpan.FromSeconds(InteractiveTimeoutSeconds);

    /// <summary>
    /// Resolves <see cref="MfaMode.Automatic"/> into a concrete mode: use the stored TOTP secret when one
    /// is configured, otherwise hand the browser to the operator. This is what makes the framework work
    /// unattended in CI (where the secret is injected) and interactively on a developer machine (where it
    /// usually is not) with no configuration change between them.
    /// </summary>
    public MfaMode Resolve(bool hasTotpSecret) => Mode switch
    {
        MfaMode.Automatic => hasTotpSecret ? MfaMode.Totp : MfaMode.Interactive,
        var explicitMode => explicitMode
    };
}
