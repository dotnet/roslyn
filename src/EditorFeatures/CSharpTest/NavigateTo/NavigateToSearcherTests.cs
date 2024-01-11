// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Moq.Language.Flow;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.NavigateTo)]
    public class NavigateToSearcherTests
    {
        private static void SetupSearchProject(
            Mock<IAdvancedNavigateToSearchService> searchService,
            string pattern,
            bool isFullyLoaded,
            INavigateToSearchResult? result)
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
                    It.IsAny<Document?>(),
                    It.IsAny<Func<Project, INavigateToSearchResult, Task>>(),
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>())).Callback(
                    (Solution solution,
                     ImmutableArray<Project> projects,
                     ImmutableArray<Document> priorityDocuments,
                     string pattern,
                     IImmutableSet<string> kinds,
                     Document? activeDocument,
                     Func<Project, INavigateToSearchResult, Task> onResultFound,
                     Func<Task> onProjectCompleted,
                     CancellationToken cancellationToken) =>
                    {
                        if (result != null)
                            onResultFound(null!, result);
                    }).Returns(Task.CompletedTask);

                searchService.Setup(ss => ss.SearchGeneratedDocumentsAsync(
                    It.IsAny<Solution>(),
                    It.IsAny<ImmutableArray<Project>>(),
                    pattern,
                    ImmutableHashSet<string>.Empty,
                    It.IsAny<Document?>(),
                    It.IsAny<Func<Project, INavigateToSearchResult, Task>>(),
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>())).Callback(
                    (Solution solution,
                     ImmutableArray<Project> projects,
                     string pattern,
                     IImmutableSet<string> kinds,
                     Document? activeDocument,
                     Func<Project, INavigateToSearchResult, Task> onResultFound,
                     Func<Task> onProjectCompleted,
                     CancellationToken cancellationToken) =>
                    {
                        if (result != null)
                            onResultFound(null!, result);
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
                    It.IsAny<Func<Project, INavigateToSearchResult, Task>>(),
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>())).Callback(
                    (Solution solution,
                     ImmutableArray<Project> projects,
                     ImmutableArray<Document> priorityDocuments,
                     string pattern2,
                     IImmutableSet<string> kinds,
                     Document? activeDocument,
                     Func<Project, INavigateToSearchResult, Task> onResultFound2,
                     Func<Task> onProjectCompleted,
                     CancellationToken cancellationToken) =>
                    {
                        if (result != null)
                            onResultFound2(null!, result);
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

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result);

            // Simulate a host that says the solution isn't fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystem: false, remoteHost: false));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportIncomplete());
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Because we returned a result when not fully loaded, we should notify the user that data was not complete.
            callbackMock.Setup(c => c.Done(false));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                kinds: ImmutableHashSet<string>.Empty,
                hostMock.Object);

            await searcher.SearchAsync(searchCurrentDocument: false, CancellationToken.None);
        }

        [Theory, CombinatorialData]
        public async Task NotFullyLoadedMakesTwoSearchProjectCallIfValueNotReturned(bool projectSystemFullyLoaded)
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);

            // First call will pass in that we're not fully loaded.  If we return null, we should get
            // another call with the request to search the fully loaded data.
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result: null);
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result);

            // Simulate a host that says the solution isn't fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystemFullyLoaded, remoteHost: false));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportIncomplete());
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            // Because the remote host wasn't fully loaded, we still notify that our results may be incomplete.
            callbackMock.Setup(c => c.Done(false));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                kinds: ImmutableHashSet<string>.Empty,
                hostMock.Object);

            await searcher.SearchAsync(searchCurrentDocument: false, CancellationToken.None);
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
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result: null);
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result: null);

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
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                kinds: ImmutableHashSet<string>.Empty,
                hostMock.Object);

            await searcher.SearchAsync(searchCurrentDocument: false, CancellationToken.None);
        }

        [Fact]
        public async Task FullyLoadedMakesSingleSearchProjectCallIfValueNotReturned()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<IAdvancedNavigateToSearchService>(MockBehavior.Strict);

            // First call will pass in that we're fully loaded.  If we return null, we should not get another call.
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result: null);

            // Simulate a host that says the solution is fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => IsFullyLoadedAsync(projectSystem: true, remoteHost: true));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            // Because we did a full search, we should let the user know it was totally accurate.
            callbackMock.Setup(c => c.Done(true));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                kinds: ImmutableHashSet<string>.Empty,
                hostMock.Object);

            await searcher.SearchAsync(searchCurrentDocument: false, CancellationToken.None);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1933220")]
        public async Task DoNotCrashWithoutSearchService()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";
            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(() => new ValueTask<bool>(true));

            // Ensure that returning null for the search service doesn't crash.
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(() => null);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportIncomplete());
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            callbackMock.Setup(c => c.Done(true));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                kinds: ImmutableHashSet<string>.Empty,
                hostMock.Object);

            await searcher.SearchAsync(searchCurrentDocument: false, CancellationToken.None);
        }

        private class TestNavigateToSearchResult(EditorTestWorkspace workspace, TextSpan sourceSpan)
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
}
