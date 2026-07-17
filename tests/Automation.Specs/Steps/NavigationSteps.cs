using NUnit.Framework;
using Automation.Pages.M365;
using Reqnroll;

namespace Automation.Specs.Steps;

[Binding]
public sealed class NavigationSteps
{
    private readonly M365HomePage _homePage;

    public NavigationSteps(M365HomePage homePage) => _homePage = homePage;

    private SidebarComponent Sidebar => _homePage.Sidebar;

    [Then("the navigation rail is shown")]
    public void ThenTheNavigationRailIsShown() =>
        Assert.That(Sidebar.IsDisplayed, Is.True, "Expected the Microsoft 365 navigation rail.");

    [Then("the navigation rail lists at least one item")]
    public void ThenTheRailListsItems()
    {
        var items = Sidebar.GetItemNames();

        // Deliberately not asserting an exact set: the rail is licence- and tenant-dependent, so pinning it
        // would fail on a tenant that is merely configured differently rather than broken.
        Assert.That(items, Is.Not.Empty, "Expected at least one navigation item.");
        TestContext.Out.WriteLine($"Navigation rail items: {string.Join(", ", items)}");
    }

    // The item arrives as its display label, so tenant-specific entries outside the M365NavigationItem
    // enum work in a scenario without any code change.
    [When("I select {string} from the navigation rail")]
    public void WhenISelectFromTheRail(string item) => Sidebar.NavigateTo(item);

    [Then("{string} is the active navigation item")]
    public void ThenIsTheActiveItem(string item) =>
        Assert.That(
            Sidebar.IsItemSelected(item),
            Is.True,
            $"Expected '{item}' to be the active item in the navigation rail after selecting it.");
}
