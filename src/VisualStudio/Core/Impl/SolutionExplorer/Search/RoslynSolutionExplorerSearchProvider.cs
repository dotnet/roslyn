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
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Entrypoint that exposes roslyn search capabilities from solution explorer.
/// </summary>
[AppliesToProject("CSharp | VB")]
[Export(typeof(ISearchProvider))]
[Name(nameof(RoslynSolutionExplorerSearchProvider))]
[Order(Before = "GraphSearchProvider")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RoslynSolutionExplorerSearchProvider(
    VisualStudioWorkspace workspace,
    IAsynchronousOperationListenerProvider listenerProvider,
    IThreadingContext threadingContext) : ISearchProvider
{
    private readonly VisualStudioWorkspace _workspace = workspace;
    private readonly IThreadingContext _threadingContext = threadingContext;
    public readonly SolutionExplorerNavigationSupport NavigationSupport = new(workspace, threadingContext, listenerProvider);

    public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
    {
        if (!parameters.Options.SearchFileContents)
            return;

        // Have to synchronously block on the search finishing as otherwise the caller will think we are
        // done prior to us reporting any results.
        _threadingContext.JoinableTaskFactory.Run(SearchAsync);

        async Task SearchAsync()
        {
            try
            {
                var solution = _workspace.CurrentSolution;
                var searcher = NavigateToSearcher.Create(
                    solution,
                    new SolutionExplorerNavigateToSearchCallback(this, resultAccumulator),
                    parameters.SearchQuery.SearchString.Trim(),
                    NavigateToUtilities.GetKindsProvided(solution),
                    new SolutionExplorerNavigateToSearcherHost(_workspace));

                await searcher.SearchAsync(NavigateToSearchScope.Solution, parameters.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        }
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
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            foreach (var result in results)
            {
                // Compute the name on the BG to avoid UI work.
                var name = result.NavigableItem.DisplayTaggedParts.JoinText();
                var imageMoniker = result.NavigableItem.Glyph.GetImageMoniker();
                resultAccumulator(new SolutionExplorerSearchResult(provider, result, name, imageMoniker));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class SolutionExplorerSearchResult(
        RoslynSolutionExplorerSearchProvider provider,
        INavigateToSearchResult result,
        string name,
        ImageMoniker imageMoniker) : ISearchResult
    {
        public object GetDisplayItem()
            => new SolutionExplorerSearchDisplayItem(provider, result, name, imageMoniker);
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
