namespace Automation.Core.Configuration;

/// <summary>
/// Where the application under test lives, and how the framework recognises its authenticated surfaces.
/// <para>
/// Every field here is a concept any web application has — a base address, somewhere sign-in starts, an
/// identity provider's host, the URLs that mean "signed in". None of them names a product or a vendor,
/// and none of them has a default that does: the values come from configuration, so pointing the suite at
/// a different application, a sovereign cloud, or a different identity provider is a config change rather
/// than a code change.
/// </para>
/// <para>
/// A surface belonging to one specific product does <b>not</b> belong here — it belongs to that product's
/// own settings class, beside its page objects. <c>M365Settings.AdminCenterUrl</c> is the worked example.
/// </para>
/// </summary>
public sealed class ApplicationSettings
{
    public const string SectionName = "Application";

    /// <summary>The authenticated landing page — where a signed-in session ends up.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Where sign-in starts, when a scenario does not name a surface of its own.
    /// <para>
    /// Worth knowing that this need not equal <see cref="BaseUrl"/>, and for Microsoft 365 it deliberately
    /// does not: hitting that base address anonymously serves a public marketing page with a "Sign in"
    /// button rather than redirecting to the identity provider, so the configured value carries a
    /// <c>?auth=2</c> parameter that forces the work-or-school flow. Applications without an anonymous
    /// landing page need no such persuasion — see <c>IAuthenticationService.SignIn(startUrl:)</c>.
    /// </para>
    /// </summary>
    public string SignInUrl { get; set; } = string.Empty;

    /// <summary>
    /// Host of the identity provider's sign-in pages — <c>login.microsoftonline.com</c>, a Ping or Okta
    /// tenant, whatever the application federates to. Used to detect "we are still on the login flow".
    /// Leave empty and that check is simply skipped.
    /// </summary>
    public string LoginHost { get; set; } = string.Empty;

    /// <summary>
    /// Substrings that, when present in the URL, mean sign-in completed.
    /// <para>
    /// This must cover <b>every</b> surface a sign-in may land on, not only the default landing page — a
    /// scenario may start sign-in at any of them. A surface missing from this list is the subtle failure:
    /// authentication genuinely succeeds, the flow simply never recognises it, and the scenario fails on a
    /// login timeout that says nothing about the real cause.
    /// </para>
    /// </summary>
    public IList<string> AuthenticatedUrlMarkers { get; set; } = [];

    /// <summary>Where failure screenshots and page dumps are written.</summary>
    public string ArtifactsDirectory { get; set; } = "TestArtifacts";
}
