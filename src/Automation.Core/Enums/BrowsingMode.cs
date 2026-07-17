namespace Automation.Core.Enums;

/// <summary>
/// Privacy mode the browser session is launched in.
/// </summary>
public enum BrowsingMode
{
    /// <summary>Standard session. Uses a throwaway profile unless a user data directory is configured.</summary>
    Normal,

    /// <summary>Private session: Chrome incognito, Edge InPrivate, Firefox private browsing.</summary>
    InPrivate
}
