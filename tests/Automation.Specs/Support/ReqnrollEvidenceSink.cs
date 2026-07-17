using Automation.Core.Diagnostics;
using Reqnroll;

namespace Automation.Specs.Support;

/// <summary>
/// Publishes captured evidence into the living documentation.
/// <para>
/// This is the only class that knows both that evidence exists and that Reqnroll exists — which is the
/// point. Reqnroll emits Cucumber Messages as the run proceeds, and an attachment becomes part of that
/// stream, so the HTML formatter renders the image inline under the step that captured it. The file on
/// disk is the same one <c>TestArtifacts/</c> gets; this is what makes it reach a reader.
/// </para>
/// </summary>
public sealed class ReqnrollEvidenceSink : IEvidenceSink
{
    private readonly IReqnrollOutputHelper _output;

    public ReqnrollEvidenceSink(IReqnrollOutputHelper output) => _output = output;

    public void Attach(string filePath, string caption)
    {
        // The caption is written first so it lands immediately above its image in the report.
        // AddAttachment carries no caption of its own — the file name is all the formatter shows — which
        // is also why the collector names the file after the caption.
        _output.WriteLine(caption);
        _output.AddAttachment(filePath);
    }
}
