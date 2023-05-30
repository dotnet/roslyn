// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Helper type that is used by all <see cref="AsynchronousTaggerProvider{TTag}"/>s to coordinator sending messages
    /// to the UI thread to do work.  The main problem this wants to avoid is N taggers waking up at different times,
    /// sending UI thread messages both to collect UI information, and then UI messages to notify about updated tags.
    /// By utilizing a shared <see cref="TaggerThreadCoordinator"/> all the taggers can instead enqueue that work into 
    /// a shared queue, which then executes as many of those UI items in the same UI message as long as they fit within
    /// the same timeslice.
    /// </summary>
    [Export(typeof(TaggerThreadCoordinator)), Shared]
    internal sealed class TaggerThreadCoordinator
    {
        /// <summary>
        /// Single piece of work that a tagger would like to perform on the UI thread.  Contains the actual callback
        /// function to execute the tagger work, and all the extract information we need to track that and map that work
        /// into TPL space.  This is effectively a "cold" Task, that will appear to the rest of the system as a normal TPL
        /// hot task.
        /// </summary>
        private readonly struct TaggerWork
        {
            /// <summary>
            /// The actual tagger specific work to do.
            /// </summary>
            private readonly Func<CancellationToken, Task> _work;

            /// <summary>
            /// Original cancellation token controlling the work the tagger needs to do.
            /// </summary>
            private readonly CancellationToken _cancellationToken;

            /// <summary>
            /// Completion source tracking the overall async lifetime of <see cref="_work"/>, including the time spent
            /// within the <see cref="TaggerThreadCoordinator"/>.  The coordinator itself does not block on <see
            /// cref="_work"/> executing, so this lets the owning tagger still know that it is occurring, and that it
            /// can itself await that work.
            /// </summary>
            private readonly TaskCompletionSource<bool> _completionSource;

            /// <summary>
            /// Registration to hear about if the work it canceled before we get around to executing it.  This allows
            /// the tagger itself to hear about the cancellation and respond to it immediatel.  Disposed once we finally
            /// remove this work item from the coordinator's queue.
            /// </summary>
            private readonly CancellationTokenRegistration _registration;

            public TaggerWork(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
            {
                _work = work;
                _cancellationToken = cancellationToken;
                var completionSource = new TaskCompletionSource<bool>();
                _completionSource = completionSource;
                _registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
            }

            public Task Task => _completionSource.Task;

            /// <summary>
            /// Go and execute the desired tagger work once the coordinator wakes up and attempts to execute a batch of items.
            /// </summary>
            public void PerformWork()
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    // Reasonable chance the tagger was canceled between when it enqueued work and when we're running.
                    // Just clear it out and return.  Fast path that to avoid any actual async/await task work.
                    _completionSource.TrySetCanceled(_cancellationToken);
                    _registration.Dispose();
                }
                else
                {
                    // Otherwise, kick off the work and have is signal to the originator (through the completion source)
                    // when it is finished.  We do this in a fire-and-forget fashion.  The async tagging work the queue
                    // is doing shouldn't impact the ability for the threading coordinator to move to the next batch of
                    // work (once the tagging work moves to the bg).
                    _ = PerformWorkAsync();
                }
            }

            private async Task PerformWorkAsync()
            {
                try
                {
                    // Go and actually do the work, forwarding the progress to the completion source to let the tagger know when finised.
                    await _work(_cancellationToken).ConfigureAwait(true);
                    _completionSource.TrySetResult(true);
                }
                catch (OperationCanceledException ex)
                {
                    _completionSource.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    _completionSource.TrySetException(ex);
                }
                finally
                {
                    // Final cleanup so we don't leak registrations.
                    _registration.Dispose();
                }
            }
        }

        /// <summary>
        /// Context we use to get back to the UI thread, and to know if the host is shutting down.
        /// </summary>
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Queue that batches up all the work we've been requested to do so we can execute all of it at once in one
        /// single UI thread message.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<TaggerWork> _queue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TaggerThreadCoordinator(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;

            _queue = new AsyncBatchingWorkQueue<TaggerWork>(
                TaggerDelay.NearImmediate.ComputeTimeDelay(),
                ProcessActionsAsync,
                listenerProvider.GetListener(FeatureAttribute.Tagging),
                threadingContext.DisposalToken);
        }

        private async ValueTask ProcessActionsAsync(
            ImmutableSegmentedList<TaggerWork> actions, CancellationToken disposalToken)
        {
            // Come back to the UI Thread to do all the UI work we collected.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(disposalToken);

            var stopwatch = SharedStopwatch.StartNew();

            foreach (var work in actions)
            {
                _threadingContext.ThrowIfNotOnUIThread();

                // If we're being torn down, don't bother doing any more work.  Note: this won't clean up the
                // cancellation token registrations.  But that's fine since we're shutting down anyways.
                if (disposalToken.IsCancellationRequested)
                    return;

                work.PerformWork();

                var elapsedTime = stopwatch.Elapsed;

                if (elapsedTime.TotalMilliseconds > 50)
                {
                    // We don't want to hog the UI thread too much.  If enough time has passed processing the work
                    // items, yield the UI thread so other important work can happen.
                    //
                    // Note: this should be exceeding unlikely to be hit as the work the taggers do themselves is
                    // extremely minimal.  However, it is possible for this to happen, as the taggers are required to
                    // notify their event handlers on the UI thread.  The listeners on the other end of those events
                    // might not be well-behaved, and might hog the UI thread for longer than desired.
                    await Task.Yield().ConfigureAwait(true);
                    stopwatch = SharedStopwatch.StartNew();
                }
            }
        }

        public Task AddUIWorkAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            // Make the cold work item that will actually perform action when we get around to the next timeslice.
            var work = new TaggerWork(action, cancellationToken);
            _queue.AddWork(work);

            // Let the caller keep track of that work item's progress.
            return work.Task;
        }
    }
}
