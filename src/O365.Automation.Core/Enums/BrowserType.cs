namespace O365.Automation.Core.Enums;

/// <summary>
/// Browsers the framework can drive. Each value must have a corresponding
/// <see cref="Drivers.IBrowserDriverProvider"/> registered in the container.
/// </summary>
public enum BrowserType
{
    Chrome,
    Edge,
    Firefox
}
