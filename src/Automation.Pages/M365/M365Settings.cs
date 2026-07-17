namespace Automation.Pages.M365;

/// <summary>
/// Surfaces that belong to Microsoft 365 specifically.
/// <para>
/// Deliberately here rather than in <c>ApplicationSettings</c>. That class holds what every web
/// application has — a base address, a login host, the URLs that mean "signed in" — and adding a
/// Microsoft product's admin portal to it would put one product's vocabulary in the layer that is
/// supposed to know about none. The next product to be automated would add its own field beside it, and
/// the vendor-neutral core would slowly become a union of every application ever tested.
/// </para>
/// <para>
/// So the pattern is: a product brings its own settings class, its own config section, and its own DI
/// extension. Copy this file to start another one.
/// </para>
/// </summary>
public sealed class M365Settings
{
    public const string SectionName = "M365";

    /// <summary>
    /// The Microsoft 365 admin center. Reachable only by an account holding an administrative role.
    /// <para>
    /// Unlike the portal, this surface has no anonymous landing page: it redirects straight to the
    /// identity provider, which is why a scenario can sign in here directly.
    /// </para>
    /// </summary>
    public string AdminCenterUrl { get; set; } = string.Empty;
}
