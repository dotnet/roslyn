// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline;

[Trait(Traits.Feature, Traits.Features.DocumentOutline)]
public sealed class DocumentOutlineTests : DocumentOutlineTestsBase
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

    private async Task<(DocumentOutlineTestMocks mocks, (ImmutableArray<DocumentSymbolData> DocumentSymbolData, ITextSnapshot OriginalSnapshot), ImmutableArray<DocumentSymbolDataViewModel> uiItems)>
        InitializeMocksAndDataModelAndUIItems(string testCode)
    {
        await using var mocks = await CreateMocksAsync(testCode);
        var response = await DocumentOutlineViewModel.DocumentSymbolsRequestAsync(
            mocks.TextBuffer, mocks.LanguageServiceBrokerCallback, mocks.FilePath, CancellationToken.None);
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
            documentSymbolData = Sort(documentSymbolData, sortOption);
            var sortedDocumentSymbols = new FixedSizeArrayBuilder<DocumentSymbolDataViewModel>(documentSymbolData.Length);
            foreach (var documentSymbol in documentSymbolData)
            {
                var sortedChildren = SortDocumentSymbols(documentSymbol.Children, sortOption);
                sortedDocumentSymbols.Add(ReplaceChildren(documentSymbol, sortedChildren));
            }

            return sortedDocumentSymbols.MoveToImmutable();
        }

        static ImmutableArray<DocumentSymbolDataViewModel> Sort(ImmutableArray<DocumentSymbolDataViewModel> items, SortOption sortOption)
            => (ImmutableArray<DocumentSymbolDataViewModel>)DocumentSymbolDataViewModelSorter.Instance.Convert([items, sortOption], typeof(ImmutableArray<DocumentSymbolDataViewModel>), parameter: null, CultureInfo.CurrentCulture);

        static DocumentSymbolDataViewModel ReplaceChildren(DocumentSymbolDataViewModel symbolToUpdate, ImmutableArray<DocumentSymbolDataViewModel> newChildren)
        {
            var data = symbolToUpdate.Data;
            var symbolData = data with { Children = [] };
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

        // Empty search.  Should filter nothing.
        var searchedSymbols = DocumentOutlineViewModel.SearchDocumentSymbolData(model.DocumentSymbolData, string.Empty, CancellationToken.None);
        Assert.Equal(3, searchedSymbols.Length);

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
        Assert.Empty(searchedSymbols);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66012")]
    public async Task TestEnumOnSingleLine()
    {
        var (_, _, items) = await InitializeMocksAndDataModelAndUIItems(
            """
            enum Test
              { a, b }
            """);

        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal(Glyph.EnumInternal, item.Data.Glyph);
                Assert.Equal("Test", item.Data.Name);
                Assert.Collection(
                    item.Children,
                    item =>
                    {
                        Assert.Equal(Glyph.EnumMemberPublic, item.Data.Glyph);
                        Assert.Equal("a", item.Data.Name);
                    },
                    item =>
                    {
                        Assert.Equal(Glyph.EnumMemberPublic, item.Data.Glyph);
                        Assert.Equal("b", item.Data.Name);
                    });
            });
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66473")]
    public async Task TestClassOnSingleLine()
    {
        var (_, _, items) = await InitializeMocksAndDataModelAndUIItems(
            """
            abstract class TypeName { public string PropertyName { get; } = "Value"; }
            """);

        Assert.Collection(
            items,
            item =>
            {
                Assert.Equal(Glyph.ClassInternal, item.Data.Glyph);
                Assert.Equal("TypeName", item.Data.Name);
                Assert.Collection(
                    item.Children,
                    item =>
                    {
                        Assert.Equal(Glyph.PropertyPublic, item.Data.Glyph);
                        Assert.Equal("PropertyName", item.Data.Name);
                    });
            });
    }
}
