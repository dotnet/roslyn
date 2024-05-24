// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal sealed class TaggerMainThreadManager
{
    private static readonly ConditionalWeakTable<IThreadingContext, TaggerMainThreadManager> s_table = new();

    public static TaggerMainThreadManager GetManager(IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider)
        => s_table.GetValue(threadingContext, _ => new TaggerMainThreadManager(threadingContext, listenerProvider));

    private readonly IThreadingContext _threadingContext;
    private readonly AsyncBatchingWorkQueue<(Action action, CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletionSource)> _workQueue;

    private TaggerMainThreadManager(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;

        _workQueue = new AsyncBatchingWorkQueue<(Action action, CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletionSource)>(
            DelayTimeSpan.NearImmediate,
            ProcessWorkItemsAsync,
            listenerProvider.GetListener(FeatureAttribute.Tagger),
            threadingContext.DisposalToken);
    }

    private static void RunActionAndUpdateCompletionSource(
        Action action,
        TaskCompletionSource<bool> taskCompletionSource)
    {
        try
        {
            // Run the underlying task.
            action();
            taskCompletionSource.TrySetResult(true);
        }
        catch (OperationCanceledException ex)
        {
            taskCompletionSource.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            taskCompletionSource.TrySetException(ex);
        }
    }

    /// <summary>
    /// Adds the provided action to a queue that will run on the UI thread in the near future (batched with other
    /// registered actions).  If the cancellation token is triggered before the action runs, it will not be run.
    /// </summary>
    public Task PerformWorkOnMainThreadAsync(Action action, CancellationToken cancellationToken)
    {
        var taskSource = new TaskCompletionSource<bool>();

        // If we're already on the main thread, just run the action directly without any delay.  This is important
        // for cases where the tagger is performing a blocking call to get tags synchronously on the UI thread (for
        // example, for determining collapsed outlining tags on document open).
        if (_threadingContext.JoinableTaskContext.IsOnMainThread)
        {
            RunActionAndUpdateCompletionSource(action, taskSource);
            var task = taskSource.Task;
            Contract.ThrowIfFalse(task.IsCompleted);
            return task;
        }

        // Ensure that if the host is closing and hte queue stops running that we transition this task to the canceled state.
        var registration = _threadingContext.DisposalToken.Register(static taskSourceObj => ((TaskCompletionSource<bool>)taskSourceObj!).TrySetCanceled(), taskSource);

        _workQueue.AddWork((action, cancellationToken, taskSource));

        // Ensure that when our work is done that we let go of the registered callback.
        return taskSource.Task.CompletesTrackingOperation(registration);
    }

    private async ValueTask ProcessWorkItemsAsync(
        ImmutableSegmentedList<(Action action, CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletionSource)> list,
        CancellationToken queueCancellationToken)
    {
        var nonCanceledActions = ImmutableSegmentedList.CreateBuilder<(Action action, CancellationToken cancellationToken, TaskCompletionSource<bool> taskCompletionSource)>();
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

        foreach (var (action, cancellationToken, taskCompletionSource) in list)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // If the work was already canceled, then just transition the task to the canceled state without
                // running the action.
                taskCompletionSource.TrySetCanceled(cancellationToken);
                continue;
            }

            // Run the user action.  This is the wrapped action created in PerformWorkOnMainThreadAsync, which will
            // not ever throw.
            try
            {
                RunActionAndUpdateCompletionSource(action, taskCompletionSource);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
            }
        }
    }
}
