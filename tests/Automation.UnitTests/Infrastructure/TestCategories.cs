namespace Automation.UnitTests.Infrastructure;

/// <summary>
/// Category names for test filtering, e.g. <c>dotnet test --filter "TestCategory=Smoke"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>Drives a real browser against a real tenant. Needs credentials; slow.</summary>
    public const string EndToEnd = "E2E";

    /// <summary>The minimal set that must pass before a build is considered usable.</summary>
    public const string Smoke = "Smoke";

    /// <summary>No browser, no network â€” pure unit tests of the framework itself.</summary>
    public const string Unit = "Unit";
}
