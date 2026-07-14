namespace O365.Automation.Pages.M365;

/// <summary>
/// Maps <see cref="M365NavigationItem"/> to the label the rail actually renders. Only entries whose
/// display text differs from the enum name need listing — everything else falls back to the name itself,
/// so adding a nav item is usually a one-line enum change.
/// </summary>
internal static class NavigationItemCatalog
{
    private static readonly IReadOnlyDictionary<M365NavigationItem, string> DisplayNameOverrides =
        new Dictionary<M365NavigationItem, string>
        {
            [M365NavigationItem.MyContent] = "My Content",
            [M365NavigationItem.PowerPoint] = "PowerPoint",
            [M365NavigationItem.OneDrive] = "OneDrive",
            [M365NavigationItem.OneNote] = "OneNote"
        };

    internal static string DisplayNameOf(M365NavigationItem item) =>
        DisplayNameOverrides.TryGetValue(item, out var displayName) ? displayName : item.ToString();
}
