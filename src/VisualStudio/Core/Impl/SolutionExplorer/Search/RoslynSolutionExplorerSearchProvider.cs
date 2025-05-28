// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[AppliesToProject("CSharp | VB")]
[Export(typeof(ISearchProvider))]
[Name("DependenciesTreeSearchProvider")]
[VisualStudio.Utilities.Order(Before = "GraphSearchProvider")]
internal sealed class RoslynSolutionExplorerSearchProvider : ISearchProvider
{
    private readonly VisualStudioWorkspace _workspace;
    private readonly IVsHierarchyItemManager _hierarchyItemManager;
    private readonly IAsynchronousOperationListener _listener;
    private readonly SymbolTreeNavigationSupport _navigationSupport;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynSolutionExplorerSearchProvider(
        VisualStudioWorkspace workspace,
        IVsHierarchyItemManager hierarchyItemManager,
        IAsynchronousOperationListenerProvider listenerProvider,
        IThreadingContext threadingContext)
    {
        _workspace = workspace;
        _hierarchyItemManager = hierarchyItemManager;
        _listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);
        _navigationSupport = new(workspace, threadingContext, _listener);
    }

    public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
    {
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

    private sealed class SolutionExplorerSearchDisplayItem(
        RoslynSolutionExplorerSearchProvider provider,
        string name,
        INavigateToSearchResult result)
        : BaseItem,
        IInvocationController
    {
        private readonly INavigateToSearchResult _result = result;

        public override string Name { get; } = name;
        public override ImageMoniker IconMoniker => _result.NavigableItem.Glyph.GetImageMoniker();

        public override IInvocationController? InvocationController => this;

        public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
        {
            if (items.FirstOrDefault() is not SolutionExplorerSearchDisplayItem displayItem)
                return false;

            provider._navigationSupport.NavigateTo(
                displayItem._result.NavigableItem.Document.Id,
                displayItem._result.NavigableItem.SourceSpan.Start,
                preview: true);
            return true;
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

#if false
        var callback = new ProgressionNavigateToSearchCallback(solution, context, graphBuilder);

        // We have a specialized host for progression vs normal nav-to.  Progression itself will tell the client if
        // the project is fully loaded or not.  But after that point, the client will be considered fully loaded and
        // results should reflect that.  So we create a host here that will always give complete results once the
        // solution is loaded and not give cached/incomplete results at that point.
        var statusService = solution.Services.GetRequiredService<IWorkspaceStatusService>();
        var isFullyLoaded = await statusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
        var host = new SearchGraphQueryNavigateToSearchHost(isFullyLoaded);

        var searcher = NavigateToSearcher.Create(
            solution,
            callback,
            searchPattern,
            NavigateToUtilities.GetKindsProvided(solution),
            host);

        await searcher.SearchAsync(NavigateToSearchScope.Solution, searchScope, cancellationToken).ConfigureAwait(false);

        return graphBuilder;
    }

#endif
}
