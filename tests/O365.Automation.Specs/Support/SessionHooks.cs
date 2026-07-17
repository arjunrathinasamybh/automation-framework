using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.Drivers;
using O365.Automation.Core.Session;
using Reqnroll;

namespace O365.Automation.Specs.Support;

/// <summary>
/// Points each scenario's browser at the shared session profile, so the suite signs in once per run
/// rather than once per scenario.
/// <para>
/// This lives in the specs rather than the framework because it is the one part that is Reqnroll's
/// business: reading the scenario's tags and acting before the browser exists. The profile itself, and
/// everything true about it, is <see cref="SessionProfile"/>.
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

    private readonly ScenarioContext _scenario;
    private readonly BrowserSessionContext _browser;
    private readonly SessionProfile _profile;
    private readonly ILogger<SessionHooks> _logger;

    public SessionHooks(
        ScenarioContext scenario,
        BrowserSessionContext browser,
        SessionProfile profile,
        ILogger<SessionHooks> logger)
    {
        _scenario = scenario;
        _browser = browser;
        _profile = profile;
        _logger = logger;
    }

    /// <summary>
    /// Discards yesterday's profile before anything runs, so a run begins from one deliberate sign-in.
    /// <para>
    /// Static, and therefore outside the scenario container — which is why it reads configuration itself
    /// rather than being handed it. That is the price of running exactly once per run instead of once per
    /// scenario, and it is the honest way to express "once per run": a static flag guarding a
    /// per-scenario hook would only look like one until the suite is parallelised.
    /// </para>
    /// </summary>
    [BeforeTestRun]
    public static void DiscardProfileFromPreviousRun()
    {
        var configuration = AutomationConfigurationBuilder.Build(
            AppContext.BaseDirectory,
            typeof(SessionHooks).Assembly);

        var settings = configuration.GetSection(SessionSettings.SectionName).Get<SessionSettings>()
            ?? new SessionSettings();

        if (!settings.IsReused || !settings.ResetOnRunStart)
        {
            return;
        }

        using var loggerFactory = LoggerFactory.Create(logging => logging
            .AddSimpleConsole(options => options.SingleLine = true));

        new SessionProfile(Options.Create(settings), loggerFactory.CreateLogger<SessionProfile>()).Reset();
    }

    /// <summary>
    /// Runs before any step, and therefore before anything resolves <c>IWebDriver</c> — which is the whole
    /// point. The browser is launched lazily on first use, so this is the last moment the choice of
    /// profile can still be made.
    /// </summary>
    [BeforeScenario(Order = 0)]
    public void UseSharedProfile()
    {
        if (!_profile.IsEnabled)
        {
            return;
        }

        if (_scenario.ScenarioInfo.Tags.Contains(FreshSessionTag, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Scenario is tagged @{Tag}: using a clean, unauthenticated browser.", FreshSessionTag);
            return;
        }

        _browser.UseProfile(_profile.DirectoryPath);
    }
}
