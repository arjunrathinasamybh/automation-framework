namespace Automation.Pages.Internal;

/// <summary>Turns a runtime value into an XPath string literal.</summary>
internal static class XPathLiteral
{
    /// <summary>
    /// XPath 1.0 has no escape character, so a value containing an apostrophe — as a localised label may —
    /// cannot simply be quoted. It has to be assembled with concat() instead.
    /// </summary>
    internal static string Of(string value)
    {
        if (!value.Contains('\''))
        {
            return $"'{value}'";
        }

        var parts = value.Split('\'').Select(part => $"'{part}'");
        return $"concat({string.Join(", \"'\", ", parts)})";
    }
}
