using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Automation.Core.Configuration;

namespace Automation.Core.Diagnostics;

/// <summary>
/// Writes evidence to disk via <see cref="IArtifactCollector"/>, then publishes it to the report via
/// <see cref="IEvidenceSink"/>. The policy — what gets captured at all — lives here, in one place, rather
/// than in every step that might want a picture.
/// </summary>
public sealed class EvidenceRecorder : IEvidenceRecorder
{
    private readonly IArtifactCollector _artifacts;
    private readonly IEvidenceSink _sink;
    private readonly EvidenceSettings _settings;
    private readonly ILogger<EvidenceRecorder> _logger;

    public EvidenceRecorder(
        IArtifactCollector artifacts,
        IEvidenceSink sink,
        IOptions<EvidenceSettings> settings,
        ILogger<EvidenceRecorder> logger)
    {
        _artifacts = artifacts;
        _sink = sink;
        _settings = settings.Value;
        _logger = logger;
    }

    public void Capture(string caption)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caption);

        if (!_settings.AllowsExplicitCapture)
        {
            _logger.LogDebug("Evidence:Mode is {Mode}; skipping capture of '{Caption}'.", _settings.Mode, caption);
            return;
        }

        Publish(_artifacts.CaptureScreenshot(caption), caption);
    }

    public void CaptureFailure(string scenarioName)
    {
        if (!_settings.CapturesFailures)
        {
            return;
        }

        var caption = $"Failed: {scenarioName}";
        Publish(_artifacts.CaptureScreenshot(caption), caption);

        if (_settings.CaptureDomOnFailure)
        {
            // Attached, not merely written, because the DOM is what answers "why did the locator miss?" —
            // and the person asking that is reading the report, not browsing the build agent's disk.
            Publish(_artifacts.CapturePageSource(caption), $"{caption} (DOM)");
        }
    }

    private void Publish(string? filePath, string caption)
    {
        // A null path means the collector already logged why it could not capture — a closed browser, most
        // often. Nothing more to say, and nothing worth failing over.
        if (filePath is null)
        {
            return;
        }

        try
        {
            _sink.Attach(filePath, caption);
        }
        catch (Exception exception)
        {
            // Same rule as the collector: evidence never masks the real result. A scenario that passed must
            // not fail because its screenshot could not be filed.
            _logger.LogWarning(exception, "Captured '{Caption}' but could not attach it to the report.", caption);
        }
    }
}
