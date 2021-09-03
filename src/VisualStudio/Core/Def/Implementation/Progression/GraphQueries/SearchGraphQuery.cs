﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed partial class SearchGraphQuery : IGraphQuery
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly string _searchPattern;

        public SearchGraphQuery(
            string searchPattern,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
        {
            _threadingContext = threadingContext;
            _asyncListener = asyncListener;
            _searchPattern = searchPattern;
        }

        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);
            var callback = new ProgressionNavigateToSearchCallback(context, graphBuilder);
            var searcher = NavigateToSearcher.Create(
                solution,
                _asyncListener,
                callback,
                _searchPattern,
                searchCurrentDocument: false,
                NavigateToUtilities.GetKindsProvided(solution),
                _threadingContext.DisposalToken);

            await searcher.SearchAsync(cancellationToken).ConfigureAwait(false);

            return graphBuilder;
        }
    }
}
