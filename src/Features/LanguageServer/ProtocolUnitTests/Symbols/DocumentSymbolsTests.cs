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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols
{
    public class DocumentSymbolsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetDocumentSymbolsAsync()
        {
            var markup =
@"{|class:class {|classSelection:A|}
{
    {|method:void {|methodSelection:M|}()
    {
    }|}
}|}";
            var clientCapabilities = new LSP.ClientCapabilities()
            {
                TextDocument = new LSP.TextDocumentClientCapabilities()
                {
                    DocumentSymbol = new LSP.DocumentSymbolSetting()
                    {
                        HierarchicalDocumentSymbolSupport = true
                    }
                }
            };
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var expected = new LSP.DocumentSymbol[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single())
            };
            CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", testLspServer.GetLocations("method").Single(), testLspServer.GetLocations("methodSelection").Single(), expected.First());

            var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithoutHierarchicalSupport()
        {
            var markup =
@"class {|class:A|}
{
    void {|method:M|}()
    {
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal),
                CreateSymbolInformation(LSP.SymbolKind.Method, "M()", testLspServer.GetLocations("method").Single(), Glyph.MethodPrivate, "A")
            };

            var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Fact(Skip = "GetDocumentSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
        // https://github.com/dotnet/roslyn/projects/45#card-20033869
        public async Task TestGetDocumentSymbolsAsync__WithLocals()
        {
            var markup =
@"class A
{
    void Method()
    {
        int i = 1;
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer).ConfigureAwait(false);
            Assert.Equal(3, results.Length);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NoSymbols()
        {
            using var testLspServer = await CreateTestLspServerAsync(string.Empty);

            var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
            Assert.Empty(results);
        }

        private static async Task<TReturn> RunGetDocumentSymbolsAsync<TReturn>(TestLspServer testLspServer)
        {
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var request = new LSP.DocumentSymbolParams
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            return await testLspServer.ExecuteRequestAsync<LSP.DocumentSymbolParams, TReturn>(LSP.Methods.TextDocumentDocumentSymbolName,
                request, CancellationToken.None);
        }

        private static void AssertDocumentSymbolEquals(LSP.DocumentSymbol expected, LSP.DocumentSymbol actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Range, actual.Range);
            Assert.Equal(expected.Children.Length, actual.Children.Length);
            for (var i = 0; i < actual.Children.Length; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }

        private static LSP.DocumentSymbol CreateDocumentSymbol(LSP.SymbolKind kind, string name, string detail,
            LSP.Location location, LSP.Location selection, LSP.DocumentSymbol parent = null)
        {
            var documentSymbol = new LSP.DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Range = location.Range,
                Children = new LSP.DocumentSymbol[0],
                Detail = detail,
                Deprecated = false,
                SelectionRange = selection.Range
            };

            if (parent != null)
            {
                var children = parent.Children.ToList();
                children.Add(documentSymbol);
                parent.Children = children.ToArray();
            }

            return documentSymbol;
        }
    }
}
