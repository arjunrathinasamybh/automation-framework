namespace Automation.Pages.M365;

/// <summary>
/// The left-rail entries on the Microsoft 365 home page (m365.cloud.microsoft).
/// <para>
/// This enum exists for discoverability and compile-time safety in tests. It is not a closed set:
/// the rail is tenant- and licence-dependent, so <see cref="SidebarComponent.NavigateTo(string)"/>
/// also accepts a raw display name for anything not modelled here.
/// </para>
/// </summary>
public enum M365NavigationItem
{
    Home,
    MyContent,
    Create,
    Apps,
    Outlook,
    Teams,
    Word,
    Excel,
    PowerPoint,
    OneDrive,
    OneNote,
    Copilot
}
