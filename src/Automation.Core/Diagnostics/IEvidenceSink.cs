namespace Automation.Core.Diagnostics;

/// <summary>
/// Where captured evidence goes so a human can see it — in this suite, the living documentation report.
/// <para>
/// This exists so the core can record evidence without knowing what a report is. Writing a PNG and
/// publishing it into a Cucumber Messages stream are different jobs, and only the second one knows about
/// Reqnroll. Keeping that behind an interface is what lets the core stay usable from a console runner, a
/// background job, or a test framework that has not been written yet.
/// </para>
/// </summary>
public interface IEvidenceSink
{
    /// <summary>
    /// Publishes an already-written file to the report.
    /// </summary>
    /// <param name="filePath">Absolute path of the artifact.</param>
    /// <param name="caption">What it shows, in a reader's words.</param>
    void Attach(string filePath, string caption);
}

/// <summary>
/// The sink for a host with no report to publish to. Evidence still reaches disk; nothing is thrown.
/// <para>
/// Registered by default so that <see cref="IEvidenceRecorder"/> is always resolvable — a page object
/// asking for evidence should not have to know whether anything is listening.
/// </para>
/// </summary>
public sealed class NullEvidenceSink : IEvidenceSink
{
    public void Attach(string filePath, string caption)
    {
        // Deliberately empty.
    }
}
