// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols;

public sealed partial class DocumentSymbolsTests : AbstractLanguageServerProtocolTests
{
    public DocumentSymbolsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_WithoutHierarchicalSupport(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namespace Test;

            class {|class:A|}
            {
                public {|constructor:A|}()
                {
                }

                void {|method:M|}()
                {
                }

                static A operator {|operator:+|}(A a1, A a2) => a1;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        LSP.SymbolInformation[] expected = [
            CreateSymbolInformation(LSP.SymbolKind.Class, "Test.A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal),
            CreateSymbolInformation(LSP.SymbolKind.Method, "A()", testLspServer.GetLocations("constructor").Single(), Glyph.MethodPublic, "Test.A"),
            CreateSymbolInformation(LSP.SymbolKind.Method, "M()", testLspServer.GetLocations("method").Single(), Glyph.MethodPrivate, "Test.A"),
            CreateSymbolInformation(LSP.SymbolKind.Operator, "operator +(A a1, A a2)", testLspServer.GetLocations("operator").Single(), Glyph.OperatorPrivate, "Test.A"),
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
        AssertJsonEquals(expected, results);
    }

    [Theory(Skip = "GetDocumentSymbolsAsync does not yet support locals."), CombinatorialData]
    // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
    // https://github.com/dotnet/roslyn/projects/45#card-20033869
    public async Task TestGetDocumentSymbolsAsync__WithLocals(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void Method()
                {
                    int i = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer).ConfigureAwait(false);
        Assert.NotNull(results);
        Assert.Equal(3, results.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_EmptyName(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namepsace NamespaceA
            {
                public class
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer).ConfigureAwait(false);
        Assert.NotNull(results);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(".", results.First().Name);
#pragma warning restore CS0618
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync__NoSymbols(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    private static async Task<TReturn?> RunGetDocumentSymbolsAsync<TReturn>(TestLspServer testLspServer)
    {
        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var request = new LSP.DocumentSymbolParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI())
        };

        return await testLspServer.ExecuteRequestAsync<LSP.DocumentSymbolParams, TReturn>(LSP.Methods.TextDocumentDocumentSymbolName,
            request, CancellationToken.None);
    }
}
