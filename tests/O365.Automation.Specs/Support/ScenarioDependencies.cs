using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using O365.Automation.Core.Configuration;
using O365.Automation.Core.DependencyInjection;
using O365.Automation.Pages.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace O365.Automation.Specs.Support;

/// <summary>
/// Composition root for the specifications.
/// <para>
/// Reqnroll's dependency-injection plugin creates a container scope per scenario and disposes it
/// afterwards. That lines up exactly with how the framework registers its services: <c>IWebDriver</c> is
/// scoped, so one scenario == one browser, and the browser is quit when the scenario ends. No hook has to
/// remember to close it.
/// </para>
/// </summary>
public static class ScenarioDependencies
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var configuration = AutomationConfigurationBuilder.Build(
            AppContext.BaseDirectory,
            typeof(ScenarioDependencies).Assembly);

        var services = new ServiceCollection()
            .AddLogging(logging => logging
                .AddSimpleConsole(options => options.SingleLine = true)
                .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug))
            .AddAutomationCore(configuration)
            .AddO365Pages();

        // Shared between the steps of a single scenario.
        services.AddScoped<ScenarioState>();

        // The plugin discovers and registers the step-definition and hook classes itself; they are
        // resolved from the scenario's scope, so they can take IWebDriver and page objects as constructor
        // parameters like any other service.
        return services;
    }
}
