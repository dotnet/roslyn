// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.LinkedEditingRange;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostLinkedEditingRangeEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("$$PageTitle", "PageTitle")]
    [InlineData("Page$$Title", "PageTitle")]
    [InlineData("PageTitle$$", "PageTitle")]
    [InlineData("PageTitle", "$$PageTitle")]
    [InlineData("PageTitle", "Page$$Title")]
    [InlineData("PageTitle", "PageTitle$$")]
    public async Task Component_StartAndEndTag(string startTag, string endTag)
    {
        var input = $"""
            This is a Razor document.

            <[|{startTag}|]>This is the title</[|{endTag}|]>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Theory]
    [InlineData("$$div")]
    [InlineData("di$$v")]
    [InlineData("div$$")]
    public async Task Html_StartTag(string startTagAndCursorLocation)
    {
        var input = $"""
            This is a Razor document.

            <[|{startTagAndCursorLocation}|]>
                Here is some content.
            </[|div|]>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Theory]
    [InlineData("$$div")]
    [InlineData("di$$v")]
    [InlineData("div$$")]
    public async Task Html_EndTag(string endTagAndCursorLocation)
    {
        var input = $"""
            This is a Razor document.

            <[|div|]>
                Here is some content.
            </[|{endTagAndCursorLocation}|]>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Fact]
    public async Task Html_EndTag_BeforeSlash()
    {
        var input = $"""
            This is a Razor document.

            <div>
                Here is some content.
            <$$/div>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Fact]
    public async Task Html_NotATag()
    {
        var input = $"""
            This is a $$Razor document.

            <div>
                Here is some content.
            </div>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Fact]
    public async Task Html_NestedTags_Outer()
    {
        var input = $"""
            This is a Razor document.

            <[|d$$iv|]>
                <div>
                    Here is some content.
                </div>
            </[|div|]>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Fact]
    public async Task Html_NestedTags_Inner()
    {
        var input = $"""
            This is a Razor document.

            <div>
                <[|d$$iv|]>
                    Here is some content.
                </[|div|]>
            </div>

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    [Fact]
    public async Task Html_SelfClosingTag()
    {
        var input = $"""
            This is a Razor document.

            <b$$r />
            Here is some content.

            The end.
            """;

        await VerifyLinkedEditingRangeAsync(input);
    }

    private async Task VerifyLinkedEditingRangeAsync(string input)
    {
        TestFileMarkupParser.GetPositionAndSpans(input, out input, out int cursorPosition, out ImmutableArray<TextSpan> spans);
        var document = CreateProjectAndRazorDocument(input);
        var sourceText = await document.GetTextAsync(DisposalToken);

        var endpoint = new CohostLinkedEditingRangeEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var request = new LinkedEditingRangeParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(cursorPosition)
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (spans.Length == 0)
        {
            Assert.Null(result);
            return;
        }

        Assert.NotNull(result);
        Assert.Equal(LinkedEditingRangeHelper.WordPattern, result.WordPattern);
        Assert.Equal(spans[0], sourceText.GetTextSpan(result.Ranges[0]));
        Assert.Equal(spans[1], sourceText.GetTextSpan(result.Ranges[1]));
    }
}
