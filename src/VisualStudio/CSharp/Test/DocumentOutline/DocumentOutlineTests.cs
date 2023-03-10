// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    [Trait(Traits.Feature, Traits.Features.DocumentOutline)]
    public class DocumentOutlineTests : DocumentOutlineTestsBase
    {
        private const string TestCode = """
            private class MyClass
            {
                int _x;

                static void Method1(string[] args) {}

                private class MyClass2 {}

                static void Method2(string[] args) {}
            }

            class App
            {
                void Method() {}

                void Z() {}
            }

            interface foo
            {
                void z() {}

                private static int apple = 9, b = 4, c = 6;

                void r() {}
            }
            """;

        public DocumentOutlineTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private async Task<(DocumentOutlineTestMocks mocks, (ImmutableArray<DocumentSymbolData> DocumentSymbolData, ITextSnapshot OriginalSnapshot), ImmutableArray<DocumentSymbolDataViewModel> uiItems)> InitializeMocksAndDataModelAndUIItems(string testCode)
        {
            await using var mocks = await CreateMocksAsync(testCode);
            var response = await DocumentOutlineViewModel.DocumentSymbolsRequestAsync(mocks.TextBuffer, mocks.LanguageServiceBroker, mocks.FilePath, CancellationToken.None);
            AssertEx.NotNull(response.Value);

            var model = DocumentOutlineViewModel.CreateDocumentSymbolData(response.Value.response, response.Value.snapshot);
            var uiItems = DocumentOutlineViewModel.GetDocumentSymbolItemViewModels(SortOption.Location, model);
            return (mocks, (model, response.Value.snapshot), uiItems);
        }

        [WpfTheory]
        [CombinatorialData]
        internal async Task TestSortDocumentSymbolData(SortOption sortOption)
        {
            var (_, _, items) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            var sortedSymbols = SortDocumentSymbols(items, sortOption);
            CheckSortedSymbols(sortedSymbols, sortOption);

            static ImmutableArray<DocumentSymbolDataViewModel> SortDocumentSymbols(
                ImmutableArray<DocumentSymbolDataViewModel> documentSymbolData,
                SortOption sortOption)
            {
                using var _ = ArrayBuilder<DocumentSymbolDataViewModel>.GetInstance(out var sortedDocumentSymbols);
                documentSymbolData = Sort(documentSymbolData, sortOption);
                foreach (var documentSymbol in documentSymbolData)
                {
                    var sortedChildren = SortDocumentSymbols(documentSymbol.Children, sortOption);
                    sortedDocumentSymbols.Add(ReplaceChildren(documentSymbol, sortedChildren));
                }

                return sortedDocumentSymbols.ToImmutable();
            }

            static ImmutableArray<DocumentSymbolDataViewModel> Sort(ImmutableArray<DocumentSymbolDataViewModel> items, SortOption sortOption)
                => (ImmutableArray<DocumentSymbolDataViewModel>)DocumentSymbolDataViewModelSorter.Instance.Convert(new object[] { items, sortOption }, typeof(ImmutableArray<DocumentSymbolDataViewModel>), parameter: null, CultureInfo.CurrentCulture);

            static DocumentSymbolDataViewModel ReplaceChildren(DocumentSymbolDataViewModel symbolToUpdate, ImmutableArray<DocumentSymbolDataViewModel> newChildren)
            {
                var data = symbolToUpdate.Data;
                var symbolData = new DocumentSymbolData(data.Name, data.SymbolKind, data.Glyph, data.RangeSpan, data.SelectionRangeSpan, ImmutableArray<DocumentSymbolData>.Empty);
                return new DocumentSymbolDataViewModel(symbolData, newChildren);
            }

            static void CheckSortedSymbols(ImmutableArray<DocumentSymbolDataViewModel> sortedSymbols, SortOption sortOption)
            {
                var actual = sortedSymbols;
                var expected = sortOption switch
                {
                    SortOption.Name => sortedSymbols.OrderBy(static x => x.Data.Name, StringComparer.OrdinalIgnoreCase),
                    SortOption.Location => sortedSymbols.OrderBy(static x => x.Data.RangeSpan.Start),
                    SortOption.Type => sortedSymbols.OrderBy(static x => x.Data.SymbolKind).ThenBy(static x => x.Data.Name),
                    _ => throw new InvalidOperationException($"The value for {nameof(sortOption)} is invalid: {sortOption}")
                };

                Assert.True(expected.SequenceEqual(actual));

                foreach (var symbol in sortedSymbols)
                    CheckSortedSymbols(symbol.Children, sortOption);
            }
        }

        [WpfFact]
        public async Task TestSearchDocumentSymbolData()
        {
            var (_, model, _) = await InitializeMocksAndDataModelAndUIItems(TestCode);

            // Empty search (added for completeness, SearchDocumentSymbolData is not called on an empty search string)
            var searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, string.Empty, CancellationToken.None);
            Assert.Equal(0, searchedSymbols.Length);

            // Search for 1 parent only (no children should match)
            searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, "foo", CancellationToken.None);
            Assert.Equal(1, searchedSymbols.Length);
            Assert.Equal(0, searchedSymbols.Single(symbol => symbol.Name.Equals("foo")).Children.Length);

            // Search for children only (across 2 parents)
            searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, "Method", CancellationToken.None);
            Assert.Equal(2, searchedSymbols.Length);
            Assert.Equal(2, searchedSymbols.Single(symbol => symbol.Name.Equals("MyClass")).Children.Length);
            Assert.Equal(1, searchedSymbols.Single(symbol => symbol.Name.Equals("App")).Children.Length);

            // Search for a parent and a child (of another parent)
            searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, "app", CancellationToken.None);
            Assert.Equal(2, searchedSymbols.Length);
            Assert.Equal(0, searchedSymbols.Single(symbol => symbol.Name.Equals("App")).Children.Length);
            Assert.Equal(1, searchedSymbols.Single(symbol => symbol.Name.Equals("foo")).Children.Length);

            // No search results found
            searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, "xyz", CancellationToken.None);
            Assert.Equal(0, searchedSymbols.Length);
        }

        [WpfFact]
        public async Task TestGetDocumentNodeToSelect()
        {
            var (mocks, model, uiItems) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            var currentTextSnapshotLines = mocks.TextBuffer.CurrentSnapshot.Lines;
            AssertEx.NotNull(model.OriginalSnapshot);

            // Click between 2 parent nodes (no symbol is selected)
            var caretPosition = currentTextSnapshotLines.ElementAt(10).End;
            var nodeToSelect = DocumentOutlineViewModel.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Null(nodeToSelect);

            // Click within range of a parent symbol
            caretPosition = currentTextSnapshotLines.ElementAt(1).End;
            nodeToSelect = DocumentOutlineViewModel.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("MyClass", nodeToSelect?.Data.Name);

            // Click within range of a child symbol
            caretPosition = currentTextSnapshotLines.ElementAt(4).End - 1;
            nodeToSelect = DocumentOutlineViewModel.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("Method1", nodeToSelect?.Data.Name);

            // Click between 2 child symbols (caret is in range of parent)
            caretPosition = currentTextSnapshotLines.ElementAt(14).End;
            nodeToSelect = DocumentOutlineViewModel.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("App", nodeToSelect?.Data.Name);
        }
    }
}
