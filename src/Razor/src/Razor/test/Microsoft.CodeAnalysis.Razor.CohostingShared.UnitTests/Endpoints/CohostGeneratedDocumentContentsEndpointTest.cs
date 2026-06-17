// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostGeneratedDocumentContentsEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CSharpImplementation()
    {
        var input = """
                @{
                    var message = "Hello World";
                }
                <div>@message</div>
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.CSharpImplementation, """var message = "Hello World";""");
    }

    [Fact]
    public async Task CSharpDeclaration()
    {
        var input = """
                <h1>@Title</h1>

                @code {
                    private string Title { get; set; } = "Hello World";
                }
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.CSharpDeclaration, """private string Title { get; set; } = "Hello World";""");
    }

    [Fact]
    public async Task CSharpDeclaration_ReturnsNullIfDocumentDoesNotExist()
    {
        var input = """
                <h1>Hello World</h1>
                """;

        await VerifyGeneratedDocumentContentsAsync(input, GeneratedDocumentKind.CSharpDeclaration, expectedContentSubstring: null, RazorFileKind.Legacy);
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

    private async Task VerifyGeneratedDocumentContentsAsync(string input, GeneratedDocumentKind kind, string? expectedContentSubstring, RazorFileKind? fileKind = null)
    {
        var razorDocument = CreateProjectAndRazorDocument(input, fileKind);
        var endpoint = new CohostGeneratedDocumentContentsEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var request = new DocumentContentsRequest
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = razorDocument.GetURI() },
            Kind = kind
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, razorDocument, DisposalToken);

        if (expectedContentSubstring is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Contains(expectedContentSubstring, result);
        }
    }
}
