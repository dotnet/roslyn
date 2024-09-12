// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.Progression;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal class GraphQueryManager
{
    private readonly Workspace _workspace;

    /// <summary>
    /// This gate locks manipulation of <see cref="_trackedQueries"/>.
    /// </summary>
    private readonly object _gate = new();
    private ImmutableArray<(WeakReference<IGraphContext> context, ImmutableArray<IGraphQuery> queries)> _trackedQueries = ImmutableArray<(WeakReference<IGraphContext>, ImmutableArray<IGraphQuery>)>.Empty;

    private readonly AsyncBatchingWorkQueue _updateQueue;

    internal GraphQueryManager(
        Workspace workspace,
        IThreadingContext threadingContext,
        IAsynchronousOperationListener asyncListener)
    {
        _workspace = workspace;

        // Update any existing live/tracking queries 1.5 seconds after every workspace changes.
        _updateQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Idle,
            UpdateExistingQueriesAsync,
            asyncListener,
            threadingContext.DisposalToken);

        // Note: this ends up always listening for workspace events, even if we have no active 'live' queries that
        // need updating.  But this should basically be practically no cost.  The queue just holds a single item
        // indicating a change happened.  And when UpdateExistingQueriesAsync fires, it will just see that there are
        // no live queries and immediately return.  So it's just simple to do things this way instead of trying to 
        // have state management where we try to decide if we should listen or not.
        _workspace.WorkspaceChanged += (_, _) => _updateQueue.AddWork();
    }

    public async Task AddQueriesAsync(IGraphContext context, ImmutableArray<IGraphQuery> graphQueries, CancellationToken disposalToken)
    {
        try
        {
            var solution = _workspace.CurrentSolution;

            // Perform the actual graph query first.
            await PopulateContextGraphAsync(solution, context, graphQueries, disposalToken).ConfigureAwait(false);

            // If this context would like to be continuously updated with live changes to this query, then add the
            // tracked query to our tracking list, keeping it alive as long as those is keeping the context alive.
            if (context.TrackChanges)
            {
                lock (_gate)
                {
                    _trackedQueries = _trackedQueries.Add((new WeakReference<IGraphContext>(context), graphQueries));
                }
            }
        }
        finally
        {
            // We want to ensure that no matter what happens, this initial context is completed
            context.OnCompleted();
        }
    }

    private async ValueTask UpdateExistingQueriesAsync(CancellationToken disposalToken)
    {
        ImmutableArray<(IGraphContext context, ImmutableArray<IGraphQuery> queries)> liveQueries;
        lock (_gate)
        {
            // First, grab the set of contexts that are still live.  We'll update them below.
            liveQueries = _trackedQueries
                .SelectAsArray(t => (context: t.context.GetTarget(), t.queries))
                .WhereAsArray(t => t.context != null)!;

            // Next, clear out any context that are now no longer alive (or have been canceled).  We no longer care
            // about these.
            _trackedQueries = _trackedQueries.RemoveAll(t =>
            {
                var target = t.context.GetTarget();
                return target is null || target.CancelToken.IsCancellationRequested;
            });
        }

        var solution = _workspace.CurrentSolution;

        // Update all the live queries in parallel.
        var tasks = liveQueries.Select(t => PopulateContextGraphAsync(solution, t.context, t.queries, disposalToken)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Populate the graph of the context with the values for the given Solution.
    /// </summary>
    private static async Task PopulateContextGraphAsync(
        Solution solution,
        IGraphContext context,
        ImmutableArray<IGraphQuery> graphQueries,
        CancellationToken disposalToken)
    {
        try
        {
            // Compute all queries in parallel.  Then as each finishes, update the graph.

            // Cancel the work if either the context wants us to cancel, or our host is getting disposed.
            using var source = CancellationTokenSource.CreateLinkedTokenSource(context.CancelToken, disposalToken);
            var cancellationToken = source.Token;

            var tasks = graphQueries.Select(q => Task.Run(() => q.GetGraphAsync(solution, context, cancellationToken), cancellationToken)).ToHashSet();

            var first = true;
            while (tasks.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(completedTask);

                // if this is the first task finished, clear out the existing results and add all the new
                // results as a single transaction.  Doing this as a single transaction is vital for
                // solution-explorer as that is how it can map the prior elements to the new ones, preserving the
                // view-state (like ensuring the same nodes stay collapsed/expanded).
                //
                // As additional queries finish, add those results in after without clearing the results of the
                // prior queries.

                var graphBuilder = await completedTask.ConfigureAwait(false);
                using var transaction = new GraphTransactionScope();

                if (first)
                {
                    first = false;
                    context.Graph.Links.Clear();
                }

                graphBuilder.ApplyToGraph(context.Graph, cancellationToken);
                context.OutputNodes.AddAll(graphBuilder.GetCreatedNodes(cancellationToken));

                transaction.Complete();
            }
        }
        catch (OperationCanceledException)
        {
            // Don't bubble this cancellation outwards.  The queue's cancellation token is mixed with the context's
            // token to make a final token that controls the work we do above.  We don't want any of the wrong
            // cancellations leaking outwards.
        }
        catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, ErrorSeverity.Diagnostic))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
