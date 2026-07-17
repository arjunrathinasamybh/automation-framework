namespace O365.Automation.Core.Enums;

/// <summary>How the authenticated session is shared between scenarios.</summary>
public enum SessionMode
{
    /// <summary>
    /// Every scenario gets a clean, unauthenticated browser and signs in for itself. Total isolation, at
    /// the price of a full sign-in per scenario.
    /// </summary>
    Fresh,

    /// <summary>
    /// Sign in once per run into a persistent browser profile; later scenarios launch already
    /// authenticated.
    /// </summary>
    Reuse
}
