using Microsoft.Extensions.Options;
using NUnit.Framework;
using OpenQA.Selenium;
using O365.Automation.Core.Authentication;
using O365.Automation.Core.Configuration;
using O365.Automation.Pages.Login;
using O365.Automation.Pages.M365;
using O365.Automation.Specs.Support;
using Reqnroll;

namespace O365.Automation.Specs.Steps;

/// <summary>
/// Steps are deliberately thin: they translate a sentence into a call on the framework and assert on the
/// result. All the automation logic — waits, retries, the sign-in state machine — lives behind
/// <see cref="IAuthenticationService"/> and the page objects, so it stays testable and reusable outside
/// Reqnroll.
/// </summary>
[Binding]
public sealed class AuthenticationSteps
{
    private readonly IWebDriver _driver;
    private readonly IAuthenticationService _authentication;
    private readonly MicrosoftLoginPage _loginPage;
    private readonly M365HomePage _homePage;
    private readonly ApplicationSettings _application;
    private readonly CredentialSettings _credentials;
    private readonly TestAccount _account;
    private readonly ScenarioState _state;

    public AuthenticationSteps(
        IWebDriver driver,
        IAuthenticationService authentication,
        MicrosoftLoginPage loginPage,
        M365HomePage homePage,
        IOptions<ApplicationSettings> application,
        IOptions<CredentialSettings> credentials,
        TestAccount account,
        ScenarioState state)
    {
        _driver = driver;
        _authentication = authentication;
        _loginPage = loginPage;
        _homePage = homePage;
        _application = application.Value;
        _credentials = credentials.Value;
        _account = account;
        _state = state;
    }

    [Given("I have not signed in")]
    public void GivenIHaveNotSignedIn()
    {
        // A fresh scenario gets a fresh browser, so there is nothing to do but assert the premise holds.
        Assert.That(_authentication.IsAuthenticated, Is.False);
    }

    [Given("I have valid Microsoft 365 credentials")]
    public void GivenIHaveValidCredentials() => _account.RequireConfigured();

    [Given("I have an incorrect password")]
    public void GivenIHaveAnIncorrectPassword()
    {
        _account.RequireConfigured();
        _state.WrongPassword = true;
    }

    [When("I open Microsoft 365")]
    public void WhenIOpenMicrosoft365()
    {
        _driver.Navigate().GoToUrl(_application.SignInUrl);
        _loginPage.WaitUntilLoaded();
    }

    [When("I sign in")]
    [Given("I am signed in to Microsoft 365")]
    public void WhenISignIn()
    {
        _account.RequireConfigured();

        _authentication.SignIn();
        _homePage.WaitUntilLoaded();
    }

    [When("I attempt to sign in")]
    public void WhenIAttemptToSignIn()
    {
        var credentials = _state.WrongPassword
            ? new CredentialSettings
            {
                Username = _credentials.Username,
                Password = "not-the-real-password",
                TotpSecret = _credentials.TotpSecret
            }
            : _credentials;

        try
        {
            _authentication.SignIn(credentials);
        }
        catch (AuthenticationFailedException exception)
        {
            // Captured rather than rethrown: the failure is the expected outcome, and the Then steps
            // assert on it.
            _state.SignInFailure = exception;
        }
    }

    [Then("I am asked to sign in")]
    public void ThenIAmAskedToSignIn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_loginPage.IsDisplayed, Is.True, "Expected the Microsoft sign-in surface.");
            Assert.That(_driver.Url, Does.Contain(_application.LoginHost));
        });
    }

    [Then("I am not signed in")]
    public void ThenIAmNotSignedIn() => Assert.That(_authentication.IsAuthenticated, Is.False);

    [Then("I land on the Microsoft 365 home page")]
    public void ThenILandOnTheHomePage()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_authentication.IsAuthenticated, Is.True, "Expected an authenticated M365 URL.");
            Assert.That(_homePage.IsDisplayed, Is.True, "Expected the M365 home page.");
        });
    }

    [Then("sign-in is rejected")]
    public void ThenSignInIsRejected() =>
        Assert.That(_state.SignInFailure, Is.Not.Null, "Expected sign-in to fail, but it succeeded.");

    [Then("the failure explains that Microsoft rejected the credentials")]
    public void ThenTheFailureExplainsWhy()
    {
        // The wording matters: an overnight failure must say "the password is wrong", not "an element
        // timed out". That distinction is the whole reason the error handler outranks the other steps.
        Assert.That(_state.SignInFailure!.Message, Does.Contain("rejected by Microsoft"));
        TestContext.Out.WriteLine(_state.SignInFailure.Message);
    }
}
