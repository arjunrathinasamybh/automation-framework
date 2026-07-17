namespace Automation.Core.Authentication;

/// <summary>
/// Sign-in could not be completed. Deliberately distinct from <see cref="OpenQA.Selenium.WebDriverTimeoutException"/>
/// so a failing suite reads as "the credentials/MFA are wrong" rather than "a locator is flaky".
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}
