using Automation.Core.Configuration;

namespace Automation.Core.Authentication;

/// <summary>
/// Handles exactly one screen of an identity provider's sign-in sequence.
/// <para>
/// The sequence is not fixed: depending on policy, account type and session state a run may see an account
/// tile, a "sign in another way" chooser, an MFA prompt, a "remember me" prompt, or none of them.
/// Modelling each screen as an independently-detectable handler — rather than scripting a fixed order —
/// means an unexpected screen is a new class, not a rewrite of the flow.
/// </para>
/// <para>
/// This contract, and the engine that drives it, name no identity provider and contain no markup. A
/// provider is a *set* of these handlers plus its own registration extension: Entra ID ships as one in
/// <c>Automation.Pages</c>, and swapping in Ping, Okta or an in-house login means writing another set
/// and registering it instead. Nothing in this project changes.
/// </para>
/// </summary>
public interface ILoginStepHandler
{
    /// <summary>Diagnostic name, surfaced in logs and in the loop-guard error message.</summary>
    string Name { get; }

    /// <summary>
    /// Priority when more than one handler matches. Lower runs first — error detection must outrank the
    /// screen it appears on, otherwise a rejected password looks like a password screen and gets retyped.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Fast, non-blocking probe: is this handler's screen the one currently rendered, and is this handler
    /// the one that should deal with it?
    /// <para>
    /// Takes the context because the answer can depend on the credentials in play, not only on the markup.
    /// The MFA screens are the case in point: whether the TOTP handler or the interactive handler owns them
    /// depends on whether a TOTP secret was supplied for <em>this</em> sign-in attempt.
    /// </para>
    /// </summary>
    bool IsCurrentScreen(LoginContext context);

    /// <summary>Advances the sign-in flow past this screen.</summary>
    void Execute(LoginContext context);
}
