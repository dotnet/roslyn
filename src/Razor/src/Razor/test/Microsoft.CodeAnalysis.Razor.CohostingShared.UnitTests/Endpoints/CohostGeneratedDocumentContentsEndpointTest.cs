// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostGeneratedDocumentContentsEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CSharp()
    {
        var input = """
                @{
                    var message = "Hello World";
                }
                <div>@message</div>
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.CSharp, """var message = "Hello World";""");
    }

    [Fact]
    public async Task Html()
    {
        var input = """
                @{
                    var message = "Hello World";
                }
                <div>@message</div>
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.Html, "<div>/*~~~~*/</div>");
    }

    [Fact]
    public async Task Formatting()
    {
        var input = """
                <div>@message</div>

                @code{
                    private string message = "Hello World";
                }
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.Formatting, "class @code{");
    }

    private async Task VerifyGeneratedDocumentContentsAsync(string input, GeneratedDocumentKind kind, string expectedContentSubstring)
    {
        var razorDocument = CreateProjectAndRazorDocument(input);
        var endpoint = new CohostGeneratedDocumentContentsEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var request = new DocumentContentsRequest
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = razorDocument.CreateDocumentUri() },
            Kind = kind
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, razorDocument, DisposalToken);

        Assert.NotNull(result);
        Assert.Contains(expectedContentSubstring, result);
    }
}
