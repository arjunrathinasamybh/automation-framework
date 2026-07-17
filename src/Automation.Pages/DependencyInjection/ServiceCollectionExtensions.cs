using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Automation.Core.Authentication;
using Automation.Pages.Admin;
using Automation.Pages.Login;
using Automation.Pages.Login.Steps;
using Automation.Pages.M365;

namespace Automation.Pages.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Entra ID (Azure AD) as the identity provider: the set of screens its sign-in can show,
    /// and the page object that models them.
    /// <para>
    /// This is the seam. The engine that drives these handlers knows no provider and contains no markup,
    /// so an application federating to Ping, Okta or an in-house login writes its own set of
    /// <see cref="ILoginStepHandler"/>s, gives it an extension method like this one, and calls that
    /// instead. Nothing in the core changes, and neither does a single line of any feature file — sign-in
    /// is environment, not behaviour, exactly as the browser is.
    /// </para>
    /// <para>
    /// The handlers are registered as an unordered collection; the engine sorts them by their declared
    /// Order. Supporting a new screen therefore means adding a class and one line here.
    /// </para>
    /// </summary>
    public static IServiceCollection AddEntraIdAuthentication(this IServiceCollection services)
    {
        services.AddScoped<ILoginStepHandler, LoginErrorStepHandler>();
        services.AddScoped<ILoginStepHandler, AccountPickerStepHandler>();
        services.AddScoped<ILoginStepHandler, UsernameStepHandler>();
        services.AddScoped<ILoginStepHandler, WorkAccountTileStepHandler>();
        services.AddScoped<ILoginStepHandler, PasswordStepHandler>();
        services.AddScoped<ILoginStepHandler, InteractiveMfaStepHandler>();
        services.AddScoped<ILoginStepHandler, VerificationMethodStepHandler>();
        services.AddScoped<ILoginStepHandler, TotpStepHandler>();
        services.AddScoped<ILoginStepHandler, StaySignedInStepHandler>();

        services.AddScoped<MicrosoftLoginPage>();

        return services;
    }

    /// <summary>
    /// Registers the Microsoft 365 page objects and the settings describing its surfaces.
    /// <para>
    /// Kept apart from <see cref="AddEntraIdAuthentication"/> on purpose: which application is under test
    /// and which identity provider guards it are two independent choices. Microsoft 365 happens to pair
    /// with Entra, but an application behind Ping would call that provider's extension and this one — or,
    /// for a different application entirely, its own equivalent of this one.
    /// </para>
    /// </summary>
    public static IServiceCollection AddO365Pages(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<M365Settings>(configuration.GetSection(M365Settings.SectionName));

        services.AddScoped<SidebarComponent>();
        services.AddScoped<M365HomePage>();

        services.AddScoped<AdminNavigationComponent>();
        services.AddScoped<AdminCenterPage>();
        services.AddScoped<ActiveUsersPage>();

        return services;
    }
}
