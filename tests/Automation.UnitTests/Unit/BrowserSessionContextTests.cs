using NUnit.Framework;
using Automation.Core.Configuration;
using Automation.Core.Drivers;
using Automation.Core.Enums;
using Automation.UnitTests.Infrastructure;

namespace Automation.UnitTests.Unit;

[TestFixture]
[Category(TestCategories.Unit)]
public sealed class BrowserSessionContextTests
{
    [Test]
    public void Constructor_CopiesTheConfiguredDefaults()
    {
        var defaults = new BrowserSettings { Type = BrowserType.Firefox, Mode = BrowsingMode.Normal };

        var context = new BrowserSessionContext(defaults);

        Assert.Multiple(() =>
        {
            Assert.That(context.Settings.Type, Is.EqualTo(BrowserType.Firefox));
            Assert.That(context.Settings.Mode, Is.EqualTo(BrowsingMode.Normal));
        });
    }

    [Test]
    public void Use_OverridesOnlyWhatIsSupplied()
    {
        var context = new BrowserSessionContext(
            new BrowserSettings { Type = BrowserType.Edge, Mode = BrowsingMode.InPrivate });

        context.Use(browser: BrowserType.Chrome);

        Assert.Multiple(() =>
        {
            Assert.That(context.Settings.Type, Is.EqualTo(BrowserType.Chrome));
            Assert.That(context.Settings.Mode, Is.EqualTo(BrowsingMode.InPrivate), "Mode should be untouched.");
        });
    }

    [Test]
    [Description("The guard against one test's browser choice leaking into the next: BrowserSettings comes " +
                 "from IOptions as a shared singleton, so the context must copy it, not alias it.")]
    public void Use_DoesNotMutateTheSharedSettingsInstance()
    {
        var shared = new BrowserSettings { Type = BrowserType.Edge, Mode = BrowsingMode.InPrivate };

        new BrowserSessionContext(shared).Use(BrowserType.Firefox, BrowsingMode.Normal);

        Assert.Multiple(() =>
        {
            Assert.That(shared.Type, Is.EqualTo(BrowserType.Edge));
            Assert.That(shared.Mode, Is.EqualTo(BrowsingMode.InPrivate));
        });
    }

    [Test]
    public void Use_DoesNotShareTheAdditionalArgumentsList()
    {
        var shared = new BrowserSettings { AdditionalArguments = { "--first" } };

        var context = new BrowserSessionContext(shared);
        context.Settings.AdditionalArguments.Add("--second");

        Assert.That(shared.AdditionalArguments, Has.Count.EqualTo(1));
    }
}
