// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Workspaces
{
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
        /// Waits the specified amount of time while the specified <paramref name="subjectBuffer"/> is not visible.  If any
        /// document visibility changes happen, the delay will cancel.
        /// </summary>
        public static async Task DelayWhileNonVisibleAsync(
            this ITextBufferVisibilityTracker? service,
            IThreadingContext threadingContext,
            ITextBuffer subjectBuffer,
            TimeSpan timeSpan,
            CancellationToken cancellationToken)
        {
            // Only add a delay if we have access to a service that will tell us when the buffer become visible or not.
            if (service is null)
                return;

            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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
                var delayTask = Task.Delay(timeSpan, cancellationToken);
                await Task.WhenAny(delayTask, visibilityChangedTaskSource.Task).ConfigureAwait(false);
            }
            finally
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                service.UnregisterForVisibilityChanges(subjectBuffer, callback);
            }
        }
    }
}
