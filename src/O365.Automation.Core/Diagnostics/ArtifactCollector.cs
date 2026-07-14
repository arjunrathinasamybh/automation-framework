using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using O365.Automation.Core.Configuration;

namespace O365.Automation.Core.Diagnostics;

public sealed class ArtifactCollector : IArtifactCollector
{
    private readonly IWebDriver _driver;
    private readonly ApplicationSettings _application;
    private readonly ILogger<ArtifactCollector> _logger;

    public ArtifactCollector(
        IWebDriver driver,
        IOptions<ApplicationSettings> application,
        ILogger<ArtifactCollector> logger)
    {
        _driver = driver;
        _application = application.Value;
        _logger = logger;
    }

    public string? CaptureScreenshot(string testName) =>
        Capture(testName, "png", path =>
            ((ITakesScreenshot)_driver).GetScreenshot().SaveAsFile(path));

    public string? CapturePageSource(string testName) =>
        Capture(testName, "html", path => File.WriteAllText(path, _driver.PageSource));

    private string? Capture(string testName, string extension, Action<string> write)
    {
        try
        {
            var directory = Path.GetFullPath(_application.ArtifactsDirectory);
            Directory.CreateDirectory(directory);

            var fileName = $"{Sanitize(testName)}_{DateTime.Now:yyyyMMdd-HHmmss-fff}.{extension}";
            var path = Path.Combine(directory, fileName);

            write(path);
            _logger.LogInformation("Captured {Extension} artifact: {Path}", extension, path);
            return path;
        }
        catch (Exception ex)
        {
            // Never let evidence collection mask the real failure — the test's own exception is what matters.
            _logger.LogWarning(ex, "Failed to capture {Extension} artifact for {Test}.", extension, testName);
            return null;
        }
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
