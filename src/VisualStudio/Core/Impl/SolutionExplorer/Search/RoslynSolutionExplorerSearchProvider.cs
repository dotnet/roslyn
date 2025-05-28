// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Entrypoint that exposes roslyn search capabilities from solution explorer.
/// </summary>
[AppliesToProject("CSharp | VB")]
[Export(typeof(ISearchProvider))]
[Name(nameof(RoslynSolutionExplorerSearchProvider))]
[Order(Before = "GraphSearchProvider")]
internal sealed class RoslynSolutionExplorerSearchProvider : ISearchProvider
{
    private readonly VisualStudioWorkspace _workspace;
    private readonly IAsynchronousOperationListener _listener;
    public readonly SolutionExplorerNavigationSupport NavigationSupport;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynSolutionExplorerSearchProvider(
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider,
        IThreadingContext threadingContext)
    {
        _workspace = workspace;
        _listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);
        NavigationSupport = new(workspace, threadingContext, _listener);
    }

    public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
    {
        if (!parameters.Options.SearchFileContents)
            return;

        var cancellationToken = parameters.CancellationToken;

        var solution = _workspace.CurrentSolution;
        var searcher = NavigateToSearcher.Create(
            solution,
            new SolutionExplorerNavigateToSearchCallback(this, resultAccumulator),
            parameters.SearchQuery.SearchString.Trim(),
            NavigateToUtilities.GetKindsProvided(solution),
            new SolutionExplorerNavigateToSearcherHost(_workspace));

        var token = _listener.BeginAsyncOperation(nameof(Search));
        searcher
            .SearchAsync(NavigateToSearchScope.Solution, cancellationToken)
            .ReportNonFatalErrorUnlessCancelledAsync(cancellationToken)
            .CompletesAsyncOperation(token);
    }

    private sealed class SolutionExplorerNavigateToSearchCallback(
        RoslynSolutionExplorerSearchProvider provider,
        Action<ISearchResult> resultAccumulator) : INavigateToSearchCallback
    {
        public void Done(bool isFullyLoaded) { }
        public void ReportIncomplete() { }
        public void ReportProgress(int current, int maximum) { }

        public Task AddResultsAsync(
            ImmutableArray<INavigateToSearchResult> results,
            CancellationToken cancellationToken)
        {
            foreach (var result in results)
                resultAccumulator(new SolutionExplorerSearchResult(provider, result));

            return Task.CompletedTask;
        }
    }

    private sealed class SolutionExplorerSearchResult(
        RoslynSolutionExplorerSearchProvider provider,
        INavigateToSearchResult result) : ISearchResult
    {
        public object GetDisplayItem()
        {
            var name = result.NavigableItem.DisplayTaggedParts.JoinText();
            return new SolutionExplorerSearchDisplayItem(
                provider, name, result);
        }
    }

    private sealed class SolutionExplorerNavigateToSearcherHost(Workspace workspace) : INavigateToSearcherHost
    {
        public INavigateToSearchService? GetNavigateToSearchService(Microsoft.CodeAnalysis.Project project)
            => project.GetLanguageService<INavigateToSearchService>();

        public async ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
        {
            var statusService = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await statusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            return isFullyLoaded;
        }
    }
}
