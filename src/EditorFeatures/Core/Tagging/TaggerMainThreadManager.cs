// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

using TaggerUIData = (bool isVisible, Microsoft.VisualStudio.Text.SnapshotPoint? caretPosition, Roslyn.Utilities.OneOrMany<Microsoft.VisualStudio.Text.SnapshotSpan> spansToTag);

namespace Microsoft.CodeAnalysis.Editor.Tagging;

using QueueData = (Func<TaggerUIData> action, TaskCompletionSource<TaggerUIData> taskCompletionSource, CancellationToken cancellationToken);

internal sealed class TaggerMainThreadManager
{
    private readonly IThreadingContext _threadingContext;
    private readonly AsyncBatchingWorkQueue<QueueData> _workQueue;

    public TaggerMainThreadManager(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;

        _workQueue = new AsyncBatchingWorkQueue<QueueData>(
            DelayTimeSpan.NearImmediate,
            ProcessWorkItemsAsync,
            listenerProvider.GetListener(FeatureAttribute.Tagger),
            threadingContext.DisposalToken);
    }

    /// <remarks>This will not ever throw.</remarks>
    private static void RunActionAndUpdateCompletionSource_NoThrow(
        Func<TaggerUIData> action,
        TaskCompletionSource<TaggerUIData> taskCompletionSource,
        CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(cancellationToken);
                return;
            }

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
            taskCompletionSource.TrySetResult(default);
            Contract.ThrowIfFalse(taskCompletionSource.Task.IsCompleted);
        }
    }

    /// <summary>
    /// Adds the provided action to a queue that will run on the UI thread in the near future (batched with other
    /// registered actions).  If the cancellation token is triggered before the action runs, it will not be run.
    /// </summary>
    public async ValueTask<TaggerUIData> PerformWorkOnMainThreadAsync(Func<TaggerUIData> action, CancellationToken cancellationToken)
    {
        var taskSource = new TaskCompletionSource<TaggerUIData>();

        // If we're already on the main thread, just run the action directly without any delay.  This is important
        // for cases where the tagger is performing a blocking call to get tags synchronously on the UI thread (for
        // example, for determining collapsed outlining tags on document open).
        if (_threadingContext.JoinableTaskContext.IsOnMainThread)
        {
            RunActionAndUpdateCompletionSource_NoThrow(action, taskSource, cancellationToken);
        }
        else
        {
            // Ensure that if the host is closing and hte queue stops running that we transition this task to the canceled state.
            var registration = _threadingContext.DisposalToken.Register(
                static taskSourceObj => ((TaskCompletionSource<TaggerUIData>)taskSourceObj!).TrySetCanceled(), taskSource);

            _workQueue.AddWork((action, taskSource, cancellationToken));

            // Ensure that when our work is done that we let go of the registered callback.
            _ = taskSource.Task.CompletesTrackingOperation(registration);
        }

        return await taskSource.Task.ConfigureAwait(true);
    }

    private async ValueTask ProcessWorkItemsAsync(ImmutableSegmentedList<QueueData> list, CancellationToken queueCancellationToken)
    {
        var hasMainThreadWorkToDo = false;
        foreach (var (action, taskCompletionSource, cancellationToken) in list)
        {
            // If the work was already canceled, then just transition the task to the canceled state without running the action.
            if (cancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(cancellationToken);
                continue;
            }

            // Otherwise, we will have to go to the main thread to execute some of these.
            hasMainThreadWorkToDo = true;
        }

        if (!hasMainThreadWorkToDo)
            return;

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(queueCancellationToken);

        foreach (var (action, taskCompletionSource, cancellationToken) in list)
            RunActionAndUpdateCompletionSource_NoThrow(action, taskCompletionSource, cancellationToken);
    }
}
