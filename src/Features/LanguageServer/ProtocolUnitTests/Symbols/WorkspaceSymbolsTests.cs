// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols
{
    public class WorkspaceSymbolsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Class()
        {
            var markup =
@"class {|class:A|}
{
    void M()
    {
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].Single())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "A").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Method()
        {
            var markup =
@"class A
{
    void {|method:M|}()
    {
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"].Single())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "M").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact(Skip = "GetWorkspaceSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetWorkspaceSymbolsAsync is updated to support all symbols.
        // https://github.com/dotnet/roslyn/projects/45#card-20033822
        public async Task TestGetWorkspaceSymbolsAsync_Local()
        {
            var markup =
@"class A
{
    void M()
    {
        int {|local:i|} = 1;
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Variable, "i", locations["local"].Single())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "i").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleKinds()
        {
            var markup =
@"class A
{
    int {|field:F|};
    void M()
    {
    }
    class {|class:F|}
    {
        int {|field:F|};
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Field, "F", locations["field"][0]),
                CreateSymbolInformation(LSP.SymbolKind.Class, "F", locations["class"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Field, "F", locations["field"][1])
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "F").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleDocuments()
        {
            var markups = new string[]
            {
@"class A
{
    void {|method:M|}()
    {
    }
}",
@"class B
{
    void {|method:M|}()
    {
    }
}"
            };

            var (solution, locations) = CreateTestSolution(markups);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"][0]),
                CreateSymbolInformation(LSP.SymbolKind.Method, "M", locations["method"][1])
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "M").ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_NoSymbols()
        {
            var markup =
@"class A
{
    void M()
    {
    }
}";
            var (solution, _) = CreateTestSolution(markup);

            var results = await RunGetWorkspaceSymbolsAsync(solution, "NonExistingSymbol").ConfigureAwait(false);
            Assert.Empty(results);
        }

        private static async Task<LSP.SymbolInformation[]> RunGetWorkspaceSymbolsAsync(Solution solution, string query)
        {
            var request = new LSP.WorkspaceSymbolParams
            {
                Query = query
            };

            return await GetLanguageServer(solution).GetWorkspaceSymbolsAsync(solution, request, new LSP.ClientCapabilities(), CancellationToken.None);
        }
    }
}
