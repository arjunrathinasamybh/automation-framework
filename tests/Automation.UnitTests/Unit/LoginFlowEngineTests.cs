using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Automation.Core.Authentication;
using Automation.Core.Configuration;
using Automation.UnitTests.Infrastructure;

namespace Automation.UnitTests.Unit;

/// <summary>
/// The engine, tested without a browser.
/// <para>
/// Every sign-in flow here belongs to an imaginary provider at <c>sso.example.com</c>. That is deliberate,
/// and it is the actual assertion: if the engine can drive a provider this project has never heard of, the
/// claim that Entra is a plug-in rather than a foundation is demonstrated rather than asserted. A future
/// Ping or Okta handler set needs no change here.
/// </para>
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
public sealed class LoginFlowEngineTests
{
    private const string LoginHost = "sso.example.com";
    private const string SignInUrl = "https://sso.example.com/login";
    private const string HomeUrl = "https://app.example.com/home";

    [Test]
    public void SignIn_DrivesAProviderTheFrameworkHasNeverHeardOf()
    {
        var driver = new FakeWebDriver();

        var username = Screen("Username", 10, driver, at: "/login", goesTo: "https://sso.example.com/password");
        var password = Screen("Password", 20, driver, at: "/password", goesTo: HomeUrl);

        var engine = Engine(driver, [password, username]);   // registered out of order on purpose

        engine.SignIn();

        Assert.Multiple(() =>
        {
            Assert.That(driver.Url, Is.EqualTo(HomeUrl));
            Assert.That(username.Executions, Is.EqualTo(1));
            Assert.That(password.Executions, Is.EqualTo(1));
            Assert.That(engine.IsAuthenticated, Is.True);
        });
    }

    [Test]
    public void SignIn_StartsAtTheConfiguredSignInUrl()
    {
        var driver = new FakeWebDriver();
        var engine = Engine(driver, [Screen("Username", 10, driver, at: "/login", goesTo: HomeUrl)]);

        engine.SignIn();

        Assert.That(driver.Visited[0], Is.EqualTo(SignInUrl));
    }

    [Test]
    public void SignIn_StartsAtTheSuppliedSurfaceInstead()
    {
        // The reason this exists: a scenario about one surface should not load another to get there.
        var driver = new FakeWebDriver();
        var engine = Engine(driver, [Screen("Username", 10, driver, at: "/login", goesTo: HomeUrl)]);

        engine.SignIn(startUrl: "https://app.example.com/admin");

        Assert.That(driver.Visited[0], Is.EqualTo("https://app.example.com/admin"));
    }

    [Test]
    public void SignIn_LetsTheLowerOrderHandlerWinWhenBothRecogniseTheScreen()
    {
        // The case this protects: an error banner renders *on* the screen that caused it. Without ordering,
        // the flow retypes the rejected password instead of reporting it.
        var driver = new FakeWebDriver();
        var executed = new List<string>();

        var error = new FakeLoginStepHandler("Error", 0, _ => driver.Url.Contains("/password"),
            _ => { executed.Add("Error"); throw new AuthenticationFailedException("rejected"); });
        var password = new FakeLoginStepHandler("Password", 20, _ => driver.Url.Contains("/password"),
            _ => executed.Add("Password"));

        driver.Url = "https://sso.example.com/password";
        var engine = Engine(driver, [password, error], signInUrl: "https://sso.example.com/password");

        Assert.Throws<AuthenticationFailedException>(() => engine.SignIn());
        Assert.That(executed, Is.EqualTo(new[] { "Error" }), "the error must outrank the screen it appears on");
    }

