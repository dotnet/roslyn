// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.NavigateTo)]
public sealed class NavigateToSearcherTests
{
    private static readonly TestComposition FirstActiveAndVisibleComposition = EditorTestCompositions.EditorFeatures.AddParts(typeof(FirstDocumentIsActiveAndVisibleDocumentTrackingService.Factory));

    private static void SetupSearchProject(
        Mock<IAdvancedNavigateToSearchService> searchService,
        string pattern,
        bool isFullyLoaded,
        ImmutableArray<INavigateToSearchResult> results)
    {
        if (isFullyLoaded)
        {
            // First do a full search
            searchService.Setup(ss => ss.SearchProjectsAsync(
                It.IsAny<Solution>(),
                It.IsAny<ImmutableArray<Project>>(),
                It.IsAny<ImmutableArray<Document>>(),
                pattern,
                ImmutableHashSet<string>.Empty,
                It.IsAny<bool>(),
                It.IsAny<Document?>(),
                It.IsAny<Func<ImmutableArray<INavigateToSearchResult>, Task>>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>())).Callback(
                (Solution solution,
                 ImmutableArray<Project> projects,
                 ImmutableArray<Document> priorityDocuments,
                 string pattern,
                 IImmutableSet<string> kinds,
                 bool searchGeneratedCode,
                 Document? activeDocument,
                 Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
                 Func<Task> onProjectCompleted,
                 CancellationToken cancellationToken) =>
                {
                    if (results.Length > 0)
                        onResultsFound(results);
                }).Returns(Task.CompletedTask);

            searchService.Setup(ss => ss.SearchSourceGeneratedDocumentsAsync(
                It.IsAny<Solution>(),
                It.IsAny<ImmutableArray<Project>>(),
                pattern,
                ImmutableHashSet<string>.Empty,
                It.IsAny<Document?>(),
                It.IsAny<Func<ImmutableArray<INavigateToSearchResult>, Task>>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>())).Callback(
                (Solution solution,
                 ImmutableArray<Project> projects,
                 string pattern,
                 IImmutableSet<string> kinds,
                 Document? activeDocument,
                 Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
                 Func<Task> onProjectCompleted,
                 CancellationToken cancellationToken) =>
                {
                    if (results.Length > 0)
                        onResultsFound(results);
                }).Returns(Task.CompletedTask);

            // Followed by a generated doc search.
        }
        else
        {
            searchService.Setup(ss => ss.SearchCachedDocumentsAsync(
                It.IsAny<Solution>(),
                It.IsAny<ImmutableArray<Project>>(),
                It.IsAny<ImmutableArray<Document>>(),
                pattern,
                ImmutableHashSet<string>.Empty,
                It.IsAny<Document?>(),
                It.IsAny<Func<ImmutableArray<INavigateToSearchResult>, Task>>(),
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>())).Callback(
                (Solution solution,
                 ImmutableArray<Project> projects,
                 ImmutableArray<Document> priorityDocuments,
                 string pattern2,
                 IImmutableSet<string> kinds,
                 Document? activeDocument,
                 Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound2,
                 Func<Task> onProjectCompleted,
                 CancellationToken cancellationToken) =>
                {
                    if (results.Length > 0)
                        onResultsFound2(results);
                }).Returns(Task.CompletedTask);
        }
    }

    private static ValueTask<bool> IsFullyLoadedAsync(bool projectSystem, bool remoteHost)
        => new(projectSystem && remoteHost);

    [Fact]
    public async Task NotFullyLoadedOnlyMakesOneSearchProjectCallIfValueReturned()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");

        var pattern = "irrelevant";

        var results = ImmutableArray.Create<INavigateToSearchResult>(new TestNavigateToSearchResult(workspace, new TextSpan(0, 0)));

