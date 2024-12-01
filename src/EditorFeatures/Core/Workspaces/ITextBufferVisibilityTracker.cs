// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Workspaces;

/// <summary>
/// All methods must be called on UI thread.
/// </summary>
internal interface ITextBufferVisibilityTracker
{
    /// <summary>
    /// Whether or not this text buffer is in an actively visible <see cref="ITextView"/>.
    /// </summary>
    bool IsVisible(ITextBuffer subjectBuffer);

    /// <summary>
    /// Registers to hear about visibility changes for this particular buffer.  Note: registration will not trigger
    /// a call to <paramref name="callback"/>.  If clients need that information, they should check the <see
    /// cref="IsVisible"/> state of the <paramref name="subjectBuffer"/> themselves.
    /// </summary>
    void RegisterForVisibilityChanges(ITextBuffer subjectBuffer, Action callback);

    /// <summary>
    /// Unregister equivalent of <see cref="RegisterForVisibilityChanges"/>.
    /// </summary>
    void UnregisterForVisibilityChanges(ITextBuffer subjectBuffer, Action callback);
}

internal static class ITextBufferVisibilityTrackerExtensions
{
    /// <summary>
    /// Waits the specified amount of time while the specified <paramref name="subjectBuffer"/> is not visible.  If
    /// any document visibility changes happen, the delay will cancel.
    /// </summary>
    public static Task DelayWhileNonVisibleAsync(
        this ITextBufferVisibilityTracker? service,
        IThreadingContext threadingContext,
        IAsynchronousOperationListener listener,
        ITextBuffer subjectBuffer,
        TimeSpan timeSpan,
        CancellationToken cancellationToken)
    {
        // Only add a delay if we have access to a service that will tell us when the buffer become visible or not.
        if (service is null)
            return Task.CompletedTask;

        // Because cancellation is both expensive, and a super common thing to occur while we're delaying the caller
        // until visibility, we special case the implementation here and transition to the canceled state
        // explicitly, rather than throwing a cancellation exception.

        var delayTask = DelayWhileNonVisibleWorkerAsync();

        // it's very reasonable for the delay-task to complete synchronously (we've already been canceled, or the
        // buffer is already visible.  So fast path that out.
        if (delayTask.IsCompleted)
            return delayTask;

        var taskOfTask = delayTask.ContinueWith(
            // Convert a successfully completed task when we were canceled to a canceled task.  Otherwise, return
            // the faulted or non-canceled task as is.
            task => task.Status == TaskStatus.RanToCompletion && cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : task,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return taskOfTask.Unwrap();

        // Normal delay logic, except that this does not throw in the event of cancellation, but instead returns
        // gracefully.  The above task continuation logic then ensures we return a canceled task without needing
        // exceptions.
        async Task DelayWhileNonVisibleWorkerAsync()
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken).NoThrowAwaitable();
            if (cancellationToken.IsCancellationRequested)
                return;

            if (service.IsVisible(subjectBuffer))
                return;

            // ensure we listen for visibility changes before checking.  That way we don't have a race where we check
            // something see it is not visible, but then do not hear about its visibility change because we've hooked up
            // our event after that happens.
            var visibilityChangedTaskSource = new TaskCompletionSource<bool>();
            var callback = void () => visibilityChangedTaskSource.TrySetResult(true);
            service.RegisterForVisibilityChanges(subjectBuffer, callback);

            try
            {
                // Listen to when the active document changed so that we startup work on a document once it becomes visible.
                var delayTask = listener.Delay(timeSpan, cancellationToken);
                await Task.WhenAny(delayTask, visibilityChangedTaskSource.Task).NoThrowAwaitable(captureContext: true);
            }
            finally
            {
                service.UnregisterForVisibilityChanges(subjectBuffer, callback);
            }
        }
    }
}
