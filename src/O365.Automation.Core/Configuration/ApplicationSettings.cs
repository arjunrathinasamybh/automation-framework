namespace O365.Automation.Core.Configuration;

/// <summary>
/// Endpoints of the system under test. Kept in config so the suite can be pointed at a different
/// cloud (GCC High, 21Vianet) or at the legacy office.com host without a code change.
/// </summary>
public sealed class ApplicationSettings
{
    public const string SectionName = "Application";

    /// <summary>The authenticated landing page — where a signed-in session ends up.</summary>
    public string BaseUrl { get; set; } = "https://m365.cloud.microsoft/";

    /// <summary>
    /// Where sign-in starts.
    /// <para>
    /// Deliberately not <see cref="BaseUrl"/>: hitting that anonymously serves a public marketing page with
    /// a "Sign in" button rather than redirecting to Entra ID. The <c>?auth=2</c> parameter forces the
    /// work-or-school sign-in flow, so automation lands on the credential page directly.
    /// </para>
    /// </summary>
    public string SignInUrl { get; set; } = "https://m365.cloud.microsoft/?auth=2";

    /// <summary>Host of the Entra ID sign-in pages. Used to detect "we are still on the login flow".</summary>
    public string LoginHost { get; set; } = "login.microsoftonline.com";

    /// <summary>
    /// Substrings that, when present in the URL, mean sign-in completed and we are on the M365 landing page.
    /// </summary>
    public IList<string> AuthenticatedUrlMarkers { get; set; } =
    [
        "m365.cloud.microsoft",
        "office.com",
        "microsoft365.com"
    ];

    /// <summary>Where failure screenshots and page dumps are written.</summary>
    public string ArtifactsDirectory { get; set; } = "TestArtifacts";
}
