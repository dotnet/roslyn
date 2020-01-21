// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.GraphModel;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal class GraphQueryManager
    {
        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>
        /// This gate locks manipulation of trackedQueries.
        /// </summary>
        private readonly object _gate = new object();
        private readonly List<ValueTuple<WeakReference<IGraphContext>, List<IGraphQuery>>> _trackedQueries = new List<ValueTuple<WeakReference<IGraphContext>, List<IGraphQuery>>>();

        // We update all of our tracked queries when this delay elapses.
        private ResettableDelay _delay;

        internal GraphQueryManager(Workspace workspace, IAsynchronousOperationListener asyncListener)
        {
            _workspace = workspace;
            _asyncListener = asyncListener;
        }

        internal void AddQueries(IGraphContext context, List<IGraphQuery> graphQueries)
        {
            var asyncToken = _asyncListener.BeginAsyncOperation("GraphQueryManager.AddQueries");

            var solution = _workspace.CurrentSolution;

            var populateTask = PopulateContextGraphAsync(solution, graphQueries, context);

            // We want to ensure that no matter what happens, this initial context is completed
            var task = populateTask.SafeContinueWith(
                _ => context.OnCompleted(), context.CancelToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            if (context.TrackChanges)
            {
                task = task.SafeContinueWith(
                    _ => TrackChangesAfterFirstPopulate(graphQueries, context, solution),
                    context.CancelToken, TaskContinuationOptions.None, TaskScheduler.Default);
            }

            task.CompletesAsyncOperation(asyncToken);
        }

        private void TrackChangesAfterFirstPopulate(List<IGraphQuery> graphQueries, IGraphContext context, Solution solution)
        {
            var workspace = solution.Workspace;
            var contextWeakReference = new WeakReference<IGraphContext>(context);

            lock (_gate)
            {
                if (_trackedQueries.IsEmpty())
                {
                    _workspace.WorkspaceChanged += OnWorkspaceChanged;
                }

                _trackedQueries.Add(ValueTuple.Create(contextWeakReference, graphQueries));
            }

            EnqueueUpdateIfSolutionIsStale(solution);
        }

        private void EnqueueUpdateIfSolutionIsStale(Solution solution)
        {
            // It's possible the workspace changed during our initial population, so let's enqueue an update if it did
            if (_workspace.CurrentSolution != solution)
            {
                EnqueueUpdate();
            }
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            EnqueueUpdate();
        }

        private void EnqueueUpdate()
        {
            const int WorkspaceUpdateDelay = 1500;
            var delay = _delay;
            if (delay == null)
            {
                var newDelay = new ResettableDelay(WorkspaceUpdateDelay, _asyncListener);
                if (Interlocked.CompareExchange(ref _delay, newDelay, null) == null)
                {
                    var asyncToken = _asyncListener.BeginAsyncOperation("WorkspaceGraphQueryManager.EnqueueUpdate");
                    newDelay.Task.SafeContinueWithFromAsync(_ => UpdateAsync(), CancellationToken.None, TaskScheduler.Default)
                        .CompletesAsyncOperation(asyncToken);
                }

                return;
            }

            delay.Reset();
        }

        private Task UpdateAsync()
        {
            List<ValueTuple<IGraphContext, List<IGraphQuery>>> liveQueries;
            lock (_gate)
            {
                liveQueries = _trackedQueries.Select(t => ValueTuple.Create(t.Item1.GetTarget(), t.Item2)).Where(t => t.Item1 != null).ToList();
            }

            var solution = _workspace.CurrentSolution;
            var tasks = liveQueries.Select(t => PopulateContextGraphAsync(solution, t.Item2, t.Item1)).ToArray();
            var whenAllTask = Task.WhenAll(tasks);

            return whenAllTask.SafeContinueWith(t => PostUpdate(solution), TaskScheduler.Default);
        }

        private void PostUpdate(Solution solution)
        {
            _delay = null;
            lock (_gate)
            {
                // See if each context is still alive. It's possible it's already been GC'ed meaning we should stop caring about the query
                _trackedQueries.RemoveAll(t => !IsTrackingContext(t.Item1));
                if (_trackedQueries.IsEmpty())
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                    return;
                }
            }

            EnqueueUpdateIfSolutionIsStale(solution);
        }

        private static bool IsTrackingContext(WeakReference<IGraphContext> weakContext)
        {
            var context = weakContext.GetTarget();
            return context != null && !context.CancelToken.IsCancellationRequested;
        }

        /// <summary>
        /// Populate the graph of the context with the values for the given Solution.
        /// </summary>
        private static Task PopulateContextGraphAsync(Solution solution, List<IGraphQuery> graphQueries, IGraphContext context)
        {
            var cancellationToken = context.CancelToken;
            var graphTasks = graphQueries.Select(q => q.GetGraphAsync(solution, context, cancellationToken)).ToArray();
            var whenAllTask = Task.WhenAll(graphTasks);
            return whenAllTask.SafeContinueWith(
                t => ProcessGraphTasks(t.Result, context),
                cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
        }

        private static void ProcessGraphTasks(GraphBuilder[] graphBuilders, IGraphContext context)
        {
            // Perform the actual graph transaction 
            using var transaction = new GraphTransactionScope();
            // Remove any links that may have been added by a previous population. We don't
            // remove nodes to maintain node identity, matching the behavior of the old
            // providers.
            context.Graph.Links.Clear();

            foreach (var graphBuilder in graphBuilders)
            {
                graphBuilder.ApplyToGraph(context.Graph);

                context.OutputNodes.AddAll(graphBuilder.CreatedNodes);
            }

            transaction.Complete();
        }
    }
}
