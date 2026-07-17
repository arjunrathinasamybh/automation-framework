using OpenQA.Selenium;

namespace Automation.Pages.Admin;

/// <summary>Locators for the admin center's Active users list.</summary>
internal static class ActiveUsersLocators
{
    /// <summary>The fragment the admin center's client-side router puts in the URL for this view.</summary>
    internal const string UrlFragment = "#/users";

    internal static readonly IReadOnlyList<By> Heading =
    [
        By.XPath("//*[@role='heading'][normalize-space()='Active users']"),
        By.XPath("//h1[normalize-space()='Active users']"),
        By.XPath("//h2[normalize-space()='Active users']")
    ];

    /// <summary>The user list itself. Doubles as the "the view has rendered" marker.</summary>
    internal static readonly IReadOnlyList<By> UserList =
    [
        By.CssSelector("[role='grid']"),
        By.CssSelector("[data-automationid='DetailsList']"),
        By.CssSelector("[role='table']")
    ];

    /// <summary>
    /// The user rows, excluding the header row. The data-item-index forms are tried first precisely
    /// because they carry that distinction; the bare role='row' fallback does not, so anything reading it
    /// has to account for the header itself.
    /// </summary>
    internal static readonly IReadOnlyList<By> UserRows =
    [
        By.CssSelector("[role='grid'] [role='row'][data-item-index]"),
        By.CssSelector("[data-automationid='DetailsList'] [role='row'][data-item-index]"),
        By.CssSelector("[role='grid'] [role='row']:not([data-automationid='DetailsHeader'])")
    ];
}