        var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);
        SetupSearchProject(searchService, pattern, isFullyLoaded: false, results);

        // Simulate a host that says the solution isn't fully loaded.
        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystem: false, remoteHost: false));
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportIncomplete());
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
        callbackMock.Setup(c => c.AddResultsAsync(results, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Because we returned a result when not fully loaded, we should notify the user that data was not complete.
        callbackMock.Setup(c => c.Done(false));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, CancellationToken.None);
    }

    [Theory, CombinatorialData]
    public async Task NotFullyLoadedMakesTwoSearchProjectCallIfValueNotReturned(bool projectSystemFullyLoaded)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");

        var pattern = "irrelevant";

        var results = ImmutableArray.Create<INavigateToSearchResult>(new TestNavigateToSearchResult(workspace, new TextSpan(0, 0)));

        var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);

        // First call will pass in that we're not fully loaded.  If we return null, we should get
        // another call with the request to search the fully loaded data.
        SetupSearchProject(searchService, pattern, isFullyLoaded: false, results: []);
        SetupSearchProject(searchService, pattern, isFullyLoaded: true, results);

        // Simulate a host that says the solution isn't fully loaded.
        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystemFullyLoaded, remoteHost: false));
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportIncomplete());
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
        callbackMock.Setup(c => c.AddResultsAsync(results, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        // Because the remote host wasn't fully loaded, we still notify that our results may be incomplete.
        callbackMock.Setup(c => c.Done(false));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, CancellationToken.None);
    }

    [Theory, CombinatorialData]
    public async Task NotFullyLoadedStillReportsAsNotCompleteIfRemoteHostIsStillHydrating(bool projectIsFullyLoaded)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");

        var pattern = "irrelevant";

        var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);

        // First call will pass in that we're not fully loaded.  If we return null, we should get another call with
        // the request to search the fully loaded data.  If we don't report anything the second time, we will still
        // tell the user the search was complete.
        SetupSearchProject(searchService, pattern, isFullyLoaded: false, results: []);
        SetupSearchProject(searchService, pattern, isFullyLoaded: true, results: []);

        // Simulate a host that says the solution isn't fully loaded.
        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectIsFullyLoaded, remoteHost: false));
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportIncomplete());
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));

        // Because the remote host wasn't fully loaded, we still notify that our results may be incomplete.
        callbackMock.Setup(c => c.Done(false));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, CancellationToken.None);
    }

    [Fact]
    public async Task FullyLoadedMakesSingleSearchProjectCallIfValueNotReturned()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");

        var pattern = "irrelevant";

        var results = ImmutableArray.Create<INavigateToSearchResult>(new TestNavigateToSearchResult(workspace, new TextSpan(0, 0)));

        var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);

        // First call will pass in that we're fully loaded.  If we return null, we should not get another call.
        SetupSearchProject(searchService, pattern, isFullyLoaded: true, results: []);

        // Simulate a host that says the solution is fully loaded.
        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystem: true, remoteHost: true));
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
        callbackMock.Setup(c => c.AddResultsAsync(results, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        // Because we did a full search, we should let the user know it was totally accurate.
        callbackMock.Setup(c => c.Done(true));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, CancellationToken.None);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1933220")]
    public async Task DoNotCrashWithoutSearchService()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");

        var pattern = "irrelevant";
        var results = ImmutableArray.Create<INavigateToSearchResult>(new TestNavigateToSearchResult(workspace, new TextSpan(0, 0)));

        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => new ValueTask<bool>(true));

        // Ensure that returning null for the search service doesn't crash.
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(() => null);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportIncomplete());
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
        callbackMock.Setup(c => c.AddResultsAsync(results, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        callbackMock.Setup(c => c.Done(true));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectScopeSearchingOnlySearchesSingleProjectForGeneratedDocuments()
    {
        using var workspace = EditorTestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="z:\\file1.cs">
                    public class C
                    {
                    }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document FilePath="z:\\file2.cs">
                    public class D
                    {
                    }
                    </Document>
                </Project>
            </Workspace>
            """, composition: FirstActiveAndVisibleComposition);

        var pattern = "irrelevant";
        var results = ImmutableArray.Create<INavigateToSearchResult>(new TestNavigateToSearchResult(workspace, new TextSpan(0, 0)));

        var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
        hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => new ValueTask<bool>(true));

        var searchGeneratedDocumentsAsyncCalled = false;
        var searchService = new MockAdvancedNavigateToSearchService
        {
            OnSearchGeneratedDocumentsAsyncCalled = () =>
            {
                Assert.False(searchGeneratedDocumentsAsyncCalled);
                searchGeneratedDocumentsAsyncCalled = true;
            }
        };

        // Ensure that returning null for the search service doesn't crash.
        hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(() => searchService);

        var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
        callbackMock.Setup(c => c.ReportIncomplete());
        callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
        callbackMock.Setup(c => c.AddResultsAsync(results, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        callbackMock.Setup(c => c.Done(true));

        var searcher = NavigateToSearcher.Create(
            workspace.CurrentSolution,
            callbackMock.Object,
            pattern,
            kinds: ImmutableHashSet<string>.Empty,
            hostMock.Object);

        // We're searching for a singular project, so we should only get a single call to search generated documents.
        await searcher.SearchAsync(NavigateToSearchScope.Project, CancellationToken.None);
        Assert.True(searchGeneratedDocumentsAsyncCalled);
    }

    private sealed class MockAdvancedNavigateToSearchService : IAdvancedNavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided => AbstractNavigateToSearchService.AllKinds;

        public bool CanFilter => true;

        public Action? OnSearchCachedDocumentsAsyncCalled { get; set; }
        public Action? OnSearchDocumentsAsyncCalled { get; set; }
        public Action? OnSearchGeneratedDocumentsAsyncCalled { get; set; }
        public Action? OnSearchProjectsAsyncCalled { get; set; }

        public Task SearchCachedDocumentsAsync(Solution solution, ImmutableArray<Project> projects, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, Func<Task> onProjectCompleted, CancellationToken cancellationToken)
        {
            OnSearchCachedDocumentsAsyncCalled?.Invoke();
            return Task.CompletedTask;
        }

        public Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, CancellationToken cancellationToken)
        {
            OnSearchDocumentsAsyncCalled?.Invoke();
            return Task.CompletedTask;
        }

        public Task SearchSourceGeneratedDocumentsAsync(Solution solution, ImmutableArray<Project> projects, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, Func<Task> onProjectCompleted, CancellationToken cancellationToken)
        {
            OnSearchGeneratedDocumentsAsyncCalled?.Invoke();
            return Task.CompletedTask;
        }

        public Task SearchProjectsAsync(Solution solution, ImmutableArray<Project> projects, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, bool searchGeneratedCode, Document? activeDocument, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, Func<Task> onProjectCompleted, CancellationToken cancellationToken)
        {
            OnSearchProjectsAsyncCalled?.Invoke();
            return Task.CompletedTask;
        }
    }

    private sealed class TestNavigateToSearchResult(EditorTestWorkspace workspace, TextSpan sourceSpan)
        : INavigateToSearchResult, INavigableItem
    {
        public INavigableItem.NavigableDocument Document => INavigableItem.NavigableDocument.FromDocument(workspace.CurrentSolution.Projects.Single().Documents.Single());
        public TextSpan SourceSpan => sourceSpan;

        public string AdditionalInformation => throw new NotImplementedException();
        public string Kind => throw new NotImplementedException();
        public NavigateToMatchKind MatchKind => throw new NotImplementedException();
        public bool IsCaseSensitive => throw new NotImplementedException();
        public string Name => throw new NotImplementedException();
        public ImmutableArray<TextSpan> NameMatchSpans => throw new NotImplementedException();
        public string SecondarySort => throw new NotImplementedException();
        public string Summary => throw new NotImplementedException();
        public INavigableItem NavigableItem => this;
        public Glyph Glyph => throw new NotImplementedException();
        public ImmutableArray<TaggedText> DisplayTaggedParts => throw new NotImplementedException();
        public bool DisplayFileLocation => throw new NotImplementedException();
        public bool IsImplicitlyDeclared => throw new NotImplementedException();
        public bool IsStale => throw new NotImplementedException();
        public ImmutableArray<INavigableItem> ChildItems => throw new NotImplementedException();
        public ImmutableArray<PatternMatch> Matches => NavigateToSearchResultHelpers.GetMatches(this);
    }
}
