// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

/// <summary>
/// The F# results are sorted as a C# or VB result is: see NavigateToPrioritizeResultInCurrentDocument in NavigateToTests.
/// </summary>
public sealed class FSharpNavigateToSearchResultTests
{
    [Fact]
    public void ResultInActiveDocumentSortsFirst()
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = AddDocument(workspace, projectId, "Active", ["A", "B", "C"]);

        Assert.Equal("0000 0002 0001 M Active", SecondarySort(document, activeDocument: document));
    }

    [Theory]
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "B", "C" }, "0001")]
    [InlineData(new[] { "A", "B", "C", "D" }, new[] { "A", "B", "C" }, "0002")]
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "B", "C", "D" }, "0002")]
    [InlineData(new[] { "A", "B", "C", "D1" }, new[] { "A", "B", "C", "D2" }, "0003")]
    public void ResultSortsByFolderDistanceFromActiveDocument(string[] activeFolders, string[] otherFolders, string folderDistance)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var activeDocument = AddDocument(workspace, projectId, "Active", activeFolders);
        var otherDocument = AddDocument(workspace, projectId, "Other", otherFolders);

        Assert.Equal($"{folderDistance} 0002 0001 M Other", SecondarySort(otherDocument, activeDocument));
    }

    [Fact]
    public void ResultWithoutActiveDocumentOrCountsSortsByName()
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = AddDocument(workspace, projectId, "Other", []);

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
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = AddDocument(workspace, projectId, "Other", []);

        INavigateToSearchResult result = new InternalFSharpNavigateToSearchResult(
            new FSharpNavigateToSearchResult("", FSharpNavigateToItemKind.Method, FSharpNavigateToMatchKind.Prefix, isCaseSensitive: true, "Program", [new TextSpan(0, 3)], NavigableItem(document), parameterCount: 0, typeParameterCount: 0),
            activeDocument: null);

        var match = Assert.Single(result.Matches);
        Assert.True(match.IsCaseSensitive);
        Assert.Equal<TextSpan>([new TextSpan(0, 3)], match.MatchedSpans);
    }

    private static string SecondarySort(Document document, Document activeDocument)
    {
        INavigateToSearchResult result = new InternalFSharpNavigateToSearchResult(
            new FSharpNavigateToSearchResult("", FSharpNavigateToItemKind.Method, FSharpNavigateToMatchKind.Exact, isCaseSensitive: false, "M", [], NavigableItem(document), parameterCount: 2, typeParameterCount: 1),
            activeDocument);

        return result.SecondarySort;
    }

    private static Document AddDocument(AdhocWorkspace workspace, ProjectId projectId, string name, string[] folders)
        => workspace.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(projectId), name + ".cs", folders, filePath: @"C:\" + name + ".cs"));

    private static FSharpNavigableItem NavigableItem(Document document)
        => new(FSharpGlyph.MethodPublic, [], document, new TextSpan(0, 0));
}
