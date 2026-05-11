// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public sealed class CohostSelectionRangeEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task SelectionRanges_CSharpMethodBody()
        => VerifySelectionRangesAsync(
            """
            @code {
                [|void M()
                [|{
                    [|[|var [|x [|= [|[|{|caret:|}1|] + 2|]|]|]|];|]
                }|]|]
            }
            """);

    [Fact]
    public async Task SelectionRanges_MultiplePositions()
    {
        TestCode input =
            """
            @code {
                void M()
                {
                    var x = {|caret:|}1;
                    var y = {|caret:|}2;
                }
            }
            """;

        var results = await GetSelectionRangesAsync(input);

        Assert.NotNull(results);
        Assert.Equal(2, results!.Length);
        AssertRangeChainIsNestedCorrectly(results[0]);
        AssertRangeChainIsNestedCorrectly(results[1]);
    }

    [Fact]
    public async Task SelectionRanges_HtmlReturnsNull()
    {
        TestCode input =
            """
            <div>He{|caret:|}llo</div>
            """;

        var results = await GetSelectionRangesAsync(input);

        Assert.Null(results);
    }

    private async Task VerifySelectionRangesAsync(TestCode input)
    {
        var results = await GetSelectionRangesAsync(input);
        Assert.NotNull(results);
        var result = Assert.Single(results!);

        var chain = new List<LspRange>();
        for (var current = result; current is not null; current = current.Parent)
        {
            chain.Add(current.Range);
        }

        var sourceText = SourceText.From(input.Text);
        var expected = input.Spans
            .Select(sourceText.GetRange)
            .ToList();

        Assert.Equal(expected, chain);
    }

    private async Task<SelectionRange[]?> GetSelectionRangesAsync(TestCode input)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = await document.GetTextAsync(DisposalToken);
        var positions = input.NamedSpans["caret"].Select(c => sourceText.GetPosition(c.Start)).ToArray();

        var endpoint = new CohostSelectionRangeEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        return await endpoint.GetTestAccessor().HandleRequestAsync(document, positions, DisposalToken);
    }

    private static void AssertRangeChainIsNestedCorrectly(SelectionRange selectionRange)
    {
        var current = selectionRange;
        while (current.Parent is not null)
        {
            Assert.True(
                ContainsOrEquals(current.Parent.Range, current.Range),
                $"Parent range {current.Parent.Range} should contain child range {current.Range}");
            current = current.Parent;
        }
    }

    private static bool ContainsOrEquals(LspRange outer, LspRange inner)
    {
        var outerStart = (outer.Start.Line, outer.Start.Character);
        var outerEnd = (outer.End.Line, outer.End.Character);
        var innerStart = (inner.Start.Line, inner.Start.Character);
        var innerEnd = (inner.End.Line, inner.End.Character);

        return outerStart.CompareTo(innerStart) <= 0 && outerEnd.CompareTo(innerEnd) >= 0;
    }
}
