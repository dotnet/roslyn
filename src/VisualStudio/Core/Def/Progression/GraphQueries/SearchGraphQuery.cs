// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal sealed partial class SearchGraphQuery(
    string searchPattern,
    NavigateToDocumentSupport searchScope) : IGraphQuery
{
    public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
    {
        var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);
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

    private sealed class SearchGraphQueryNavigateToSearchHost(bool isFullyLoaded) : INavigateToSearcherHost
    {
        public INavigateToSearchService? GetNavigateToSearchService(Project project)
            => project.GetLanguageService<INavigateToSearchService>();

        public ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            => new(isFullyLoaded);
    }
}
