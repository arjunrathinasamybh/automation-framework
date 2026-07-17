using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Automation.Core.Authentication;
using Automation.Core.Drivers;
using Automation.Core.Session;
using Reqnroll;

namespace Automation.Specs.Support;

/// <summary>
/// Signs the run in once, then gives each scenario its own copy of that session.
/// <para>
/// This lives in the specs rather than the framework because it is the part that is Reqnroll's business:
/// reading a scenario's tags, and acting before the browser exists. Everything true about the profile
/// itself — including why each scenario gets a copy rather than sharing one — is
/// <see cref="SessionProfile"/>.
/// </para>
/// </summary>
[Binding]
public sealed class SessionHooks
{
    /// <summary>
    /// Scenarios tagged with this always get a clean, unauthenticated browser, whatever the configured
    /// session mode. Sign-in behaviour is the obvious case: a scenario asserting that a visitor is asked
    /// to sign in cannot start from a session that already has.
    /// </summary>
    public const string FreshSessionTag = "fresh-session";

    /// <summary>
    /// Guards the one sign-in per run.
    /// <para>
    /// Static state, which is worth justifying. The stateless alternative — signing in from
    /// <c>[BeforeTestRun]</c> — would sign in unconditionally, so <c>--filter TestCategory=smoke</c>,
    /// which needs no account at all, would still stop and demand MFA. Doing it on demand means a run pays
    /// for a session only if a scenario actually wants one. It is safe because scenarios run sequentially
    /// (see AssemblyInfo), and the lock keeps it honest if that ever changes.
    /// </para>
    /// </summary>
    private static readonly Lock BootstrapGate = new();
    private static bool _masterSessionReady;

    private readonly ScenarioContext _scenario;
    private readonly BrowserSessionContext _browser;
    private readonly SessionProfile _profile;
    private readonly TestAccount _account;
    private readonly ILogger<SessionHooks> _logger;

    private string? _copyDirectory;

    public SessionHooks(
        ScenarioContext scenario,
        BrowserSessionContext browser,
        SessionProfile profile,
        TestAccount account,
        ILogger<SessionHooks> logger)
    {
        _scenario = scenario;
        _browser = browser;
        _profile = profile;
        _account = account;
        _logger = logger;
    }

    /// <summary>
    /// Runs before any step, and therefore before anything resolves <c>IWebDriver</c> — which is the whole
    /// point. The browser launches lazily on first use, so this is the last moment the choice of profile
    /// can still be made.
    /// </summary>
    [BeforeScenario(Order = 0)]
    public void UseSharedSession()
    {
        if (!_profile.IsEnabled || !_account.IsConfigured)
        {
            return;
        }

        if (_scenario.ScenarioInfo.Tags.Contains(FreshSessionTag, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Scenario is tagged @{Tag}: using a clean, unauthenticated browser.", FreshSessionTag);
            return;
        }

        EnsureMasterSession();

        _copyDirectory = _profile.CreateCopy(_scenario.ScenarioInfo.Title);
        _browser.UseProfile(_copyDirectory);
    }

    /// <summary>
    /// Discards this scenario's copy. Ordered late so it runs after the scenario's container scope is
    /// disposed — that is what quits the browser, and a profile cannot be deleted while its browser still
    /// holds it open.
    /// </summary>
    [AfterScenario(Order = 10_000)]
    public void DiscardCopy()
    {
        if (_copyDirectory is not null)
        {
            _profile.DeleteCopy(_copyDirectory);
        }
    }

    /// <summary>
    /// Signs in once, into the master profile, in a browser of its own.
    /// <para>
    /// Its own container and its own browser, deliberately: this must not touch the scenario's driver.
    /// Disposing the scope quits that browser, and quitting is what flushes the persistent cookie to disk
    /// — so the master has finished being written before any scenario copies it.
    /// </para>
    /// </summary>
    private void EnsureMasterSession()
    {
        lock (BootstrapGate)
        {
            if (_masterSessionReady)
            {
                return;
            }

            if (_profile.ResetOnRunStart)
            {
                _profile.Reset();
            }
            else if (_profile.HasMasterSession)
            {
                _logger.LogInformation("Reusing the session profile left behind by an earlier run.");
                _masterSessionReady = true;
                return;
            }

            _logger.LogInformation("Signing in once for this run; later scenarios copy the result.");

            var services = ScenarioDependencies.CreateServices();
            using var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<BrowserSessionContext>()
                    .UseProfile(_profile.MasterDirectory);

                scope.ServiceProvider.GetRequiredService<IAuthenticationService>().SignIn();
            }

            // Scope disposed, browser quit, cookies flushed: only now is the master safe to copy.
            _masterSessionReady = true;
        }
    }
}
