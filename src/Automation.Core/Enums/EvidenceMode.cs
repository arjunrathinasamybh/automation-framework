namespace Automation.Core.Enums;

/// <summary>
/// How much evidence a run records into the report. A ladder: each mode does everything the one above it
/// does, and a little more.
/// </summary>
public enum EvidenceMode
{
    /// <summary>Nothing is captured, not even on failure. For a run where the browser is not worth photographing.</summary>
    Off,

    /// <summary>Only failures are captured — a screenshot and a DOM dump, automatically.</summary>
    OnFailure,

    /// <summary>
    /// Failures, plus whatever a step or page object explicitly asks for. The report then reads as a
    /// narrative someone chose, rather than a contact sheet.
    /// </summary>
    Explicit,

    /// <summary>
    /// Everything: a screenshot after every step, on top of the above. Nothing is ever missing, at the
    /// price of a large report and a capture's worth of time per step.
    /// </summary>
    EveryStep
}
