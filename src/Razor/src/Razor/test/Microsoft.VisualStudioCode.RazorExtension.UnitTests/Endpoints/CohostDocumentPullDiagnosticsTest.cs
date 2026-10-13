// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentPullDiagnosticsTest
{
    [Fact]
    public async Task CSharpUnusedUsings_HintDiagnosticsInVSCode()
    {
        var document = CreateProjectAndRazorDocument("""
            @using System
            @using System.Text

            <div></div>

            @code
            {
                public void BuildsStrings(StringBuilder b)
                {
                }
            }
            """);

        var requestInvoker = new TestHtmlRequestInvoker();
        var result = await MakeDiagnosticsRequestAsync(document, taskListRequest: false, requestInvoker, IncompatibleProjectService, RemoteServiceInvoker, ClientCapabilitiesService, LoggerFactory, DisposalToken);

        Assert.NotNull(result);
        var diagnostic = Assert.Single(result);
        Assert.Equal(0, diagnostic.Range.Start.Line);
        Assert.Equal(0, diagnostic.Range.End.Line);
        Assert.Equal("RZ0005", diagnostic.Code.AssumeNotNull().Second);
        Assert.Equal(LspDiagnosticSeverity.Hint, diagnostic.Severity);

        var tags = Assert.IsType<DiagnosticTag[]>(diagnostic.Tags);
        Assert.Contains(tags, tag => tag == DiagnosticTag.Unnecessary);
    }

    [Fact]
    public async Task DiagnosticMetadata_InVSCode()
    {
        var result = await VerifyDiagnosticsAsync("""
            @{|CS0103:CallMeMaybe|}()
            """);

        Assert.False(Assert.Single(result) is VSDiagnostic { Identifier: not null });
    }
}
