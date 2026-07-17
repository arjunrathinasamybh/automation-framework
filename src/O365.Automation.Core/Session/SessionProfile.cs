using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using O365.Automation.Core.Configuration;

namespace O365.Automation.Core.Session;

/// <summary>
/// The browser profile directory that carries one signed-in session across scenarios.
/// <para>
/// The idea is ordinary — point every browser at the same profile, sign in once, and the rest launch
/// already authenticated — but it rests on two couplings that are invisible until they bite:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>It cannot be InPrivate.</b> A private window exists precisely to discard profile state at the end
/// of the session, which is the state being reused. <c>BrowserSessionContext.UseProfile</c> therefore
/// forces a normal session rather than letting the two settings contradict each other silently.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>"Stay signed in?" must be answered Yes.</b> One scenario still equals one browser *process* — only
/// the profile on disk is shared — and a non-persistent cookie dies with the process that received it.
/// Answer No and every scenario would sign in again, reusing the profile perfectly and gaining nothing.
/// <c>StaySignedInStepHandler</c> derives its answer from this setting for that reason.
/// </description>
/// </item>
/// </list>
/// <para>
/// The profile holds a live session for the test account: treat the directory as a credential. It stays
/// under the test output folder, and therefore out of the repository, by default.
/// </para>
/// </summary>
public sealed class SessionProfile
{
    private readonly SessionSettings _settings;
    private readonly ILogger<SessionProfile> _logger;

    public SessionProfile(IOptions<SessionSettings> settings, ILogger<SessionProfile> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Whether scenarios should share one signed-in session.</summary>
    public bool IsEnabled => _settings.IsReused;

    /// <summary>Absolute path of the profile directory.</summary>
    public string DirectoryPath => Path.GetFullPath(_settings.ProfileDirectory);

    /// <summary>Whether the profile should be discarded at the start of each run.</summary>
    public bool ResetOnRunStart => _settings.ResetOnRunStart;

    /// <summary>
    /// Discards the profile, so the next sign-in is a real one. Never throws: a profile that could not be
    /// deleted is a slower run or a stale session, and failing the whole suite in teardown-adjacent code
    /// would turn that into a mystery.
    /// </summary>
    public void Reset()
    {
        var path = DirectoryPath;

        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
            _logger.LogInformation("Discarded the reused session profile at {Path}.", path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                exception,
                "Could not discard the session profile at {Path}. A browser holding it open is the usual " +
                "cause. The run continues with the existing profile, so it may start already signed in.",
                path);
        }
    }
}
