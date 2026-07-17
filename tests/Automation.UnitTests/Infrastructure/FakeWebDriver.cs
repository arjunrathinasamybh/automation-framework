using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace Automation.UnitTests.Infrastructure;

/// <summary>
/// A browser that only knows where it is.
/// <para>
/// <c>LoginFlowEngine</c> touches exactly two things on a driver — the current URL, and navigation — which
/// is itself the point being tested: the engine reads a URL and asks handlers what they see, and knows
/// nothing else about a browser. Everything else here throws, so the day the engine starts reaching for
/// the DOM, these tests fail and say so.
/// </para>
/// <para>
/// Hand-written rather than mocked, to keep a mocking library out of a project that has managed without
/// one, and because a fake that <em>simulates a sign-in</em> reads better than a pile of setups.
/// </para>
/// </summary>
public sealed class FakeWebDriver : IWebDriver, INavigation
{
    private readonly List<string> _visited = [];

    /// <summary>Where the fake browser currently is. Handlers move it, exactly as real screens do.</summary>
    public string Url { get; set; } = "about:blank";

    /// <summary>Every URL navigated to, in order.</summary>
    public IReadOnlyList<string> Visited => _visited;

    public INavigation Navigate() => this;

    void INavigation.GoToUrl(string url)
    {
        _visited.Add(url);
        Url = url;
    }

    void INavigation.GoToUrl(Uri url) => ((INavigation)this).GoToUrl(url.ToString());

    public string Title => throw new NotSupportedException();
    public string PageSource => throw new NotSupportedException();
    public string CurrentWindowHandle => throw new NotSupportedException();
    public ReadOnlyCollection<string> WindowHandles => throw new NotSupportedException();

    public IWebElement FindElement(By by) => throw new NotSupportedException();
    public ReadOnlyCollection<IWebElement> FindElements(By by) => throw new NotSupportedException();
    public IOptions Manage() => throw new NotSupportedException();
    public ITargetLocator SwitchTo() => throw new NotSupportedException();

    void INavigation.Back() => throw new NotSupportedException();
    void INavigation.Forward() => throw new NotSupportedException();
    void INavigation.Refresh() => throw new NotSupportedException();

    Task INavigation.GoToUrlAsync(string url)
    {
        ((INavigation)this).GoToUrl(url);
        return Task.CompletedTask;
    }

    Task INavigation.GoToUrlAsync(Uri url) => ((INavigation)this).GoToUrlAsync(url.ToString());

    Task INavigation.BackAsync() => throw new NotSupportedException();
    Task INavigation.ForwardAsync() => throw new NotSupportedException();
    Task INavigation.RefreshAsync() => throw new NotSupportedException();

    public void Close() { }
    public void Quit() { }
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
