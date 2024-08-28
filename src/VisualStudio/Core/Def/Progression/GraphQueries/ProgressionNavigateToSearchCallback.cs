// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.CodeAnalysis.NavigateTo;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal sealed partial class SearchGraphQuery
{
    private class ProgressionNavigateToSearchCallback : INavigateToSearchCallback
    {
        private readonly Solution _solution;
        private readonly IGraphContext _context;
        private readonly GraphBuilder _graphBuilder;

        public ProgressionNavigateToSearchCallback(Solution solution, IGraphContext context, GraphBuilder graphBuilder)
        {
            _solution = solution;
            _context = context;
            _graphBuilder = graphBuilder;
        }

        public void Done(bool isFullyLoaded)
        {
            // Do nothing here.  Even though the navigate to search completed, we still haven't passed any
            // information along to progression.  That will happen in GraphQueryManager.PopulateContextGraphAsync
        }

        public void ReportProgress(int current, int maximum)
            => _context.ReportProgress(current, maximum, null);

        public void ReportIncomplete()
        {
        }

        public async Task AddResultsAsync(ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            foreach (var result in results)
            {
                var node = await _graphBuilder.CreateNodeAsync(_solution, result, cancellationToken).ConfigureAwait(false);
                if (node != null)
                {
                    // _context.OutputNodes is not threadsafe.  So ensure only one navto callback can mutate it at a time.
                    lock (this)
                        _context.OutputNodes.Add(node);
                }
            }
        }
    }
}
