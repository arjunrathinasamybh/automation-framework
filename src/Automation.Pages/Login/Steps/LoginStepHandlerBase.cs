using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Automation.Core.Authentication;
using Automation.Core.Interactions;
using Automation.Core.Waits;

namespace Automation.Pages.Login.Steps;

/// <summary>
/// Shared plumbing for the Entra ID sign-in screens: resolving an element from a list of candidate
/// locators, and the two interactions every step performs (click, type).
/// </summary>
public abstract class LoginStepHandlerBase : ILoginStepHandler
{
    protected LoginStepHandlerBase(IWaitService wait, IElementInteractor interactor, ILogger logger)
    {
        Wait = wait;
        Interactor = interactor;
        Logger = logger;
    }

    protected IWaitService Wait { get; }
    protected IElementInteractor Interactor { get; }
    protected ILogger Logger { get; }

    public abstract string Name { get; }
    public abstract int Order { get; }
    public abstract bool IsCurrentScreen(LoginContext context);
    public abstract void Execute(LoginContext context);

    /// <summary>
    /// Fast, wait-free probe used by <see cref="IsCurrentScreen"/>. The engine calls this on every handler
    /// on every poll, so it must never block — a per-locator wait here would cost minutes per sign-in.
    /// </summary>
    protected bool AnyDisplayed(IReadOnlyList<By> locators) =>
        locators.Any(locator => Wait.IsDisplayed(locator));

    /// <summary>Resolves the first matching locator, waiting for it, and fails loudly if none match.</summary>
    protected IWebElement Resolve(IReadOnlyList<By> locators, string description) =>
        Wait.FirstDisplayedOrDefault(locators)
        ?? throw new AuthenticationFailedException(
            $"The '{Name}' step could not find the {description}. Microsoft may have changed the sign-in " +
            $"markup — update the candidate locators in {nameof(MicrosoftLoginLocators)}. " +
            $"Tried: {string.Join(" | ", locators)}");

    protected void ClickFirst(IReadOnlyList<By> locators, string description) =>
        Interactor.Click(Resolve(locators, description));

    protected void TypeInto(IReadOnlyList<By> locators, string text, string description) =>
        Interactor.Type(Resolve(locators, description), text);

    /// <summary>
    /// Waits for this screen's own markers to disappear, i.e. for the flow to move on.
    /// <para>
    /// A timeout here is deliberately swallowed. If the screen does not advance, the cause is almost always
    /// a rejected credential — and the error banner is on the same page, so the engine's next poll will hand
    /// control to the higher-priority error handler, which reports Microsoft's actual message. Throwing here
    /// instead would replace that precise diagnosis with a generic "element still visible" timeout.
    /// </para>
    /// </summary>
    protected void WaitForScreenToAdvance(IReadOnlyList<By> screenMarkers)
    {
        try
        {
            Wait.Until(_ => !AnyDisplayed(screenMarkers), ScreenTransitionTimeout);
        }
        catch (WebDriverTimeoutException)
        {
            Logger.LogDebug("The '{Step}' screen did not advance; deferring to the login engine.", Name);
        }
    }

    private static readonly TimeSpan ScreenTransitionTimeout = TimeSpan.FromSeconds(15);
}
