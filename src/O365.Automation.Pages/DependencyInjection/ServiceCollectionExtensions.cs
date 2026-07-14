using Microsoft.Extensions.DependencyInjection;
using O365.Automation.Core.Authentication;
using O365.Automation.Pages.Login;
using O365.Automation.Pages.Login.Steps;
using O365.Automation.Pages.M365;

namespace O365.Automation.Pages.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Microsoft 365 page objects and the sign-in step handlers.
    /// <para>
    /// The handlers are registered as an unordered collection of <see cref="ILoginStepHandler"/>; the login
    /// engine sorts them by their declared Order. Supporting a new sign-in screen therefore means adding a
    /// handler class and one line here — no existing code changes.
    /// </para>
    /// </summary>
    public static IServiceCollection AddO365Pages(this IServiceCollection services)
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
        services.AddScoped<SidebarComponent>();
        services.AddScoped<M365HomePage>();

        return services;
    }
}
