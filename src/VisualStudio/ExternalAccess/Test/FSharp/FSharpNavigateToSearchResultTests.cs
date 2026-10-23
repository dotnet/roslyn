// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

// F# results sort as C# or VB ones do: see NavigateToPrioritizeResultInCurrentDocument in NavigateToTests.
public sealed class FSharpNavigateToSearchResultTests : IDisposable
{
    private readonly AdhocWorkspace _workspace = new(NoCompilationLanguage.CreateHostServices());
    private readonly ProjectId _projectId;

    public FSharpNavigateToSearchResultTests()
        => _projectId = _workspace.AddProject("Project", NoCompilationLanguage.Name).Id;

    public void Dispose()
        => _workspace.Dispose();

    [Fact]
    public void ResultInActiveDocumentSortsFirst()
    {
        var document = AddDocument("Active", ["A", "B", "C"]);

        Assert.Equal("0000 0002 0001 M Active", SecondarySort(document, activeDocument: document));
    }

    [Theory]
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "B", "C" }, "0001")]
    [InlineData(new[] { "A", "B", "C", "D" }, new[] { "A", "B", "C" }, "0002")]
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "B", "C", "D" }, "0002")]
    [InlineData(new[] { "A", "B", "C", "D1" }, new[] { "A", "B", "C", "D2" }, "0003")]
    public void ResultSortsByFolderDistanceFromActiveDocument(string[] activeFolders, string[] otherFolders, string folderDistance)
    {
        var activeDocument = AddDocument("Active", activeFolders);
        var otherDocument = AddDocument("Other", otherFolders);

        Assert.Equal($"{folderDistance} 0002 0001 M Other", SecondarySort(otherDocument, activeDocument));
    }

    [Fact]
    public void ResultWithoutActiveDocumentOrCountsSortsByName()
    {
        var document = AddDocument("Other", []);

        INavigateToSearchResult result = new InternalFSharpNavigateToSearchResult(
            new FSharpNavigateToSearchResult("", FSharpNavigateToItemKind.Method, FSharpNavigateToMatchKind.Exact, "M", NavigableItem(document)),
            activeDocument: null);

        Assert.Equal("0000 0000 0000 M Other", result.SecondarySort);
        Assert.False(result.IsCaseSensitive);
        Assert.Empty(result.NameMatchSpans);
    }

    [Fact]
    public void ResultMatchesWithTheCaseAndSpansOfTheName()
    {
        var document = AddDocument("Other", []);

        var result = SearchResult(document, matchKind: FSharpNavigateToMatchKind.Prefix, isCaseSensitive: true, name: "Program", nameMatchSpans: [new TextSpan(0, 3)]);

        var match = Assert.Single(result.Matches);
        Assert.True(match.IsCaseSensitive);
        Assert.Equal<TextSpan>([new TextSpan(0, 3)], match.MatchedSpans);
    }

    private static string SecondarySort(Document document, Document activeDocument)
        => SearchResult(document, activeDocument, parameterCount: 2, typeParameterCount: 1).SecondarySort;

    private static INavigateToSearchResult SearchResult(
        Document document, Document? activeDocument = null, FSharpNavigateToMatchKind matchKind = FSharpNavigateToMatchKind.Exact,
        bool isCaseSensitive = false, string name = "M", ImmutableArray<TextSpan> nameMatchSpans = default, int parameterCount = 0, int typeParameterCount = 0)
        => new InternalFSharpNavigateToSearchResult(
            new FSharpNavigateToSearchResult("", FSharpNavigateToItemKind.Method, matchKind, isCaseSensitive, name, nameMatchSpans, NavigableItem(document), parameterCount, typeParameterCount),
            activeDocument);

    // Rooted with '/' rather than a drive and backslash, which Unix would keep as part of the file name in the sort key.
    private Document AddDocument(string name, string[] folders)
        => _workspace.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(_projectId), name + ".fs", folders, filePath: "/" + name + ".fs"));

    private static FSharpNavigableItem NavigableItem(Document document)
        => new(FSharpGlyph.MethodPublic, [], document, new TextSpan(0, 0));
}
