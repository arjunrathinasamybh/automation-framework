using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using Automation.Core.Authentication;
using Automation.Core.Configuration;
using Automation.Core.Diagnostics;
using Automation.Core.Drivers;
using Automation.Core.Interactions;
using Automation.Core.Session;
using Automation.Core.Waits;

namespace Automation.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the browser, wait, authentication and diagnostics infrastructure.
    /// <para>
    /// Lifetimes: everything that touches a browser is <b>scoped</b>, and one DI scope == one browser
    /// session == one test. Disposing the scope quits the driver, so a leaked browser process requires
    /// a leaked scope rather than a forgotten teardown.
    /// </para>
    /// </summary>
    public static IServiceCollection AddAutomationCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BrowserSettings>(configuration.GetSection(BrowserSettings.SectionName));
        services.Configure<TimeoutSettings>(configuration.GetSection(TimeoutSettings.SectionName));
        services.Configure<CredentialSettings>(configuration.GetSection(CredentialSettings.SectionName));
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));
        services.Configure<MfaSettings>(configuration.GetSection(MfaSettings.SectionName));
        services.Configure<SessionSettings>(configuration.GetSection(SessionSettings.SectionName));

        // One provider per browser. The factory picks between them; nothing else needs to know they exist.
        services.AddSingleton<IBrowserDriverProvider, ChromeDriverProvider>();
        services.AddSingleton<IBrowserDriverProvider, EdgeDriverProvider>();
        services.AddSingleton<IBrowserDriverProvider, FirefoxDriverProvider>();
        services.AddSingleton<IWebDriverFactory, WebDriverFactory>();

        services.AddScoped(sp => new BrowserSessionContext(sp.GetRequiredService<IOptions<BrowserSettings>>().Value));

        // Resolved lazily: the browser launches on first use, so a test that overrides the browser choice
        // on BrowserSessionContext still gets the browser it asked for.
        services.AddScoped<IWebDriver>(sp =>
        {
            var session = sp.GetRequiredService<BrowserSessionContext>();
            var driver = sp.GetRequiredService<IWebDriverFactory>().Create(session.Settings);
            session.MarkDriverLaunched();
            return driver;
        });

        // Stateless — it derives the profile path from configuration and owns nothing — so the lifetime
        // carries no meaning here beyond avoiding pointless allocation.
        services.AddSingleton<SessionProfile>();

        services.AddScoped<IWaitService, WaitService>();
        services.AddScoped<IElementInteractor, ElementInteractor>();
        services.AddScoped<IArtifactCollector, ArtifactCollector>();
        services.AddScoped<IAuthenticationService, LoginFlowEngine>();
        services.AddSingleton<ITotpProvider, TotpProvider>();

        return services;
    }
}
