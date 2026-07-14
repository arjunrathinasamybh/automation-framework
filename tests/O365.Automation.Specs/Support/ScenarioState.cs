using O365.Automation.Core.Authentication;

namespace O365.Automation.Specs.Support;

/// <summary>
/// State shared between the steps of one scenario.
/// <para>
/// Scoped to the scenario by the container, which is why it is a typed class rather than Reqnroll's
/// untyped <c>ScenarioContext</c> dictionary: a mistyped key in a dictionary fails at run time, whereas a
/// missing property here fails at compile time.
/// </para>
/// </summary>
public sealed class ScenarioState
{
    /// <summary>Set by "Given I have an incorrect password", read by "When I attempt to sign in".</summary>
    public bool WrongPassword { get; set; }

    /// <summary>The sign-in failure captured by "When I attempt to sign in", for a Then step to assert on.</summary>
    public AuthenticationFailedException? SignInFailure { get; set; }
}
