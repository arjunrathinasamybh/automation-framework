using OpenQA.Selenium;
using Automation.Pages.Internal;

namespace Automation.Pages.Admin;

/// <summary>
/// Locators for the Microsoft 365 admin center (admin.cloud.microsoft).
/// <para>
/// As everywhere else in this project, nothing keys off generated CSS classes — this navigation's are
/// hashed (<c>root-402</c>, <c>text-397</c>) and change with every rebuild Microsoft ships.
/// </para>
/// <para>
/// The navigation is an ARIA menu, not the tree its indentation suggests: every entry is
/// <c>role="menuitem"</c>, groups are buttons carrying <c>aria-expanded</c>, and their children are links
/// in a nested <c>role="menu"</c>. Each entry names itself in a <c>name</c> attribute matching its visible
/// label, which is what these locators key off. The <c>data-automation-id</c> alongside it is tempting but
/// unusable here: it is built from an internal route value, so "Active users" is
/// <c>LeftNavusersNavToggler</c> and its own parent is <c>LeftNavusersnodeNavToggler</c> — not derivable
/// from a display name.
/// </para>
/// </summary>
internal static class AdminCenterLocators
{
    /// <summary>The attribute each entry carries its label in.</summary>
    internal const string NameAttribute = "name";

    /// <summary>The admin center's left navigation. Doubles as the "the shell has rendered" marker.</summary>
    internal static readonly IReadOnlyList<By> NavigationContainer =
    [
        By.CssSelector("nav[role='navigation']"),
        By.CssSelector("[role='navigation']"),
        By.CssSelector("nav")
    ];

    /// <summary>
    /// Every entry in the navigation, including collapsed group headers. Used to enumerate what the
    /// signed-in account can actually see — the useful thing to report when an entry is missing, because
    /// on this surface that usually means the account lacks the role, not that the locator broke.
    /// </summary>
    internal static readonly IReadOnlyList<By> AllItems =
    [
        By.CssSelector("[role='navigation'] [role='menuitem'][name]"),
        By.CssSelector("nav [role='menuitem'][name]"),
        By.CssSelector("[role='navigation'] a[aria-label], [role='navigation'] button[aria-label]")
    ];

    /// <summary>
    /// Candidate locators for a single navigation entry, most-specific first. Every candidate is an exact
    /// match — nothing here uses contains(), because these labels nest: a contains() form for "Users"
    /// would just as happily resolve to "Active users" or "Guest users", and the group is the one entry
    /// whose mis-resolution would be silent (clicking the child instead of expanding the parent lands on
    /// the right page for the wrong reason).
    /// </summary>
    internal static IReadOnlyList<By> Item(string displayName)
    {
        var escaped = XPathLiteral.Of(displayName);

        return
        [
            By.CssSelector($"[role='navigation'] [role='menuitem'][name='{displayName}']"),
            By.CssSelector($"nav [role='menuitem'][name='{displayName}']"),
            By.CssSelector($"[role='navigation'] [aria-label='{displayName}']"),
            By.XPath($"//*[@role='navigation']//*[@role='menuitem'][normalize-space(.)={escaped}]"),
            By.XPath($"//nav//*[@role='menuitem'][normalize-space(.)={escaped}]")
        ];
    }
}
