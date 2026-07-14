using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using O365.Automation.Core.Authentication;
using O365.Automation.UnitTests.Infrastructure;

namespace O365.Automation.UnitTests.Unit;

[TestFixture]
[Category(TestCategories.Unit)]
public sealed class TotpProviderTests
{
    // RFC 6238 test vector secret ("12345678901234567890" in Base32).
    private const string ValidSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private TotpProvider _provider = null!;

    [SetUp]
    public void SetUp() => _provider = new TotpProvider(NullLogger<TotpProvider>.Instance);

    [Test]
    public void GenerateCode_WithValidSecret_ReturnsSixDigits()
    {
        var code = _provider.GenerateCode(ValidSecret);

        Assert.That(code, Does.Match("^[0-9]{6}$"));
    }

    [Test]
    [Description("Authenticator secrets are displayed in spaced groups; pasting them verbatim must still work.")]
    public void GenerateCode_WithFormattedSecret_IgnoresSpacingAndCase()
    {
        var formatted = "gezd gnbv gy3t qojq gezd gnbv gy3t qojq";

        Assert.That(_provider.GenerateCode(formatted), Is.EqualTo(_provider.GenerateCode(ValidSecret)));
    }

    [Test]
    public void GenerateCode_WithMissingSecret_ThrowsWithActionableMessage()
    {
        var exception = Assert.Throws<AuthenticationFailedException>(() => _provider.GenerateCode(""));

        Assert.That(exception!.Message, Does.Contain("TotpSecret"));
    }

    [Test]
    public void GenerateCode_WithNonBase32Secret_ThrowsWithActionableMessage()
    {
        // '1' and '8' are not in the Base32 alphabet â€” the classic "pasted the QR payload" mistake.
        var exception = Assert.Throws<AuthenticationFailedException>(() => _provider.GenerateCode("11118888"));

        Assert.That(exception!.Message, Does.Contain("Base32"));
    }

    [Test]
    public void SecondsUntilExpiry_IsWithinTheThirtySecondWindow()
    {
        Assert.That(_provider.SecondsUntilExpiry(ValidSecret), Is.InRange(1, 30));
    }
}
