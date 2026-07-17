using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Automation.Core.Configuration;
using Automation.Core.Enums;
using Automation.Core.Session;
using Automation.UnitTests.Infrastructure;

namespace Automation.UnitTests.Unit;

/// <summary>
/// Covers the copying, which is the part with something to get wrong. Whether a copied session satisfies
/// an identity provider is not knowable here — that needs a live tenant — but whether the right bytes land
/// in the right place is, and these are the mistakes that would look like an authentication bug.
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
public sealed class SessionProfileTests
{
    private string _root = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"session-profile-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public void CreateCopy_CopiesTheSessionState()
    {
        var profile = ProfileWith(master =>
        {
            Directory.CreateDirectory(Path.Combine(master, "Default", "Network"));
            File.WriteAllText(Path.Combine(master, "Default", "Network", "Cookies"), "the-session");
            File.WriteAllText(Path.Combine(master, "Local State"), "the-encryption-key");
        });

        var copy = profile.CreateCopy("a scenario");

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(Path.Combine(copy, "Default", "Network", "Cookies")), Is.EqualTo("the-session"));
            Assert.That(File.ReadAllText(Path.Combine(copy, "Local State")), Is.EqualTo("the-encryption-key"));
        });
    }

    [Test]
    public void CreateCopy_LeavesTheLockEntriesBehind()
    {
        // Copying these would tell a fresh browser that a profile it has never opened is already in use —
        // which is the failure the copying exists to avoid in the first place.
        var profile = ProfileWith(master =>
        {
            File.WriteAllText(Path.Combine(master, "SingletonLock"), "held");
            File.WriteAllText(Path.Combine(master, "SingletonCookie"), "held");
            File.WriteAllText(Path.Combine(master, "Local State"), "keep me");
        });

        var copy = profile.CreateCopy("a scenario");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(copy, "SingletonLock")), Is.False);
            Assert.That(File.Exists(Path.Combine(copy, "SingletonCookie")), Is.False);
            Assert.That(File.Exists(Path.Combine(copy, "Local State")), Is.True, "the session itself must still be copied");
        });
    }

    [Test]
    public void CreateCopy_SkipsCachesAndCrashDumps()
    {
        var profile = ProfileWith(master =>
        {
            Directory.CreateDirectory(Path.Combine(master, "Cache"));
            File.WriteAllText(Path.Combine(master, "Cache", "big-blob"), new string('x', 1024));
            Directory.CreateDirectory(Path.Combine(master, "Crashpad"));
            File.WriteAllText(Path.Combine(master, "Crashpad", "dump"), "boom");
            File.WriteAllText(Path.Combine(master, "Local State"), "keep me");
        });

        var copy = profile.CreateCopy("a scenario");

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(copy, "Cache")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(copy, "Crashpad")), Is.False);
            Assert.That(File.Exists(Path.Combine(copy, "Local State")), Is.True);
        });
    }

    [Test]
    public void CreateCopy_GivesEachScenarioItsOwnDirectory()
    {
        var profile = ProfileWith(master => File.WriteAllText(Path.Combine(master, "Local State"), "shared"));

        var first = profile.CreateCopy("first scenario");
        var second = profile.CreateCopy("second scenario");

        Assert.That(first, Is.Not.EqualTo(second), "two scenarios sharing a directory is the bug this design exists to prevent");
    }

    [Test]
    public void CreateCopy_PutsCopiesOutsideTheMaster()
    {
        // Inside it, the copy would be copying itself.
        var profile = ProfileWith(master => File.WriteAllText(Path.Combine(master, "Local State"), "x"));

        var copy = profile.CreateCopy("a scenario");

        Assert.That(copy, Does.Not.StartWith(profile.MasterDirectory + Path.DirectorySeparatorChar));
    }

    [Test]
    public void CreateCopy_ToleratesAScenarioNameThatIsNotAFileName()
    {
        var profile = ProfileWith(master => File.WriteAllText(Path.Combine(master, "Local State"), "x"));

        var copy = profile.CreateCopy("Active users: reached via \"Users\" / navigation");

        Assert.That(Directory.Exists(copy), Is.True);
    }

    [Test]
    public void Reset_DiscardsTheMasterAndEveryCopy()
    {
        var profile = ProfileWith(master => File.WriteAllText(Path.Combine(master, "Local State"), "x"));
        var copy = profile.CreateCopy("a scenario");

        profile.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(profile.MasterDirectory), Is.False);
            Assert.That(Directory.Exists(copy), Is.False);
            Assert.That(profile.HasMasterSession, Is.False);
        });
    }

    [Test]
    public void HasMasterSession_IsFalseBeforeAnythingHasSignedIn()
    {
        var profile = ProfileWith(_ => { });

        Assert.That(profile.HasMasterSession, Is.False);
    }

    [Test]
    public void IsEnabled_FollowsTheConfiguredMode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Profile(SessionMode.Reuse).IsEnabled, Is.True);
            Assert.That(Profile(SessionMode.Fresh).IsEnabled, Is.False);
        });
    }

    private SessionProfile Profile(SessionMode mode) =>
        new(
            Options.Create(new SessionSettings { Mode = mode, ProfileDirectory = Path.Combine(_root, "master") }),
            NullLogger<SessionProfile>.Instance);

    private SessionProfile ProfileWith(Action<string> populateMaster)
    {
        var profile = Profile(SessionMode.Reuse);

        Directory.CreateDirectory(profile.MasterDirectory);
        populateMaster(profile.MasterDirectory);

        return profile;
    }
}
