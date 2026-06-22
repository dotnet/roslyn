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
    /// <summary>
    /// Caret at the literal <c>1</c> inside a binary expression in a method body.
    /// The chain expands through literal → binary → equals-value → declarator →
    /// declaration → local-decl-stmt → block → method → class.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_MethodBody(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|class C
            {
                [|void M()
                [|{
                    [|[|var [|x [|= [|[|{|caret:|}1|] + 2|]|]|]|];|]
                }|]|]
            }|]
            """);

    /// <summary>
    /// Caret at <c>1</c> in a top-level statement file (no class or namespace wrapper).
    /// The chain reaches the compilation unit directly.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_TopLevelStatements(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|[|[|var [|x [|= [|[|{|caret:|}1|] + 2|]|]|]|];|]
            var y = 1;|]
            """);

    /// <summary>
    /// Caret at <c>1</c> in a method inside a file-scoped namespace.
    /// The outermost range is the <c>FileScopedNamespaceDeclarationSyntax</c>.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_FileScopedNamespace(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|namespace MyNamespace;
            [|class C
            {
                [|void M()
                [|{
                    [|[|var [|x [|= [|{|caret:|}1|]|]|]|];|]
                }|]|]
            }|]|]
            """);

    /// <summary>
    /// Caret at <c>1</c> inside a doubly-nested namespace.
    /// The chain expands through both inner and outer <c>NamespaceDeclarationSyntax</c> nodes.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_NestedNamespaces(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|namespace Outer
            {
                [|namespace Inner
                {
                    [|class C
                    {
                        [|void M()
                        [|{
                            [|[|var [|x [|= [|{|caret:|}1|]|]|]|];|]
                        }|]|]
                    }|]
                }|]
            }|]
            """);

    /// <summary>
    /// Caret at <c>a</c> inside the body of a local function.
    /// The chain expands through the local function's block and then the outer method's block.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_LocalFunction(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|class C
            {
                [|void M()
                [|{
                    [|int Compute(int a, int b)
                    [|{
                        [|return [|[|{|caret:|}a|] + b|];|]
                    }|]|]
                    _ = Compute(1, 2);
                }|]|]
            }|]
            """);

    /// <summary>
    /// Caret at <c>a</c> inside an expression-bodied method.
    /// The chain expands through binary → arrow-expression-clause → method → class.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_ExpressionBodyMember(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|class C
            {
                [|int Compute(int a, int b) [|=> [|[|{|caret:|}a|] + b|]|];|]
            }|]
            """);

    /// <summary>
    /// Caret at <c>1</c> (the true-branch literal) inside a conditional expression.
    /// The chain expands through the full ternary before reaching the enclosing declaration.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_ConditionalExpression(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|class C
            {
                [|void M(bool flag)
                [|{
                    [|[|var [|x [|= [|flag ? [|{|caret:|}1|] : 2|]|]|]|];|]
                }|]|]
            }|]
            """);

    /// <summary>
    /// Caret at the <c>return</c> keyword inside a single-line if statement.
    /// The chain expands: return-stmt → if-stmt → block → method → class.
    /// </summary>
    [Theory, CombinatorialData]
    public Task TestGetSelectionRangeAsync_SingleLineIf(bool mutatingLspWorkspace)
        => AssertSelectionRangesAsync(mutatingLspWorkspace,
            """
            [|class C
            {
                [|void M()
                [|{
                    [|if (true) [|{|caret:|}return;|]|]
                }|]|]
            }|]
            """);

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

    private async Task AssertSelectionRangesAsync(bool mutatingLspWorkspace, string markup)
    {
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caret = testLspServer.GetLocations("caret").Single();
        var result = await RunGetSelectionRangeAsync(testLspServer, caret);
        Assert.NotNull(result);

        // Collect the actual chain from innermost to outermost.
        var chain = new List<LSP.Range>();
        for (var current = result; current is not null; current = current.Parent)
            chain.Add(current.Range);

        // [|...|] spans use the same LIFO stack as named spans, so SelectedSpans is
        // returned innermost-first — matching the handler chain order. If the markup
        // parser's ordering ever changes, these tests would surface the mismatch as
        // a sequence-equality failure.
        var testDocument = testLspServer.TestWorkspace.Documents.Single();
        var document = testLspServer.GetCurrentSolution().GetDocument(testDocument.Id)!;
        var text = await document.GetTextAsync(CancellationToken.None);
        var expected = testDocument.SelectedSpans
            .Select(span => ProtocolConversions.TextSpanToRange(span, text))
            .ToList();

        Assert.Equal(expected, chain);
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
