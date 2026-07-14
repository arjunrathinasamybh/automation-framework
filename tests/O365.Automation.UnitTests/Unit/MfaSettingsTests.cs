using NUnit.Framework;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Enums;
using O365.Automation.UnitTests.Infrastructure;

namespace O365.Automation.UnitTests.Unit;

[TestFixture]
[Category(TestCategories.Unit)]
public sealed class MfaSettingsTests
{
    [Test]
    [Description("Automatic runs unattended when a secret exists — the CI case.")]
    public void Automatic_WithATotpSecret_UsesTotp() =>
        Assert.That(
            new MfaSettings { Mode = MfaMode.Automatic }.Resolve(hasTotpSecret: true),
            Is.EqualTo(MfaMode.Totp));

    [Test]
    [Description("Automatic falls back to a human when no secret exists — the developer-machine case.")]
    public void Automatic_WithoutATotpSecret_UsesInteractive() =>
        Assert.That(
            new MfaSettings { Mode = MfaMode.Automatic }.Resolve(hasTotpSecret: false),
            Is.EqualTo(MfaMode.Interactive));

    [Test]
    [Description("An explicit mode is never second-guessed, even when a secret is available.")]
    public void Interactive_IsHonouredEvenWhenASecretExists() =>
        Assert.That(
            new MfaSettings { Mode = MfaMode.Interactive }.Resolve(hasTotpSecret: true),
            Is.EqualTo(MfaMode.Interactive));

    [Test]
    [Description("Totp stays Totp with no secret, so the TOTP step can fail with a precise message rather " +
                 "than silently stalling on a prompt nobody is watching.")]
    public void Totp_IsHonouredEvenWithoutASecret() =>
        Assert.That(
            new MfaSettings { Mode = MfaMode.Totp }.Resolve(hasTotpSecret: false),
            Is.EqualTo(MfaMode.Totp));

    [Test]
    public void InteractiveTimeout_DefaultsToFiveMinutes() =>
        Assert.That(new MfaSettings().InteractiveTimeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
}
