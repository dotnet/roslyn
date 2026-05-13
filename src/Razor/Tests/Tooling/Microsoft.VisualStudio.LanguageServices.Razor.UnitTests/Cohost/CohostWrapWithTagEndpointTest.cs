// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.VisualStudio.Razor.LanguageClient.WrapWithTag;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostWrapWithTagEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task InsideHtml()
    {
        await VerifyWrapWithTagAsync(
            input: """
                <div>
                    [||]
                </div>
                """,
            expected: """
                <div>
                    <p></p>
                </div>
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 4), length: 0),
                [LspFactory.CreateTextEdit(position: (1, 4), "<p></p>")]
            ));
    }

    [Fact]
    public async Task HtmlInCSharp()
    {
        await VerifyWrapWithTagAsync(
            input: """
                @if (true)
                {
                    [|<p></p>|]
                }
                """,
            expected: """
                @if (true)
                {
                    <div><p></p></div>
                }
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 4), length: 0),
                [
                    LspFactory.CreateTextEdit(position: (2, 4), "<div>"),
                    LspFactory.CreateTextEdit(position: (2, 11), "</div>")
                ]
            ));
    }

    [Fact]
    public async Task HtmlInCSharp_WithWhitespace()
    {
        await VerifyWrapWithTagAsync(
            input: """
                @if (true)
                {
                    [| <p></p> |]
                }
                """,
            expected: """
                @if (true)
                {
                    <div><p></p></div>
                }
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 4), length: 0),
                [
                    LspFactory.CreateTextEdit(2, 4, 2, 5, "<div>"),
                    LspFactory.CreateTextEdit(2, 12, 2, 13, "</div>")
                ]
            ));
    }

    [Fact]
    public async Task NotInHtmlInCSharp_WithNewline()
    {
        await VerifyWrapWithTagAsync(
            input: """
                @if (true)
                {[|
                    <p></p>|]
                }
                """,
            expected: null,
            htmlResponse: null);
    }

    [Fact]
    public async Task RazorBlockStart()
    {
        await VerifyWrapWithTagAsync(
            input: """
                [|@if (true) { }
                <div>
                </div>|]
                """,
            expected: """
                <div>
                    @if (true) { }
                    <div>
                    </div>
                </div>
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 4), length: 0),
                [
                    LspFactory.CreateTextEdit(position: (0, 0), $"<div>{Environment.NewLine}    "),
                    LspFactory.CreateTextEdit(position: (1, 0), "    "),
                    LspFactory.CreateTextEdit(position: (2, 0), "    "),
                    LspFactory.CreateTextEdit(position: (2, 6), $"{Environment.NewLine}</div>")
                ]
            ));
    }

    [Fact]
    public async Task NotInCodeBlock()
    {
        await VerifyWrapWithTagAsync(
            input: """
                @code {
                    [||]
                }
                """,
            htmlResponse: null,
            expected: null);
    }

    [Fact]
    public async Task ImplicitExpression()
    {
        await VerifyWrapWithTagAsync(
            input: """
                <div>
                    @[||]currentCount
                </div>
                """,
            expected: """
                <div>
                    <span>@currentCount</span>
                </div>
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 5), length: 13),
                [LspFactory.CreateTextEdit(1, 4, 1, 17, "<span>@currentCount</span>")]
            ));
    }

    [Fact]
    public async Task HtmlWithTildes()
    {
        await VerifyWrapWithTagAsync(
            input: """
                <div>
                    @[||]currentCount
                </div>
                """,
            htmlDocument: """
                <div>
                    /*~~~~~~~~~*/
                </div>
                """,
            expected: """
                <div>
                    <span>@currentCount</span>
                </div>
                """,
            htmlResponse: new VSInternalWrapWithTagResponse(
                LspFactory.CreateSingleLineRange(start: (1, 5), length: 13),
                [LspFactory.CreateTextEdit(1, 4, 1, 17, "<span>/*~~~~~~~~~*/</span>")]
            ));
    }

    private async Task VerifyWrapWithTagAsync(TestCode input, string? expected, VSInternalWrapWithTagResponse? htmlResponse, string? htmlDocument = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var requestInvoker = new TestHtmlRequestInvoker([(LanguageServerConstants.RazorWrapWithTagEndpoint, htmlResponse)]);

        var documentUri = document.CreateUri();
        var documentManager = new TestDocumentManager();
        if (htmlDocument is not null)
        {
            var snapshot = new StringTextSnapshot(htmlDocument);
            var buffer = new TestTextBuffer(snapshot);
            var htmlSnapshot = new HtmlVirtualDocumentSnapshot(documentUri, snapshot, hostDocumentSyncVersion: 1, state: null);
            var documentSnapshot = new TestLSPDocumentSnapshot(documentUri, version: 1, htmlSnapshot);
            documentManager.AddDocument(documentUri, documentSnapshot);
        }

        var endpoint = new CohostWrapWithTagEndpoint(RemoteServiceInvoker, requestInvoker, documentManager, IncompatibleProjectService, LoggerFactory);

        var request = new VSInternalWrapWithTagParams(
            sourceText.GetRange(input.Span),
            "div",
            new FormattingOptions(),
            new VersionedTextDocumentIdentifier()
            {
                DocumentUri = new(documentUri)
            });

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);

            var changedDoc = sourceText.WithChanges(result.TextEdits.Select(sourceText.GetTextChange));
            AssertEx.EqualOrDiff(expected, changedDoc.ToString());
        }
    }
}