    [Test]
    public void SignIn_FailsWithADiagnosisWhenAScreenKeepsReappearing()
    {
        // A screen handled repeatedly means the flow is not progressing. Reporting that beats burning the
        // login budget and then blaming a timeout.
        var driver = new FakeWebDriver();
        var stuck = Screen("Password", 10, driver, at: "/login", goesTo: SignInUrl);   // never advances

        var exception = Assert.Throws<AuthenticationFailedException>(() => Engine(driver, [stuck]).SignIn());

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("stalled"));
            Assert.That(exception.Message, Does.Contain("Password"), "the diagnosis must name the screen");
            Assert.That(stuck.Executions, Is.LessThanOrEqualTo(4), "the loop guard must stop it, not the timeout");
        });
    }

    [Test]
    public void SignIn_FailsFastWhenNoAuthenticatedMarkersAreConfigured()
    {
        // Otherwise the silent failure: sign-in genuinely succeeds, the flow never recognises it, and the
        // scenario dies on a login timeout that blames the credentials.
        var driver = new FakeWebDriver();
        var engine = Engine(driver, [], markers: []);

        var exception = Assert.Throws<AuthenticationFailedException>(() => engine.SignIn());

        Assert.That(exception!.Message, Does.Contain("AuthenticatedUrlMarkers"));
    }

    [Test]
    public void SignIn_FailsFastWithoutAUsername()
    {
        var engine = Engine(new FakeWebDriver(), [], credentials: new CredentialSettings { Password = "p" });

        var exception = Assert.Throws<AuthenticationFailedException>(() => engine.SignIn());

        Assert.That(exception!.Message, Does.Contain("username"));
    }

    [Test]
    public void SignIn_UsesTheSuppliedCredentialsOverTheConfiguredOnes()
    {
        var driver = new FakeWebDriver();
        string? seen = null;

        var handler = new FakeLoginStepHandler("Username", 10, _ => driver.Url.Contains("/login"),
            context => { seen = context.Credentials.Username; driver.Url = HomeUrl; });

        Engine(driver, [handler]).SignIn(new CredentialSettings { Username = "other@example.com", Password = "p" });

        Assert.That(seen, Is.EqualTo("other@example.com"));
    }

    [Test]
    public void IsAuthenticated_IsFalseWhileStillOnTheIdentityProvider()
    {
        // The marker alone is not enough: a login URL can contain the application's own name.
        var driver = new FakeWebDriver { Url = "https://sso.example.com/login?redirect=app.example.com" };

        Assert.That(Engine(driver, []).IsAuthenticated, Is.False);
    }

    [Test]
    public void IsAuthenticated_IgnoresAnUnsetLoginHost()
    {
        // Every URL "contains" the empty string. Tested against directly, because the naive check makes the
        // flow decide it is permanently on the login page and wait out the timeout for reasons no log explains.
        var driver = new FakeWebDriver { Url = HomeUrl };

        Assert.That(Engine(driver, [], loginHost: string.Empty).IsAuthenticated, Is.True);
    }

    [Test]
    public void IsAuthenticated_IsFalseSomewhereUnrelated()
    {
        var driver = new FakeWebDriver { Url = "https://example.org/somewhere-else" };

        Assert.That(Engine(driver, []).IsAuthenticated, Is.False);
    }

    /// <summary>A screen that recognises itself by URL and moves the browser on when handled.</summary>
    private static FakeLoginStepHandler Screen(string name, int order, FakeWebDriver driver, string at, string goesTo) =>
        new(name, order, _ => driver.Url.Contains(at), _ => driver.Url = goesTo);

    private static ApplicationSettings Application() => new()
    {
        BaseUrl = "https://app.example.com/",
        SignInUrl = SignInUrl,
        LoginHost = LoginHost,
        AuthenticatedUrlMarkers = ["app.example.com"]
    };

    private static LoginFlowEngine Engine(
        FakeWebDriver driver,
        IEnumerable<ILoginStepHandler> handlers,
        CredentialSettings? credentials = null,
        string? signInUrl = null,
        string? loginHost = null,
        IList<string>? markers = null)
    {
        var application = Application();
        application.SignInUrl = signInUrl ?? application.SignInUrl;
        application.LoginHost = loginHost ?? application.LoginHost;
        application.AuthenticatedUrlMarkers = markers ?? application.AuthenticatedUrlMarkers;

        return new LoginFlowEngine(
            driver,
            new FakeWaitService(),
            handlers,
            Options.Create(application),
            Options.Create(credentials ?? new CredentialSettings { Username = "someone@example.com", Password = "p" }),
            // Small, so a test that exercises the timeout takes a moment rather than two minutes.
            Options.Create(new TimeoutSettings { LoginSeconds = 2, PollingMilliseconds = 10 }),
            NullLogger<LoginFlowEngine>.Instance);
    }
}
