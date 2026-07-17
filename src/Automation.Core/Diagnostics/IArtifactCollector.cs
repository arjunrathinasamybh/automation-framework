namespace Automation.Core.Diagnostics;

/// <summary>
/// Captures evidence when a test fails. A failed UI test with no screenshot is close to undebuggable,
/// so this is wired into the test teardown rather than left to individual tests to remember.
/// </summary>
public interface IArtifactCollector
{
    /// <summary>Saves a PNG screenshot. Returns the path written, or null if capture failed.</summary>
    string? CaptureScreenshot(string testName);

    /// <summary>Saves the rendered DOM. Returns the path written, or null if capture failed.</summary>
    string? CapturePageSource(string testName);
}
