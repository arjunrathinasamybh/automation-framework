using Automation.Core.Authentication;
using Automation.Core.Waits;
using OpenQA.Selenium;

namespace Automation.UnitTests.Infrastructure;

/// <summary>
/// One screen of an imaginary identity provider.
/// <para>
/// A handler is only ever asked two questions — "is this your screen?" and "then deal with it" — so a fake
/// is two lambdas. That these tests can drive the engine with handlers that mention no vendor is the
/// evidence for the claim that the engine is provider-agnostic.
/// </para>
/// </summary>
public sealed class FakeLoginStepHandler : ILoginStepHandler
{
    private readonly Func<LoginContext, bool> _isCurrentScreen;
    private readonly Action<LoginContext> _execute;

    public FakeLoginStepHandler(
        string name,
        int order,
        Func<LoginContext, bool> isCurrentScreen,
        Action<LoginContext> execute)
    {
        Name = name;
        Order = order;
        _isCurrentScreen = isCurrentScreen;
        _execute = execute;
    }

    public string Name { get; }
    public int Order { get; }

    /// <summary>How many times the engine asked this handler to act.</summary>
    public int Executions { get; private set; }

    public bool IsCurrentScreen(LoginContext context) => _isCurrentScreen(context);

    public void Execute(LoginContext context)
    {
        Executions++;
        _execute(context);
    }
}

/// <summary>
/// Waits that do not wait. The engine's only use of the wait service during a flow is "let the page
/// settle", which has no meaning against a fake browser.
/// </summary>
public sealed class FakeWaitService : IWaitService
{
    public void UntilDocumentReady(TimeSpan? timeout = null) { }

    public IWebElement UntilVisible(By locator, TimeSpan? timeout = null) => throw new NotSupportedException();
    public IWebElement UntilClickable(By locator, TimeSpan? timeout = null) => throw new NotSupportedException();
    public void UntilGone(By locator, TimeSpan? timeout = null) => throw new NotSupportedException();
    public void UntilUrlContains(string fragment, TimeSpan? timeout = null) => throw new NotSupportedException();
    public TResult Until<TResult>(Func<IWebDriver, TResult> condition, TimeSpan? timeout = null) => throw new NotSupportedException();
    public bool IsDisplayed(By locator) => throw new NotSupportedException();
    public IWebElement? FirstDisplayedOrDefault(IEnumerable<By> locators, TimeSpan? timeout = null) => throw new NotSupportedException();
}
