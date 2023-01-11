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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal class GraphQueryManager
    {
        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;

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
            _asyncListener = asyncListener;
            _updateQueue = new AsyncBatchingWorkQueue(
                DelayTimeSpan.Idle,
                UpdateExistingQueriesAsync,
                asyncListener,
                threadingContext.DisposalToken);

            _workspace.WorkspaceChanged += (_, _) => _updateQueue.AddWork();
        }

        public void AddQueries(IGraphContext context, ImmutableArray<IGraphQuery> graphQueries)
        {
            var asyncToken = _asyncListener.BeginAsyncOperation("GraphQueryManager.AddQueries");

            var solution = _workspace.CurrentSolution;

            var populateTask = PopulateContextGraphAsync(solution, context, graphQueries);

            // We want to ensure that no matter what happens, this initial context is completed
            var task = populateTask.SafeContinueWith(
                _ => context.OnCompleted(), context.CancelToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            if (context.TrackChanges)
            {
                task = task.SafeContinueWith(
                    _ => TrackChangesAfterFirstPopulate(context, graphQueries),
                    context.CancelToken, TaskContinuationOptions.None, TaskScheduler.Default);
            }

            task.CompletesAsyncOperation(asyncToken);
        }

        private void TrackChangesAfterFirstPopulate(IGraphContext context, ImmutableArray<IGraphQuery> graphQueries)
        {
            lock (_gate)
            {
                _trackedQueries = _trackedQueries.Add((new WeakReference<IGraphContext>(context), graphQueries));
            }

            _updateQueue.AddWork();
        }

        private async ValueTask UpdateExistingQueriesAsync(CancellationToken cancellationToken)
        {
            ImmutableArray<(IGraphContext context, ImmutableArray<IGraphQuery> queries)> liveQueries;
            lock (_gate)
            {
                liveQueries = _trackedQueries
                    .SelectAsArray(t => (t.context.GetTarget(), t.queries))
                    .WhereAsArray(t => t.Item1 != null)!;
            }

            if (liveQueries.Length == 0)
                return;

            var solution = _workspace.CurrentSolution;
            var tasks = liveQueries.Select(t => PopulateContextGraphAsync(solution, t.context, t.queries)).ToArray();
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                PostUpdate();
            }
        }

        private void PostUpdate()
        {
            lock (_gate)
            {
                // See if each context is still alive. It's possible it's already been GC'ed meaning we should stop caring about the query
                _trackedQueries = _trackedQueries.RemoveAll(t => !IsTrackingContext(t.context));
            }

            _updateQueue.AddWork();
        }

        private static bool IsTrackingContext(WeakReference<IGraphContext> weakContext)
        {
            var context = weakContext.GetTarget();
            return context != null && !context.CancelToken.IsCancellationRequested;
        }

        /// <summary>
        /// Populate the graph of the context with the values for the given Solution.
        /// </summary>
        private static async Task PopulateContextGraphAsync(
            Solution solution,
            IGraphContext context,
            ImmutableArray<IGraphQuery> graphQueries)
        {
            Contract.ThrowIfTrue(graphQueries.IsEmpty);
            var cancellationToken = context.CancelToken;

            try
            {
                // Compute all queries in parallel.  Then as each finishes, update the graph.

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
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, ErrorSeverity.Diagnostic))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
