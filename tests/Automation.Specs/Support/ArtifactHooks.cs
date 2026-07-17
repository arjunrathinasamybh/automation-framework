using Automation.Core.Diagnostics;
using Automation.Core.Drivers;
using Reqnroll;

namespace Automation.Specs.Support;

/// <summary>
/// Captures evidence whenever a scenario fails. Wired here rather than left to individual steps, because a
/// failed UI scenario with no evidence is close to undebuggable.
/// </summary>
[Binding]
public sealed class ArtifactHooks
{
    private readonly ScenarioContext _scenario;
    private readonly BrowserSessionContext _session;
    private readonly IServiceProvider _services;

    public ArtifactHooks(ScenarioContext scenario, BrowserSessionContext session, IServiceProvider services)
    {
        _scenario = scenario;
        _session = session;
        _services = services;
    }

    [AfterScenario]
    public void CaptureEvidenceOnFailure()
    {
        if (_scenario.TestError is null || !_session.DriverLaunched)
        {
            // Nothing failed, or nothing was ever opened. Resolving the recorder when no browser was
            // launched would start one during teardown just to photograph a blank page.
            return;
        }

        // Resolved lazily, and only now: see above.
        var evidence = (IEvidenceRecorder)_services.GetService(typeof(IEvidenceRecorder))!;

        // Through the recorder rather than the collector, so the screenshot and DOM dump are attached to
        // the report instead of only written to disk. Evidence nobody can find from the report is evidence
        // somebody has to go digging for.
        evidence.CaptureFailure(_scenario.ScenarioInfo.Title);
    }
}
