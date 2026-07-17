using NUnit.Framework;
using Automation.Core.Authentication;
using Automation.Core.Diagnostics;
using Automation.Pages.Admin;
using Automation.Specs.Support;
using Reqnroll;

namespace Automation.Specs.Steps;

[Binding]
public sealed class AdminCenterSteps
{
    private readonly IAuthenticationService _authentication;
    private readonly AdminCenterPage _adminCenter;
    private readonly ActiveUsersPage _activeUsers;
    private readonly TestAccount _account;
    private readonly IEvidenceRecorder _evidence;

    public AdminCenterSteps(
        IAuthenticationService authentication,
        AdminCenterPage adminCenter,
        ActiveUsersPage activeUsers,
        TestAccount account,
        IEvidenceRecorder evidence)
    {
        _authentication = authentication;
        _adminCenter = adminCenter;
        _activeUsers = activeUsers;
        _account = account;
        _evidence = evidence;
    }

    /// <summary>
    /// Signs in at the admin center itself, rather than signing in to the portal and navigating here.
    /// The portal is not part of this behaviour, and loading it would cost a page nobody asserts on.
    /// </summary>
    [Given("I am signed in to the Microsoft 365 admin center")]
    public void GivenIAmSignedInToTheAdminCenter()
    {
        _account.RequireConfigured();

        _authentication.SignIn(startUrl: _adminCenter.Url);
        _adminCenter.WaitUntilLoaded();

        _evidence.Capture("Signed in to the admin center");
    }

    [When("I open the Microsoft 365 admin center")]
    public void WhenIOpenTheAdminCenter() => _adminCenter.Open();

    // The group and entry arrive as display labels, so any other admin entry — Contacts, Guest users,
    // Teams — is a scenario away without a code change.
    [When("I select {string} under {string} in the admin navigation")]
    public void WhenISelectUnderInTheAdminNavigation(string item, string group)
    {
        _adminCenter.Navigation.ExpandGroup(group);
        _adminCenter.Navigation.NavigateTo(item);
    }

    [Then("the active users list is shown")]
    public void ThenTheActiveUsersListIsShown()
    {
        _activeUsers.WaitUntilLoaded();
        Assert.That(_activeUsers.IsDisplayed, Is.True, "Expected the Active users list.");

        // Captured after the assertion, not before: the point is to show the state that satisfied it.
        _evidence.Capture("Active users list");
    }

    [Then("the active users list has at least one user")]
    public void ThenTheListHasAtLeastOneUser()
    {
        // Not asserting an exact count or a specific account: the list is tenant-dependent, virtualised and
        // paged, so pinning either would fail on a tenant that is merely different rather than broken. But
        // a tenant always contains at least the account that just signed in, so an empty list is a real
        // failure — it means the grid rendered its empty state.
        Assert.That(
            _activeUsers.VisibleUserCount(),
            Is.GreaterThan(0),
            "Expected the Active users list to show at least the signed-in account.");

        TestContext.Out.WriteLine($"Active users shown: {string.Join(" | ", _activeUsers.VisibleUsers())}");
    }
}
