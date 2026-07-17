using Automation.Core.Enums;

namespace Automation.Core.Configuration;

/// <summary>
/// Whether scenarios share one signed-in session, and where it is kept.
/// <para>
/// The default is <see cref="SessionMode.Fresh"/> — the isolated, obvious behaviour. Reuse is a
/// deliberate trade a suite opts into once its sign-in cost outgrows its isolation budget, so it is the
/// application's config that turns it on, not this default.
/// </para>
/// </summary>
public sealed class SessionSettings
{
    public const string SectionName = "Session";

    public SessionMode Mode { get; set; } = SessionMode.Fresh;

    /// <summary>Where the reused profile lives. Relative paths resolve against the test output directory.</summary>
    public string ProfileDirectory { get; set; } = "SessionProfile";

    /// <summary>
    /// Whether to discard the profile at the start of each run, so a run always begins from one deliberate
    /// sign-in rather than inheriting whatever the last one left behind.
    /// </summary>
    public bool ResetOnRunStart { get; set; } = true;

    public bool IsReused => Mode == SessionMode.Reuse;
}
