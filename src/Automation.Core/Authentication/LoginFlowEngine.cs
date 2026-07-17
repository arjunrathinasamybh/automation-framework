using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using Automation.Core.Configuration;
using Automation.Core.Waits;

namespace Automation.Core.Authentication;

/// <summary>
/// Drives sign-in as a state machine rather than a fixed script: until the browser reaches an
/// authenticated URL, repeatedly ask the registered handlers which screen is on display and let the
/// first match advance the flow.
/// <para>
/// This engine knows nothing about Microsoft's markup — that lives entirely in the handlers — so it is
/// reusable for any identity provider whose sign-in is a sequence of screens.
/// </para>
/// </summary>
public sealed class LoginFlowEngine : IAuthenticationService
{
    /// <summary>
    /// How many times one screen may be handled before we call it a loop. A password screen that
    /// reappears after submission means the password was rejected without a parseable error banner;
    /// retrying forever would burn the login timeout and report a misleading failure.
    /// </summary>
    private const int MaxExecutionsPerStep = 3;

    private readonly IWebDriver _driver;
    private readonly IWaitService _wait;
    private readonly IReadOnlyList<ILoginStepHandler> _handlers;
    private readonly ApplicationSettings _application;
    private readonly CredentialSettings _credentials;
    private readonly TimeoutSettings _timeouts;
    private readonly ILogger<LoginFlowEngine> _logger;

    public LoginFlowEngine(
        IWebDriver driver,
        IWaitService wait,
        IEnumerable<ILoginStepHandler> handlers,
        IOptions<ApplicationSettings> application,
        IOptions<CredentialSettings> credentials,
        IOptions<TimeoutSettings> timeouts,
        ILogger<LoginFlowEngine> logger)
    {
        _driver = driver;
        _wait = wait;
        _handlers = [.. handlers.OrderBy(handler => handler.Order)];
        _application = application.Value;
        _credentials = credentials.Value;
        _timeouts = timeouts.Value;
        _logger = logger;
    }

    public bool IsAuthenticated
    {
        get
        {
            var url = _driver.Url;

            // Guarded rather than tested directly: an unset LoginHost is the empty string, every URL
            // "contains" it, and the flow would decide it was forever on the login page and never finish.
            if (!string.IsNullOrWhiteSpace(_application.LoginHost)
                && url.Contains(_application.LoginHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _application.AuthenticatedUrlMarkers
                .Any(marker => !string.IsNullOrWhiteSpace(marker)
                    && url.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void SignIn(CredentialSettings? credentials = null, string? startUrl = null)
    {
        var context = new LoginContext(credentials ?? _credentials);
        var entryPoint = string.IsNullOrWhiteSpace(startUrl) ? _application.SignInUrl : startUrl;

        if (string.IsNullOrWhiteSpace(context.Credentials.Username))
        {
            throw new AuthenticationFailedException(
                "No username is configured. Set Credentials:Username in appsettings.json, and supply the " +
                "password via user-secrets or the Credentials__Password environment variable.");
        }

        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            throw new AuthenticationFailedException(
                "Nowhere to sign in: no startUrl was supplied and Application:SignInUrl is not configured.");
        }

        // Checked up front, because the alternative is silent. With no markers the flow can never
        // recognise that it has arrived, so a perfectly good sign-in would end in a login timeout that
        // blames the credentials.
        if (!_application.AuthenticatedUrlMarkers.Any(marker => !string.IsNullOrWhiteSpace(marker)))
        {
            throw new AuthenticationFailedException(
                "Application:AuthenticatedUrlMarkers is empty, so sign-in could never be detected as " +
                "complete. List the URL fragments that mean the application is signed in.");
        }

        _logger.LogInformation("Signing in as {Username} at {Url}.",
            context.Credentials.Username, entryPoint);

        _driver.Navigate().GoToUrl(entryPoint);

        RunFlow(context);

        _logger.LogInformation("Signed in after {Steps} step(s). Landed on {Url}.",
            context.StepsCompleted, _driver.Url);
    }

    private void RunFlow(LoginContext context)
    {
        var executionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var deadline = Stopwatch.StartNew();

        while (deadline.Elapsed < _timeouts.Login)
        {
            if (IsAuthenticated)
            {
                _wait.UntilDocumentReady();
                return;
            }

            var handler = _handlers.FirstOrDefault(candidate => candidate.IsCurrentScreen(context));

            if (handler is null)
            {
                // Nothing recognisable on screen. Usually a redirect or an interstitial spinner between
                // steps, so yield briefly and re-probe rather than failing on a transient state.
                Thread.Sleep(_timeouts.Polling);
                continue;
            }

            var count = executionCounts[handler.Name] = executionCounts.GetValueOrDefault(handler.Name) + 1;

            if (count > MaxExecutionsPerStep)
            {
                throw new AuthenticationFailedException(
                    $"Sign-in stalled: the '{handler.Name}' screen was handled {count} times without progressing. " +
                    "This usually means the credentials or TOTP secret are wrong, or the tenant is showing an " +
                    $"unhandled prompt. Current URL: {_driver.Url}");
            }

            _logger.LogDebug("Login step: {Handler} (attempt {Attempt}).", handler.Name, count);
            handler.Execute(context);
            context.StepsCompleted++;
        }

        // The deadline is only tested between steps, so a single long step can overrun it — interactive MFA
        // routinely does, because it is waiting on a person. Re-check before failing: having actually signed
        // in, only to be told sign-in timed out, would be absurd.
        if (IsAuthenticated)
        {
            _wait.UntilDocumentReady();
            return;
        }

        throw new AuthenticationFailedException(
            $"Sign-in did not complete within {_timeouts.LoginSeconds}s. " +
            $"Completed {context.StepsCompleted} step(s). Current URL: {_driver.Url}");
    }
}
