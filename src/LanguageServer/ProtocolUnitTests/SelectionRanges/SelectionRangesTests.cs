// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SelectionRanges;

public sealed class SelectionRangesTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestGetSelectionRangeAsync_MethodBody(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void M()
                {
                    var x = {|caret:|}1 + 2;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await RunGetSelectionRangeAsync(testLspServer, caret);

        // Verify that selection ranges form a proper nesting hierarchy from the caret position outward.
        Assert.NotNull(result);
        AssertRangeChainIsNestedCorrectly(result);
    }

    [Theory, CombinatorialData]
    public async Task TestGetSelectionRangeAsync_VerifiesExpectedSpans(bool mutatingLspWorkspace)
    {
        // The markup annotates key expected spans in the selection range chain.
        // Starting at the caret inside literal '1', the chain should expand through
        // the binary expression, the local variable statement, the method, and the compilation unit.
        var markup =
            """
            {|compilationUnit:class C
            {
                {|method:void M()
                {
                    {|statement:var x = {|binary:{|caret:|}1 + 2|};|}
                }|}
            }|}
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await RunGetSelectionRangeAsync(testLspServer, caret);
        Assert.NotNull(result);

        // Collect all ranges in the chain from innermost to outermost.
        var chain = new List<LSP.Range>();
        for (var current = result; current is not null; current = current.Parent)
            chain.Add(current.Range);

        // Verify that each annotated span appears in the selection range chain.
        Assert.Contains(testLspServer.GetLocations("binary").Single().Range, chain);
        Assert.Contains(testLspServer.GetLocations("statement").Single().Range, chain);
        Assert.Contains(testLspServer.GetLocations("method").Single().Range, chain);
        Assert.Contains(testLspServer.GetLocations("compilationUnit").Single().Range, chain);
    }

    [Theory, CombinatorialData]
    public async Task TestGetSelectionRangeAsync_MultiplePositions(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void M()
                {
                    var x = {|caret1:|}1;
                    var y = {|caret2:|}2;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caret1 = testLspServer.GetLocations("caret1").Single();
        var caret2 = testLspServer.GetLocations("caret2").Single();

        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var request = new LSP.SelectionRangeParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
            Positions = [caret1.Range.Start, caret2.Range.Start]
        };

        var results = await testLspServer.ExecuteRequestAsync<LSP.SelectionRangeParams, LSP.SelectionRange[]>(
            LSP.Methods.TextDocumentSelectionRangeName, request, CancellationToken.None);

        Assert.NotNull(results);
        Assert.Equal(2, results.Length);
        AssertRangeChainIsNestedCorrectly(results[0]);
        AssertRangeChainIsNestedCorrectly(results[1]);
    }

    [Theory, CombinatorialData]
    public async Task TestGetSelectionRangeAsync_ClassDeclaration(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|caret:|}class C
            {
                void M() { }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await RunGetSelectionRangeAsync(testLspServer, caret);

        Assert.NotNull(result);
        AssertRangeChainIsNestedCorrectly(result);
    }

    private static async Task<LSP.SelectionRange?> RunGetSelectionRangeAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var request = new LSP.SelectionRangeParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
            Positions = [caret.Range.Start]
        };

        var results = await testLspServer.ExecuteRequestAsync<LSP.SelectionRangeParams, LSP.SelectionRange[]>(
            LSP.Methods.TextDocumentSelectionRangeName, request, CancellationToken.None);

        return results?.FirstOrDefault();
    }

    private static void AssertRangeChainIsNestedCorrectly(LSP.SelectionRange selectionRange)
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

    private static bool ContainsOrEquals(LSP.Range outer, LSP.Range inner)
    {
        var outerStart = (outer.Start.Line, outer.Start.Character);
        var outerEnd = (outer.End.Line, outer.End.Character);
        var innerStart = (inner.Start.Line, inner.Start.Character);
        var innerEnd = (inner.End.Line, inner.End.Character);

        return outerStart.CompareTo(innerStart) <= 0 && outerEnd.CompareTo(innerEnd) >= 0;
    }
}
