namespace Automation.Core.Diagnostics;

/// <summary>
/// Records what the browser looked like, into the run's report.
/// <para>
/// The point of asking for evidence through this rather than <see cref="IArtifactCollector"/> is that this
/// one honours the configured <c>Evidence:Mode</c> and publishes to the report. The collector only writes
/// files, and a file nobody opens is not evidence.
/// </para>
/// </summary>
public interface IEvidenceRecorder
{
    /// <summary>
    /// Captures the browser as it is now and attaches it to the report under <paramref name="caption"/>.
    /// <para>
    /// Does nothing unless the configured mode allows explicit capture, so a step can ask freely and the
    /// run decides whether to spend the time. Never throws: evidence must not be able to fail a scenario
    /// that was otherwise passing.
    /// </para>
    /// </summary>
    /// <param name="caption">What this shows, phrased for whoever reads the report.</param>
    void Capture(string caption);

    /// <summary>
    /// Captures the evidence a failure needs — a screenshot, and the DOM if configured. Wired into
    /// teardown, so no scenario has to remember it.
    /// </summary>
    void CaptureFailure(string scenarioName);
}
