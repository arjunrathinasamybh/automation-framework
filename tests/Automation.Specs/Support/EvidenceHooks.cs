using Microsoft.Extensions.Options;
using Automation.Core.Configuration;
using Automation.Core.Diagnostics;
using Automation.Core.Drivers;
using Reqnroll;

namespace Automation.Specs.Support;

/// <summary>
/// Photographs every step, when the run asks for it (<c>Evidence:Mode=EveryStep</c>).
/// <para>
/// A hook rather than a step concern, for the same reason failure evidence is: a scenario should not have
/// to remember. Steps ask for evidence when they have something worth showing; this is for the runs where
/// you would rather have everything than choose.
/// </para>
/// </summary>
[Binding]
public sealed class EvidenceHooks
{
    private readonly ScenarioContext _scenario;
    private readonly BrowserSessionContext _session;
    private readonly IEvidenceRecorder _evidence;
    private readonly EvidenceSettings _settings;

    public EvidenceHooks(
        ScenarioContext scenario,
        BrowserSessionContext session,
        IEvidenceRecorder evidence,
        IOptions<EvidenceSettings> settings)
    {
        _scenario = scenario;
        _session = session;
        _evidence = evidence;
        _settings = settings.Value;
    }

    [AfterStep]
    public void CaptureStep()
    {
        // No browser means nothing to photograph. Asking anyway would launch one purely to picture a blank
        // page — the same trap the failure hook avoids.
        if (!_settings.CapturesEveryStep || !_session.DriverLaunched)
        {
            return;
        }

        _evidence.Capture($"{_scenario.StepContext.StepInfo.StepDefinitionType} {_scenario.StepContext.StepInfo.Text}");
    }
}
