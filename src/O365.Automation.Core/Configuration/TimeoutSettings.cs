namespace O365.Automation.Core.Configuration;

/// <summary>
/// Central timeout policy. Nothing in the framework should hard-code a wait duration —
/// inject this instead so timings can be tuned per environment (a slow CI agent needs longer than a dev box).
/// </summary>
public sealed class TimeoutSettings
{
    public const string SectionName = "Timeouts";

    /// <summary>Default explicit wait for an element to appear or become interactable.</summary>
    public int ElementSeconds { get; set; } = 30;

    /// <summary>Wait for a full page load / navigation to settle.</summary>
    public int PageLoadSeconds { get; set; } = 60;

    /// <summary>
    /// Budget for the whole Entra ID sign-in sequence (email → password → MFA → KMSI).
    /// Generous, because interactive MFA screens and tenant redirects are slow.
    /// </summary>
    public int LoginSeconds { get; set; } = 120;

    /// <summary>Poll interval used by explicit waits.</summary>
    public int PollingMilliseconds { get; set; } = 500;

    public TimeSpan Element => TimeSpan.FromSeconds(ElementSeconds);
    public TimeSpan PageLoad => TimeSpan.FromSeconds(PageLoadSeconds);
    public TimeSpan Login => TimeSpan.FromSeconds(LoginSeconds);
    public TimeSpan Polling => TimeSpan.FromMilliseconds(PollingMilliseconds);
}
