using Automation.Core.Enums;

namespace Automation.Core.Configuration;

/// <summary>What a run records into its report, and how much of it.</summary>
public sealed class EvidenceSettings
{
    public const string SectionName = "Evidence";

    public EvidenceMode Mode { get; set; } = EvidenceMode.Explicit;

    /// <summary>
    /// Whether a failure also dumps the DOM beside its screenshot. Kept separate from <see cref="Mode"/>
    /// because the two answer different questions: a screenshot shows what a person would have seen, a DOM
    /// dump shows why the locator disagreed. The second is what you need when markup changed underneath
    /// you, and it is worth having even in runs that capture nothing else.
    /// </summary>
    public bool CaptureDomOnFailure { get; set; } = true;

    /// <summary>Whether explicit <c>Capture</c> calls are honoured at all.</summary>
    public bool AllowsExplicitCapture => Mode is EvidenceMode.Explicit or EvidenceMode.EveryStep;

    /// <summary>Whether every step is photographed automatically.</summary>
    public bool CapturesEveryStep => Mode is EvidenceMode.EveryStep;

    /// <summary>Whether a failing scenario records evidence.</summary>
    public bool CapturesFailures => Mode is not EvidenceMode.Off;
}
