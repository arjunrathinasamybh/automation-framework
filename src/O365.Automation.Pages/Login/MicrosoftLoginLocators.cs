using OpenQA.Selenium;

namespace O365.Automation.Pages.Login;

/// <summary>
/// Every selector for the Entra ID (login.microsoftonline.com) sign-in pages, in one place.
/// <para>
/// Microsoft reskins these pages without notice and A/B-tests variants across tenants, so each element
/// is expressed as an ordered list of candidate locators: a stable one first (the long-lived element id),
/// then attribute- and text-based fallbacks. Callers resolve them through
/// <see cref="Core.Waits.IWaitService.FirstDisplayedOrDefault"/>, which takes the first that matches.
/// </para>
/// <para>When the flow breaks after a Microsoft UI change, this file is the only one that should need editing.</para>
/// </summary>
internal static class MicrosoftLoginLocators
{
    // --- Step 1: account / email ---
    internal static readonly IReadOnlyList<By> UsernameInput =
    [
        By.Id("i0116"),
        By.CssSelector("input[name='loginfmt']"),
        By.CssSelector("input[type='email']")
    ];

    // Shared by most "Next"/"Sign in"/"Yes" buttons — the AAD pages reuse this id across steps,
    // which is exactly why each step must be identified by its *input*, never by its button.
    internal static readonly IReadOnlyList<By> PrimarySubmitButton =
    [
        By.Id("idSIButton9"),
        By.CssSelector("input[type='submit']"),
        By.CssSelector("button[type='submit']")
    ];

    // --- Account picker ---
    /// <summary>
    /// "Pick an account". Appears in a *normal* (non-private) browser on a domain- or Entra-joined machine,
    /// where the browser offers the Windows identity and any previously signed-in accounts via seamless SSO.
    /// </summary>
    internal static readonly IReadOnlyList<By> AccountPicker =
    [
        By.Id("tilesHolder"),
        By.CssSelector("#tilesHolder .tile-container")
    ];

    /// <summary>"Use another account" — the tile that opens the normal email prompt.</summary>
    internal static readonly IReadOnlyList<By> UseAnotherAccountTile =
    [
        By.Id("otherTileText"),
        By.Id("otherTile"),
        By.XPath("//*[@id='tilesHolder']//*[contains(normalize-space(.), 'Use another account')]")
    ];

    // --- Account type chooser: shown when the address exists as both a personal and a work account ---
    internal static readonly IReadOnlyList<By> WorkAccountTile =
    [
        By.Id("aadTile"),
        By.CssSelector("[data-test-id='aadTile']")
    ];

    // --- Step 2: password ---
    internal static readonly IReadOnlyList<By> PasswordInput =
    [
        By.Id("i0118"),
        By.CssSelector("input[name='passwd']"),
        By.CssSelector("input[type='password']")
    ];

    // --- Step 3: MFA ---
    internal static readonly IReadOnlyList<By> TotpInput =
    [
        By.Id("idTxtBx_SAOTCC_OTC"),
        By.CssSelector("input[name='otc']"),
        By.CssSelector("input[data-testid='otc-input']")
    ];

    internal static readonly IReadOnlyList<By> TotpSubmitButton =
    [
        By.Id("idSubmit_SAOTCC_Continue"),
        By.CssSelector("input[type='submit']"),
        By.CssSelector("button[type='submit']")
    ];

    /// <summary>The push-notification / number-matching screen, which cannot be satisfied without a phone.</summary>
    internal static readonly IReadOnlyList<By> PushApprovalPrompt =
    [
        By.Id("idRichContext_DisplaySign"),
        By.Id("idDiv_RemoteNGC_PollingDescription")
    ];

    /// <summary>The number the operator must tap in the Authenticator app during number matching.</summary>
    internal static readonly IReadOnlyList<By> NumberMatchingDigits =
    [
        By.Id("idRichContext_DisplaySign")
    ];

    /// <summary>"I can't use my Microsoft Authenticator app right now" — escapes push approval to a code prompt.</summary>
    internal static readonly IReadOnlyList<By> SignInAnotherWayLink =
    [
        By.Id("signInAnotherWay"),
        By.CssSelector("a#signInAnotherWay")
    ];

    /// <summary>The list of alternative verification methods shown after "sign in another way".</summary>
    internal static readonly IReadOnlyList<By> VerificationMethodList =
    [
        By.Id("idDiv_SAOTCS_Proofs"),
        By.CssSelector("[data-testid='proof-list']")
    ];

    /// <summary>The "Use a verification code" option within that list, matched on its wording.</summary>
    internal static readonly IReadOnlyList<By> VerificationCodeOption =
    [
        By.XPath("//div[@id='idDiv_SAOTCS_Proofs']//div[contains(., 'verification code')]"),
        By.XPath("//*[contains(text(), 'Use a verification code')]")
    ];

    // --- Step 4: "Stay signed in?" (KMSI) ---
    internal static readonly IReadOnlyList<By> StaySignedInPrompt =
    [
        By.Id("KmsiCheckboxField"),
        By.Id("idSIButton9_kmsi")
    ];

    internal static readonly IReadOnlyList<By> StaySignedInNoButton =
    [
        By.Id("idBtn_Back")
    ];

    // --- Errors ---
    /// <summary>
    /// Error banners across all steps. Checked before every other handler: a rejected password renders
    /// the password screen *plus* an error, and without this the flow would simply retype the password.
    /// </summary>
    internal static readonly IReadOnlyList<By> ErrorMessage =
    [
        By.Id("passwordError"),
        By.Id("usernameError"),
        By.Id("idSpan_SAOTCC_Error_OTC"),
        By.CssSelector("#loginHeader + .alert-error"),
        By.CssSelector("[role='alert'].alert-error")
    ];

    /// <summary>
    /// Every screen that constitutes an MFA challenge.
    /// <para>
    /// Interactive mode waits for <em>all</em> of these to clear rather than for one specific prompt,
    /// because the operator may be asked for a code, a push approval, an SMS or a security key — and which
    /// one appears is the tenant's choice, not ours. Waiting on the whole set is what lets interactive mode
    /// support every authenticator type without modelling each individually.
    /// </para>
    /// <para>Declared last: it composes the lists above, and static initialisers run in textual order.</para>
    /// </summary>
    internal static readonly IReadOnlyList<By> AnyMfaChallenge =
    [
        .. TotpInput,
        .. PushApprovalPrompt,
        .. VerificationMethodList
    ];
}
