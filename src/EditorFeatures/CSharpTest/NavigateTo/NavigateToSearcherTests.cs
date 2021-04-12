﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Moq.Language.Flow;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.NavigateTo)]
    public class NavigateToSearcherTests
    {
        private static IReturnsResult<INavigateToSearchService> SetupSearchProject(
            Mock<INavigateToSearchService> searchService,
            string pattern,
            bool isFullyLoaded,
            INavigateToSearchResult? result)
        {
            var searchServiceSetup = searchService.Setup(ss => ss.SearchProjectAsync(
                It.IsAny<Project>(),
                It.IsAny<ImmutableArray<Document>>(),
                pattern,
                ImmutableHashSet<string>.Empty,
                It.IsAny<Func<INavigateToSearchResult, Task>>(),
                isFullyLoaded,
                It.IsAny<CancellationToken>()));

            var searchServiceCallback = searchServiceSetup.Callback(
                (Project project,
                 ImmutableArray<Document> priorityDocuments,
                 string pattern2,
                 IImmutableSet<string> kinds,
                 Func<INavigateToSearchResult, Task> onResultFound2,
                 bool isFullyLoaded2,
                 CancellationToken cancellationToken) =>
                {
                    if (result != null)
                        onResultFound2(result);
                });

            return searchServiceCallback.ReturnsAsync(isFullyLoaded ? NavigateToSearchLocation.Latest : NavigateToSearchLocation.Cache);
        }

        private static ValueTask<(bool projectSystem, bool remoteHost)> IsFullyLoadedAsync(bool projectSystem, bool remoteHost)
            => new((projectSystem, remoteHost));

        [Fact]
        public async Task NotFullyLoadedOnlyMakesOneSearchProjectCallIfValueReturned()
        {
            using var workspace = TestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<INavigateToSearchService>(MockBehavior.Strict);
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result);

            // Simulate a host that says the solution isn't fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(IsFullyLoadedAsync(projectSystem: false, remoteHost: false));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Because we returned a result when not fully loaded, we should notify the user that data was not complete.
            callbackMock.Setup(c => c.Done(false));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                searchCurrentDocument: false,
                kinds: ImmutableHashSet<string>.Empty,
                CancellationToken.None,
                hostMock.Object);

            await searcher.SearchAsync(CancellationToken.None);
        }

        [Theory]
        [CombinatorialData]
        public async Task NotFullyLoadedMakesTwoSearchProjectCallIfValueNotReturned(bool projectSystemFullyLoaded)
        {
            using var workspace = TestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<INavigateToSearchService>(MockBehavior.Strict);

            // First call will pass in that we're not fully loaded.  If we return null, we should get
            // another call with the request to search the fully loaded data.
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result: null);
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result);

            // Simulate a host that says the solution isn't fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(IsFullyLoadedAsync(projectSystemFullyLoaded, remoteHost: false));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));
            callbackMock.Setup(c => c.AddItemAsync(It.IsAny<Project>(), result, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);

            // Because we did a full search, the accuracy is dependent on it the project system was fully loaded or not.
            callbackMock.Setup(c => c.Done(projectSystemFullyLoaded));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                searchCurrentDocument: false,
                kinds: ImmutableHashSet<string>.Empty,
                CancellationToken.None,
                hostMock.Object);

            await searcher.SearchAsync(CancellationToken.None);
        }

        [Theory]
        [CombinatorialData]
        public async Task NotFullyLoadedStillReportsAsFullyCompletedIfSecondCallReturnsNothing(bool projectIsFullyLoaded)
        {
            using var workspace = TestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var searchService = new Mock<INavigateToSearchService>(MockBehavior.Strict);

            // First call will pass in that we're not fully loaded.  If we return null, we should get another call with
            // the request to search the fully loaded data.  If we don't report anything the second time, we will still
            // tell the user the search was complete.
            SetupSearchProject(searchService, pattern, isFullyLoaded: false, result: null);
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result: null);

            // Simulate a host that says the solution isn't fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(IsFullyLoadedAsync(projectIsFullyLoaded, remoteHost: false));
            hostMock.Setup(h => h.GetNavigateToSearchService(It.IsAny<Project>())).Returns(searchService.Object);

            var callbackMock = new Mock<INavigateToSearchCallback>(MockBehavior.Strict);
            callbackMock.Setup(c => c.ReportProgress(It.IsAny<int>(), It.IsAny<int>()));

            // Because we did a full search, the accuracy is dependent on it the project system was fully loaded or not.
            callbackMock.Setup(c => c.Done(projectIsFullyLoaded));

            var searcher = NavigateToSearcher.Create(
                workspace.CurrentSolution,
                AsynchronousOperationListenerProvider.NullListener,
                callbackMock.Object,
                pattern,
                searchCurrentDocument: false,
                kinds: ImmutableHashSet<string>.Empty,
                CancellationToken.None,
                hostMock.Object);

            await searcher.SearchAsync(CancellationToken.None);
        }

        [Fact]
        public async Task FullyLoadedMakesSingleSearchProjectCallIfValueNotReturned()
        {
            using var workspace = TestWorkspace.CreateCSharp("");

            var pattern = "irrelevant";

            var result = new TestNavigateToSearchResult(workspace, new TextSpan(0, 0));

            var searchService = new Mock<INavigateToSearchService>(MockBehavior.Strict);

            // First call will pass in that we're fully loaded.  If we return null, we should not get another call.
            SetupSearchProject(searchService, pattern, isFullyLoaded: true, result: null);

            // Simulate a host that says the solution is fully loaded.
            var hostMock = new Mock<INavigateToSearcherHost>(MockBehavior.Strict);
            hostMock.Setup(h => h.IsFullyLoadedAsync(It.IsAny<CancellationToken>())).Returns(IsFullyLoadedAsync(projectSystem: true, remoteHost: true));
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
                searchCurrentDocument: false,
                kinds: ImmutableHashSet<string>.Empty,
                CancellationToken.None,
                hostMock.Object);

            await searcher.SearchAsync(CancellationToken.None);
        }

        private class TestNavigateToSearchResult : INavigateToSearchResult, INavigableItem
        {
            private readonly TestWorkspace _workspace;
            private readonly TextSpan _sourceSpan;

            public TestNavigateToSearchResult(TestWorkspace workspace, TextSpan sourceSpan)
            {
                _workspace = workspace;
                _sourceSpan = sourceSpan;
            }

            public Document Document => _workspace.CurrentSolution.Projects.Single().Documents.Single();
            public TextSpan SourceSpan => _sourceSpan;

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
        }
    }
}
