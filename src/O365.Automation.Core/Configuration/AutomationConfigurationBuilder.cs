using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace O365.Automation.Core.Configuration;

/// <summary>
/// Assembles the configuration from the <c>Configuration/</c> folder.
/// <para>
/// The settings are split into one file per concern rather than a single appsettings.json, because they
/// have genuinely different lifecycles: the URLs and timeouts are reviewed and committed, the browser
/// choice is churned locally and overridden per pipeline, and the credentials must never be committed at
/// all. Separate files let .gitignore draw that line precisely.
/// </para>
/// </summary>
public static class AutomationConfigurationBuilder
{
    /// <summary>Folder, relative to the test output directory, holding the configuration files.</summary>
    public const string ConfigurationFolder = "Configuration";

    /// <summary>
    /// Builds the configuration. Later sources override earlier ones, so the order is deliberate:
    /// committed defaults → machine-local overrides → secrets → environment.
    /// </summary>
    /// <param name="basePath">Usually the test output directory.</param>
    /// <param name="userSecretsAssembly">Assembly carrying the UserSecretsId, if user-secrets are in play.</param>
    public static IConfigurationRoot Build(string basePath, Assembly? userSecretsAssembly = null)
    {
        var builder = new ConfigurationBuilder().SetBasePath(basePath);

        // Committed, reviewed, environment-agnostic.
        AddFile(builder, "application.json", optional: false);
        AddFile(builder, "browser.json", optional: false);
        AddFile(builder, "timeouts.json", optional: false);
        AddFile(builder, "mfa.json", optional: false);

        // Git-ignored. Holds the username; the password and TOTP secret may be put here for local
        // convenience, but user-secrets or environment variables are the safer home for them.
        AddFile(builder, "credentials.json", optional: true);

        // Git-ignored catch-all: override any section on one machine without touching a tracked file.
        AddFile(builder, "local.json", optional: true);

        // Stored outside the repository entirely — the right place for secrets on a developer machine.
        if (userSecretsAssembly is not null)
        {
            builder.AddUserSecrets(userSecretsAssembly, optional: true);
        }

        // Highest precedence: how CI injects secrets and how a pipeline drives a browser matrix,
        // e.g. Browser__Type=Chrome, Credentials__Password=***
        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    private static void AddFile(IConfigurationBuilder builder, string fileName, bool optional) =>
        builder.AddJsonFile(Path.Combine(ConfigurationFolder, fileName), optional, reloadOnChange: false);
}
