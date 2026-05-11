// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostTextPresentationEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task HtmlResponse_TranslatesVirtualDocumentUri()
    {
        await VerifyUriPresentationAsync(
            input: """
                This is a Razor document.

                <div>
                    [||]
                </div>

                The end.
                """,
            text: "Hello World",
            htmlResponse: new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new()
                        {
                            DocumentUri = new(FileUri($"File1.razor{LanguageServerConstants.HtmlVirtualDocumentSuffix}"))
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), "Hello World")]
                    }
                }
            },
            expected: "Hello World");
    }

    private async Task VerifyUriPresentationAsync(string input, string text, string? expected, WorkspaceEdit? htmlResponse = null)
    {
        TestFileMarkupParser.GetSpan(input, out input, out var span);
        var document = CreateProjectAndRazorDocument(input);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var requestInvoker = new TestHtmlRequestInvoker([(VSInternalMethods.TextDocumentTextPresentationName, htmlResponse)]);

        var endpoint = new CohostTextPresentationEndpoint(IncompatibleProjectService, FilePathService, requestInvoker);

        var request = new VSInternalTextPresentationParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Range = sourceText.GetRange(span),
            Text = text
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.NotNull(result.DocumentChanges);
            Assert.Equal(expected, ((TextEdit)result.DocumentChanges.Value.First[0].Edits[0]).NewText);
            Assert.Equal(document.CreateUri(), result.DocumentChanges.Value.First[0].TextDocument.DocumentUri.GetRequiredParsedUri());
        }
    }
}
