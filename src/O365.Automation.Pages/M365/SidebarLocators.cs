using OpenQA.Selenium;
using O365.Automation.Pages.Internal;

namespace O365.Automation.Pages.M365;

/// <summary>
/// Locators for the Microsoft 365 left navigation rail.
/// <para>
/// The rail is a React surface with generated class names, so nothing here keys off CSS classes.
/// It targets the accessibility contract instead — role="navigation", aria-label, title — which is what
/// screen readers depend on and is therefore the most stable thing on the page.
/// </para>
/// </summary>
internal static class SidebarLocators
{
    /// <summary>The rail container. Doubles as the page-loaded marker.</summary>
    internal static readonly IReadOnlyList<By> Container =
    [
        By.CssSelector("[role='navigation']"),
        By.CssSelector("nav"),
        By.CssSelector("[data-automationid='LeftNav']")
    ];

    /// <summary>Every clickable entry in the rail. Used to enumerate what the signed-in account can see.</summary>
    internal static readonly IReadOnlyList<By> AllItems =
    [
        By.CssSelector("[role='navigation'] a[aria-label], [role='navigation'] button[aria-label]"),
        By.CssSelector("nav a[aria-label], nav button[aria-label]")
    ];

    /// <summary>
    /// Candidate locators for a single rail entry, most-specific first.
    /// The exact-match forms are tried before the contains() forms so that "Outlook" cannot accidentally
    /// resolve to a longer label that merely contains the word.
    /// </summary>
    internal static IReadOnlyList<By> Item(string displayName)
    {
        var escaped = XPathLiteral.Of(displayName);

        return
        [
            By.CssSelector($"[role='navigation'] [aria-label='{displayName}']"),
            By.CssSelector($"[role='navigation'] [title='{displayName}']"),
            By.CssSelector($"nav [aria-label='{displayName}']"),
            By.XPath($"//*[@role='navigation']//*[normalize-space(text())={escaped}]"),
            By.XPath($"//*[@role='navigation']//*[@aria-label][contains(@aria-label, {escaped})]"),
            By.XPath($"//nav//*[contains(normalize-space(.), {escaped})]")
        ];
    }
}
