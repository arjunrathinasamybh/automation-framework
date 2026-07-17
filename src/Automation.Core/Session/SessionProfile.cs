using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Automation.Core.Configuration;

namespace Automation.Core.Session;

/// <summary>
/// The browser profile that carries one signed-in session across scenarios.
/// <para>
/// One sign-in populates a <b>master</b> profile; every scenario then runs against its own <b>copy</b> of
/// it. The copying is not caution — it is the design, and a failure forced it:
/// </para>
/// <para>
/// Sharing the master directly, which is the obvious implementation, does not work. One scenario is still
/// one browser <i>process</i>, and Chromium will not let consecutive processes share a
/// <c>--user-data-dir</c>: the second reaches the directory before the first has released it, hands off to
/// it, and exits. The driver's connection dies with "invalid session id ... not connected to DevTools",
/// and the half-written profile left behind is worse than the crash. Every later scenario then launches
/// holding cookies, so the application attempts a <i>silent</i> SSO instead of an interactive sign-in and
/// the identity provider refuses it — "Session information is not sufficient for single-sign-on" — which
/// reaches the user as an unhelpful "we're having trouble verifying your account". Four scenarios failed
/// that way before the cause was found, and not one of them pointed at the profile.
/// </para>
/// <para>
/// A copy per scenario removes the whole class of problem: no directory is ever open twice, and a scenario
/// that dirties its profile dirties only its own. The cost is a file copy, which buys back a full sign-in
/// — and, where MFA is interactive, a human.
/// </para>
/// <para>
/// Two couplings remain, and both are requirements rather than choices:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>It cannot be InPrivate.</b> A private window discards profile state by design — the very state being
/// reused. <c>BrowserSessionContext.UseProfile</c> forces a normal session rather than letting the two
/// settings contradict each other silently.
/// </description></item>
/// <item><description>
/// <b>"Stay signed in?" must be answered Yes.</b> A non-persistent cookie dies with the process that
/// received it, so the copies would inherit a profile signed in to nothing.
/// <c>StaySignedInStepHandler</c> derives its answer from this setting for that reason.
/// </description></item>
/// </list>
/// <para>
/// The master holds a live session for the test account: treat the directory as a credential. By default
/// it sits under the test output folder, and therefore outside the repository.
/// </para>
/// </summary>
public sealed class SessionProfile
{
    /// <summary>
    /// The lock and IPC entries Chromium uses to mean "this profile is open". Copying them would tell a
    /// new browser that a profile it has never opened is already in use.
    /// </summary>
    private static readonly string[] LockEntries = ["SingletonLock", "SingletonSocket", "SingletonCookie"];

    /// <summary>
    /// Caches, crash dumps and metrics: most of the bytes and none of the meaning. Skipping them turns a
    /// ~100 MB copy into a fraction of it, once per scenario.
    /// </summary>
    private static readonly string[] DisposableDirectories =
    [
        "Cache", "Code Cache", "GPUCache", "ShaderCache", "GrShaderCache",
        "Crashpad", "BrowserMetrics", "component_crx_cache"
    ];

    private readonly SessionSettings _settings;
    private readonly ILogger<SessionProfile> _logger;

    public SessionProfile(IOptions<SessionSettings> settings, ILogger<SessionProfile> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Whether scenarios should share one signed-in session.</summary>
    public bool IsEnabled => _settings.IsReused;

    /// <summary>The profile that is signed in once, and thereafter only copied.</summary>
    public string MasterDirectory => Path.GetFullPath(_settings.ProfileDirectory);

    /// <summary>Whether the profile should be discarded at the start of each run.</summary>
    public bool ResetOnRunStart => _settings.ResetOnRunStart;

    /// <summary>
    /// Whether a master profile exists to copy from. Deliberately shallow: it answers "has a sign-in
    /// populated this?", not "is that session still valid". An expired session looks identical on disk to
    /// a live one, and the honest way to find out is to use it and let sign-in happen if it has to.
    /// </summary>
    public bool HasMasterSession =>
        Directory.Exists(MasterDirectory) && Directory.EnumerateFileSystemEntries(MasterDirectory).Any();

    /// <summary>
    /// Where per-scenario copies live — beside the master, never inside it. Copying a directory into
    /// itself does not terminate.
    /// </summary>
    private string CopiesDirectory => MasterDirectory + ".scenarios";

    /// <summary>Copies the master into a private directory for one scenario, and returns its path.</summary>
    public string CreateCopy(string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        var destination = Path.Combine(CopiesDirectory, Sanitize(scenarioName));

        DeleteDirectory(destination);
        Directory.CreateDirectory(destination);

        var copied = CopyDirectory(MasterDirectory, destination);
        _logger.LogDebug("Copied the session profile for '{Scenario}' ({Files} files).", scenarioName, copied);

        return destination;
    }

    /// <summary>Discards a scenario's copy. Never throws — a leftover profile is litter, not a failure.</summary>
    public void DeleteCopy(string directory) => DeleteDirectory(directory);

    /// <summary>
    /// Discards the master and every copy, so the next sign-in is a real one. Never throws: a profile that
    /// could not be deleted means a slower run or a stale session, and failing the suite from
    /// teardown-adjacent code would turn that into a mystery.
    /// </summary>
    public void Reset()
    {
        if (Directory.Exists(MasterDirectory))
        {
            DeleteDirectory(MasterDirectory);
            _logger.LogInformation("Discarded the session profile at {Path}.", MasterDirectory);
        }

        DeleteDirectory(CopiesDirectory);
    }

    private int CopyDirectory(string source, string destination)
    {
        var copied = 0;

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(directory);

            if (DisposableDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var target = Path.Combine(destination, name);
            Directory.CreateDirectory(target);
            copied += CopyDirectory(directory, target);
        }

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var name = Path.GetFileName(file);

            if (LockEntries.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Copy(file, Path.Combine(destination, name), overwrite: true);
                copied++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Chromium keeps journals and logs it may still be touching. They are not the session, so
                // skipping one beats failing a copy that is otherwise complete.
                _logger.LogTrace(exception, "Skipped {File} while copying the session profile.", name);
            }
        }

        return copied;
    }

    private void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                exception,
                "Could not delete the profile directory {Path}. A browser holding it open is the usual cause.",
                path);
        }
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
