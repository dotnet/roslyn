// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    [Trait(Traits.Feature, Traits.Features.DocumentOutline)]
    public class DocumentOutlineTests : DocumentOutlineTestsBase
    {
        private const string TestCode = @"
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
        }"
;

        public DocumentOutlineTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private async Task<(DocumentOutlineTestMocks mocks, DocumentSymbolDataModel model, ImmutableArray<DocumentSymbolUIItem> uiItems)> InitializeMocksAndDataModelAndUIItems(string testCode)
        {
            await using var mocks = await CreateMocksAsync(testCode);
            var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(mocks.TextBuffer, mocks.LanguageServiceBroker, mocks.FilePath, CancellationToken.None);
            AssertEx.NotNull(response.Value);

            var responseBody = response.Value.response?.ToObject<DocumentSymbol[]>();
            AssertEx.NotNull(responseBody);

            var snapshot = response.Value.snapshot;
            AssertEx.NotNull(snapshot);

            var model = DocumentOutlineHelper.CreateDocumentSymbolDataModel(responseBody, snapshot);
            var uiItems = DocumentOutlineHelper.GetDocumentSymbolUIItems(model.DocumentSymbolData, mocks.ThreadingContext);
            return (mocks, model, uiItems);
        }

        [WpfFact]
        public async Task TestSortDocumentSymbolDataByName()
        {
            await CheckSorting(SortOption.Name);
        }

        [WpfFact]
        public async Task TestSortDocumentSymbolDataByType()
        {
            await CheckSorting(SortOption.Type);
        }

        [WpfFact]
        public async Task TestSortDocumentSymbolDataByLocation()
        {
            await CheckSorting(SortOption.Location);
        }

        private async Task CheckSorting(SortOption sortOption)
        {
            var (_, model, _) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            var sortedSymbols = DocumentOutlineHelper.SortDocumentSymbolData(model.DocumentSymbolData, sortOption, CancellationToken.None);
            CheckSortedSymbols(sortedSymbols);

            void CheckSortedSymbols(ImmutableArray<DocumentSymbolData> sortedSymbols)
            {
                for (var i = 0; i < sortedSymbols.Length - 1; i++)
                {
                    switch (sortOption)
                    {
                        case SortOption.Name:
                            Assert.True(StringComparer.OrdinalIgnoreCase.Compare(sortedSymbols[i].Name, sortedSymbols[i + 1].Name) <= 0);
                            break;
                        case SortOption.Location:
                            Assert.True(sortedSymbols[i].RangeSpan.Start < sortedSymbols[i + 1].RangeSpan.Start);
                            break;
                        case SortOption.Type:
                            if (sortedSymbols[i].SymbolKind != sortedSymbols[i + 1].SymbolKind)
                                Assert.True(sortedSymbols[i].SymbolKind < sortedSymbols[i + 1].SymbolKind);
                            else
                                Assert.True(StringComparer.OrdinalIgnoreCase.Compare(sortedSymbols[i].Name, sortedSymbols[i + 1].Name) <= 0);
                            break;
                    }
                }

                foreach (var symbol in sortedSymbols)
                    CheckSortedSymbols(symbol.Children);
            }
        }

        [WpfFact]
        public async Task TestSearchDocumentSymbolData()
        {
            var (_, model, _) = await InitializeMocksAndDataModelAndUIItems(TestCode);

            // Empty search (added for completeness, SearchDocumentSymbolData is not called on an empty search string)
            var searchedSymbols = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, string.Empty, CancellationToken.None);
            Assert.Equal(0, searchedSymbols.Length);

            // Search for 1 parent only (no children should match)
            searchedSymbols = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, "foo", CancellationToken.None);
            Assert.Equal(1, searchedSymbols.Length);
            Assert.Equal(0, searchedSymbols.Single(symbol => symbol.Name.Equals("foo")).Children.Length);

            // Search for children only (across 2 parents)
            searchedSymbols = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, "Method", CancellationToken.None);
            Assert.Equal(2, searchedSymbols.Length);
            Assert.Equal(2, searchedSymbols.Single(symbol => symbol.Name.Equals("MyClass")).Children.Length);
            Assert.Equal(1, searchedSymbols.Single(symbol => symbol.Name.Equals("App")).Children.Length);

            // Search for a parent and a child (of aanother parent)
            searchedSymbols = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, "app", CancellationToken.None);
            Assert.Equal(2, searchedSymbols.Length);
            Assert.Equal(0, searchedSymbols.Single(symbol => symbol.Name.Equals("App")).Children.Length);
            Assert.Equal(1, searchedSymbols.Single(symbol => symbol.Name.Equals("foo")).Children.Length);

            // No search results found
            searchedSymbols = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, "xyz", CancellationToken.None);
            Assert.Equal(0, searchedSymbols.Length);
        }

        [WpfFact]
        public async Task TestGetDocumentNodeToSelect()
        {
            var (mocks, model, uiItems) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            var currentTextSnapshotLines = mocks.TextBuffer.CurrentSnapshot.Lines;

            // Click between 2 parent nodes (no symbol is selected)
            var caretPosition = currentTextSnapshotLines.ElementAt(11).End;
            var nodeToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Null(nodeToSelect);

            // Click within range of a parent symbol
            caretPosition = currentTextSnapshotLines.ElementAt(1).End;
            nodeToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("MyClass", nodeToSelect?.Name);

            // Click within range of a child symbol
            caretPosition = currentTextSnapshotLines.ElementAt(5).End - 1;
            nodeToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("Method1", nodeToSelect?.Name);

            // Click between 2 child symbols (caret is in range of parent)
            caretPosition = currentTextSnapshotLines.ElementAt(15).End;
            nodeToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(uiItems, model.OriginalSnapshot, caretPosition);
            Assert.Equal("App", nodeToSelect?.Name);
        }

        [WpfFact]
        public async Task TestSetIsExpanded()
        {
            var (mocks, model, originalUIItems) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            var updatedUIItems = DocumentOutlineHelper.GetDocumentSymbolUIItems(model.DocumentSymbolData, mocks.ThreadingContext);

            // Check that all updatedUIItems nodes are collapsed (originalUIItems parameter is unused)
            DocumentOutlineHelper.SetIsExpanded(updatedUIItems, originalUIItems, ExpansionOption.Collapse);
            CheckNodeExpansion(updatedUIItems, false);

            // Check that all updatedUIItems nodes are expanded (originalUIItems parameter is unused)
            DocumentOutlineHelper.SetIsExpanded(updatedUIItems, originalUIItems, ExpansionOption.Expand);
            CheckNodeExpansion(updatedUIItems, true);

            // Collapse 3 nodes in originalUIItems
            originalUIItems.Single(parent => parent.Name.Equals("App")).IsExpanded = false;
            originalUIItems.Single(parent => parent.Name.Equals("MyClass")).Children.Single(child => child.Name.Equals("Method2")).IsExpanded = false;
            originalUIItems.Single(parent => parent.Name.Equals("foo")).Children.Single(child => child.Name.Equals("r")).IsExpanded = false;

            // Apply same expansion as originalUIItems to updatedUIItems
            DocumentOutlineHelper.SetIsExpanded(updatedUIItems, originalUIItems, ExpansionOption.CurrentExpansion);

            // Confirm that matching expanded/collapsed node states have been applied
            CheckNodeExpansionMatches(updatedUIItems, originalUIItems);

            static void CheckNodeExpansion(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems, bool isExpanded)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    Assert.True(symbol.IsExpanded == isExpanded);
                    CheckNodeExpansion(symbol.Children, isExpanded);
                }
            }

            static void CheckNodeExpansionMatches(ImmutableArray<DocumentSymbolUIItem> newUIItems, ImmutableArray<DocumentSymbolUIItem> originalUIItems)
            {
                for (var i = 0; i < newUIItems.Length; i++)
                {
                    Assert.True(newUIItems[i].IsExpanded == originalUIItems[i].IsExpanded);
                    CheckNodeExpansionMatches(newUIItems[i].Children, originalUIItems[i].Children);
                }
            }
        }

        [WpfFact]
        public async Task TestExpandAncestors()
        {
            var (mocks, model, uiItems) = await InitializeMocksAndDataModelAndUIItems(TestCode);

            // Collapse all nodes first
            DocumentOutlineHelper.SetIsExpanded(uiItems, uiItems, ExpansionOption.Collapse);

            // Call ExpandAncestors on a child node
            var selectedNode = uiItems.Single(parent => parent.Name.Equals("MyClass")).Children.Single(child => child.Name.Equals("Method2"));
            DocumentOutlineHelper.ExpandAncestors(uiItems, selectedNode.RangeSpan);

            // Confirm that only the child node and its ancestors are expanded
            CheckAncestorNodeExpansion(uiItems);

            static void CheckAncestorNodeExpansion(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    Assert.True(symbol.Name.Equals("MyClass") || symbol.Name.Equals("Method2") ? symbol.IsExpanded : !symbol.IsExpanded);
                    CheckAncestorNodeExpansion(symbol.Children);
                }
            }
        }

        [WpfFact]
        public async Task TestUnselectAll()
        {
            var (_, _, uiItems) = await InitializeMocksAndDataModelAndUIItems(TestCode);
            DocumentOutlineHelper.UnselectAll(uiItems);
            CheckNodesUnselected(uiItems);

            static void CheckNodesUnselected(ImmutableArray<DocumentSymbolUIItem> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    Assert.False(symbol.IsSelected);
                    CheckNodesUnselected(symbol.Children);
                }
            }
        }
    }
}
