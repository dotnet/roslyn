// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal sealed class TaggerMainThreadManager(
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider)
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider = listenerProvider;

    private readonly ConcurrentDictionary<Type, StronglyTypedTaggerMainThreadManager> _stronglyTypedManagers = new();

    /// <summary>
    /// Adds the provided action to a queue that will run on the UI thread in the near future (batched with other
    /// registered actions).  If the cancellation token is triggered before the action runs, it will not be run.
    /// </summary>
    public async ValueTask<TResult> PerformWorkOnMainThreadAsync<TResult>(Func<TResult> action, CancellationToken cancellationToken)
    {
        var manager = (StronglyTypedTaggerMainThreadManager<TResult>)GetManager();

        return await manager.PerformWorkOnMainThreadAsync(action, cancellationToken).ConfigureAwait(true);

        StronglyTypedTaggerMainThreadManager GetManager()
        {
            if (_stronglyTypedManagers.TryGetValue(typeof(TResult), out var manager))
                return manager;

            return _stronglyTypedManagers.GetOrAdd(typeof(TResult), new StronglyTypedTaggerMainThreadManager<TResult>(_threadingContext, _listenerProvider));
        }
    }

    private abstract class StronglyTypedTaggerMainThreadManager
    {
    }

    private sealed class StronglyTypedTaggerMainThreadManager<TResult> : StronglyTypedTaggerMainThreadManager
    {
        private readonly IThreadingContext _threadingContext;
        private readonly AsyncBatchingWorkQueue<(Func<TResult> action, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource)> _workQueue;

        public StronglyTypedTaggerMainThreadManager(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;

            _workQueue = new AsyncBatchingWorkQueue<(Func<TResult> action, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource)>(
                DelayTimeSpan.NearImmediate,
                ProcessWorkItemsAsync,
                listenerProvider.GetListener(FeatureAttribute.Tagger),
                threadingContext.DisposalToken);
        }

        /// <remarks>This will not ever throw.</remarks>
        private static void RunActionAndUpdateCompletionSource_NoThrow(
            Func<TResult> action,
            TaskCompletionSource<TResult> taskCompletionSource)
        {
            try
            {
                // Run the underlying task.
                taskCompletionSource.SetResult(action());
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }
            finally
            {
                taskCompletionSource.TrySetResult(default!);
            }
        }

        /// <summary>
        /// Adds the provided action to a queue that will run on the UI thread in the near future (batched with other
        /// registered actions).  If the cancellation token is triggered before the action runs, it will not be run.
        /// </summary>
        public async ValueTask<TResult> PerformWorkOnMainThreadAsync(Func<TResult> action, CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<TResult>();

            // If we're already on the main thread, just run the action directly without any delay.  This is important
            // for cases where the tagger is performing a blocking call to get tags synchronously on the UI thread (for
            // example, for determining collapsed outlining tags on document open).
            if (_threadingContext.JoinableTaskContext.IsOnMainThread)
            {
                RunActionAndUpdateCompletionSource_NoThrow(action, taskSource);
                Contract.ThrowIfFalse(taskSource.Task.IsCompleted);
            }
            else
            {
                // Ensure that if the host is closing and hte queue stops running that we transition this task to the canceled state.
                var registration = _threadingContext.DisposalToken.Register(static taskSourceObj => ((TaskCompletionSource<VoidResult>)taskSourceObj!).TrySetCanceled(), taskSource);

                _workQueue.AddWork((action, cancellationToken, taskSource));

                // Ensure that when our work is done that we let go of the registered callback.
                _ = taskSource.Task.CompletesTrackingOperation(registration);
            }

            return await taskSource.Task.ConfigureAwait(true);
        }

        private async ValueTask ProcessWorkItemsAsync(
            ImmutableSegmentedList<(Func<TResult> action, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource)> list,
            CancellationToken queueCancellationToken)
        {
            var nonCanceledActions = ImmutableSegmentedList.CreateBuilder<(Func<TResult> action, CancellationToken cancellationToken, TaskCompletionSource<TResult> taskCompletionSource)>();
            foreach (var (action, cancellationToken, taskCompletionSource) in list)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // If the work was already canceled, then just transition the task to the canceled state without
                    // running the action.
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                    continue;
                }

                nonCanceledActions.Add((action, cancellationToken, taskCompletionSource));
            }

            // No need to do anything if all the requested work was canceled.
            if (nonCanceledActions.Count == 0)
                return;

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(queueCancellationToken);

            foreach (var (action, cancellationToken, taskCompletionSource) in nonCanceledActions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // If the work was already canceled, then just transition the task to the canceled state without
                    // running the action.
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                    continue;
                }

                // Run the user action, completing the task completion source as appropriate. This will not ever throw.
                RunActionAndUpdateCompletionSource_NoThrow(action, taskCompletionSource);
                Contract.ThrowIfFalse(taskCompletionSource.Task.IsCompleted);
            }
        }
    }
}
