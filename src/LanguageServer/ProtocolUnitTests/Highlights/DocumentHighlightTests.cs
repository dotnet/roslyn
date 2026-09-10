// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Highlights;

public sealed class DocumentHighlightTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestGetDocumentHighlightAsync(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class B
            {
            }
            class A
            {
                B {|text:classB|};
                void M()
                {
                    var someVar = {|read:classB|};
                    {|caret:|}{|write:classB|} = new B();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);
        var expected = new LSP.DocumentHighlight[]
        {
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Text, testLspServer.GetLocations("text").Single()),
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Read, testLspServer.GetLocations("read").Single()),
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Write, testLspServer.GetLocations("write").Single())
        };

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertJsonEquals(expected, results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/59120")]
    public async Task TestGetDocumentHighlightAsync_Keywords(bool lspMutatingWorkspace)
    {
        var markup =
            """
            using System.Threading.Tasks;
            class A
            {
                {|text:async|} Task MAsync()
                {
                    {|text:await|} Task.Delay(100);
                    {|caret:|}{|text:await|} Task.Delay(100);
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var expectedLocations = testLspServer.GetLocations("text");

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(LSP.DocumentHighlightKind.Text, r.Kind));
        Assert.Equal(expectedLocations[0].Range, results[0].Range);
        Assert.Equal(expectedLocations[1].Range, results[1].Range);
        Assert.Equal(expectedLocations[2].Range, results[2].Range);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentHighlightAsync_InvalidLocation(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|caret:|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/76089")]
    public async Task TestGetDocumentHighlightAsync_PartialConstructor(bool lspMutatingWorkspace)
    {
        var markup =
            """
            partial class C
            {
                partial {|caret:|}{|text:C|}();
                partial {|text:C|}()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var expectedLocations = testLspServer.GetLocations("text");

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(2, results.Length);
        Assert.All(results, r => Assert.Equal(LSP.DocumentHighlightKind.Text, r.Kind));
        Assert.Equal(expectedLocations[0].Range, results[0].Range);
        Assert.Equal(expectedLocations[1].Range, results[1].Range);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentHighlightAsync_ConstructorOverloads(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class C
            {
                {|caret:|}{|text:C|}()
                {
                }

                C(int x)
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var expectedLocations = testLspServer.GetLocations("text");

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        // Should only highlight the parameterless constructor
        Assert.Single(results);
        Assert.Equal(LSP.DocumentHighlightKind.Text, results[0].Kind);
        Assert.Equal(expectedLocations[0].Range, results[0].Range);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/83245")]
    public async Task TestGetDocumentHighlightAsync_DelegateConstructor(bool lspMutatingWorkspace)
    {
        var markup =
            """
            using System;
            class C
            {
                void M()
                {
                    var z = new {|caret:|}Comparison<int>((a, b) => 0);
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.NotNull(results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/9716")]
    public async Task TestGetDocumentHighlightAsync_CharacterPastEndOfLine(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                }
            }{|caret:|}
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        // The LSP spec allows a client to send a character past the end of the line:
        // "If the character value is greater than the line length it defaults back to the line length."
        // The caret is on the last line here, so an unclamped offset lands past the end of the document
        // and SyntaxNode.FindToken throws ArgumentOutOfRangeException.
        var pastEndOfLine = new LSP.Position { Line = caret.Range.Start.Line, Character = caret.Range.Start.Character + 100 };

        var results = await RunGetDocumentHighlightAsync(testLspServer, caret.DocumentUri, pastEndOfLine);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/9716")]
    public async Task TestGetDocumentHighlightAsync_KeywordCharacterPastEndOfLine(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    if (true)
                        return;
                    else{|caret:|}
                        return;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        // Far enough past the end of the line that the unclamped offset lands past the end of the
        // document, which is what reaches the throw in SyntaxNode.FindToken via AbstractKeywordHighlighter.
        var pastEndOfLine = new LSP.Position { Line = caret.Range.Start.Line, Character = caret.Range.Start.Character + 1000 };

        var expected = await RunGetDocumentHighlightAsync(testLspServer, caret);

        // Verify the keyword highlighter is actually what produces results for this position.
        Assert.NotEmpty(expected);
        Assert.All(expected, r => Assert.Equal(LSP.DocumentHighlightKind.Text, r.Kind));

        // Clamping the character to the line end must produce the same result as the in-range position.
        var results = await RunGetDocumentHighlightAsync(testLspServer, caret.DocumentUri, pastEndOfLine);
        AssertJsonEquals(expected, results);
    }

    private static Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(TestLspServer testLspServer, LSP.Location caret)
        => RunGetDocumentHighlightAsync(testLspServer, caret.DocumentUri, caret.Range.Start);

    private static async Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(
        TestLspServer testLspServer, LSP.DocumentUri documentUri, LSP.Position position)
    {
        var request = new LSP.TextDocumentPositionParams
        {
            TextDocument = CreateTextDocumentIdentifier(documentUri),
            Position = position,
        };

        var results = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]>(
            LSP.Methods.TextDocumentDocumentHighlightName, request, CancellationToken.None);
        Array.Sort(results, (h1, h2) =>
        {
            var compareKind = h1.Kind.CompareTo(h2.Kind);
            var compareRange = CompareRange(h1.Range, h2.Range);
            return compareKind != 0 ? compareKind : compareRange;
        });

        return results;
    }

    private static LSP.DocumentHighlight CreateDocumentHighlight(LSP.DocumentHighlightKind kind, LSP.Location location)
        => new()
        {
            Kind = kind,
            Range = location.Range
        };
}
