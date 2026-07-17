using Automation.Core.Diagnostics;
using Automation.Core.Drivers;
using Reqnroll;

namespace Automation.Specs.Support;

/// <summary>
/// Captures a screenshot and a DOM dump whenever a scenario fails. Wired here rather than left to
/// individual steps, because a failed UI scenario with no evidence is close to undebuggable.
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
            // Nothing failed, or nothing was ever opened. Resolving the collector when no browser was
            // launched would start one during teardown just to photograph a blank page.
            return;
        }

        // Resolved lazily, and only now: see above.
        var artifacts = (IArtifactCollector)_services.GetService(typeof(IArtifactCollector))!;
        var name = _scenario.ScenarioInfo.Title;

        artifacts.CaptureScreenshot(name);
        artifacts.CapturePageSource(name);
    }
}
