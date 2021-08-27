// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class SerializationThreadPool
    {
        public static Task<object?> RunOnBackgroundThreadAsync(Func<object?> start)
            => ImmediateBackgroundThreadPool.QueueAsync(start);

        public static Task<object?> RunOnBackgroundThreadAsync(Func<object?, object?> start, object? obj)
            => ImmediateBackgroundThreadPool.QueueAsync(start, obj);

        /// <summary>
        /// Naive thread pool focused on reducing the latency to execution of chunky work items as much as possible.
        /// If a thread is ready to process a work item the moment a work item is queued, it's used, otherwise
        /// a new thread is created. This is meant as a stop-gap measure for workloads that would otherwise be
        /// creating a new thread for every work item.
        /// </summary>
        /// <remarks>
        /// This class is derived from <see href="https://github.com/dotnet/machinelearning/blob/ebc431f531436c45097c88757dfd14fe0c1381b3/src/Microsoft.ML.Core/Utilities/ThreadUtils.cs">dotnet/machinelearning</see>.
        /// </remarks>
        private static class ImmediateBackgroundThreadPool
        {
            /// <summary>How long should threads wait around for additional work items before retiring themselves.</summary>
            private static readonly TimeSpan s_idleTimeout = TimeSpan.FromSeconds(1);

            /// <summary>The queue of work items. Also used as a lock to protect all relevant state.</summary>
            private static readonly Queue<(Delegate function, object? state, TaskCompletionSource<object?> tcs)> s_queue = new();

            /// <summary>The number of threads currently waiting in <c>tryDequeue</c> for work to arrive.</summary>
            private static int s_availableThreads = 0;

            /// <summary>
            /// Queues a <see cref="Func{TResult}"/> delegate to be executed immediately on another thread,
            /// and returns a <see cref="Task"/> that represents its eventual completion. The task will
            /// always end in the <see cref="TaskStatus.RanToCompletion"/> state; if the delegate throws
            /// an exception, it'll be allowed to propagate on the thread, crashing the process.
            /// </summary>
            public static Task<object?> QueueAsync(Func<object?> threadStart) => QueueAsync((Delegate)threadStart, state: null);

            /// <summary>
            /// Queues a <see cref="Func{T, TResult}"/> delegate and associated state to be executed immediately on
            /// another thread, and returns a <see cref="Task"/> that represents its eventual completion.
            /// </summary>
            public static Task<object?> QueueAsync(Func<object?, object?> threadStart, object? state) => QueueAsync((Delegate)threadStart, state);

            private static Task<object?> QueueAsync(Delegate threadStart, object? state)
            {
                // Create the TaskCompletionSource used to represent this work. 'RunContinuationsAsynchronously' ensures
                // continuations do not also run on the threads created by 'createThread'.
                var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Queue the work for a thread to pick up. If no thread is immediately available, it will create one.
                enqueue((threadStart, state, tcs));

                // Return the task.
                return tcs.Task;

                static void createThread()
                {
                    // Create a new background thread to run the work.
                    var t = new Thread(() =>
                    {
                        // Repeatedly get the next item and invoke it, setting its TCS when we're done.
                        // This will wait for up to the idle time before giving up and exiting.
                        while (tryDequeue(out var item))
                        {
                            try
                            {
                                if (item.function is Func<object?, object?> callbackWithState)
                                {
                                    item.tcs.SetResult(callbackWithState(item.state));
                                }
                                else
                                {
                                    item.tcs.SetResult(((Func<object?>)item.function)());
                                }
                            }
                            catch (OperationCanceledException ex)
                            {
                                item.tcs.TrySetCanceled(ex.CancellationToken);
                            }
                            catch (Exception ex)
                            {
                                item.tcs.TrySetException(ex);
                            }
                        }
                    });
                    t.IsBackground = true;
                    t.Start();
                }

                static void enqueue((Delegate function, object? state, TaskCompletionSource<object?> tcs) item)
                {
                    // Enqueue the work. If there are currently fewer threads waiting
                    // for work than there are work items in the queue, create another
                    // thread. This is a heuristic, in that we might end up creating
                    // more threads than are truly needed, but this whole type is being
                    // used to replace a previous solution where every work item created
                    // its own thread, so this is an improvement regardless of any
                    // such inefficiencies.
                    lock (s_queue)
                    {
                        s_queue.Enqueue(item);

                        if (s_queue.Count <= s_availableThreads)
                        {
                            Monitor.Pulse(s_queue);
                            return;
                        }
                    }

                    // No thread was currently available.  Create one.
                    createThread();
                }

                static bool tryDequeue(out (Delegate function, object? state, TaskCompletionSource<object?> tcs) item)
                {
                    // Dequeues the next item if one is available. Before checking,
                    // the available thread count is increased, so that enqueuers can
                    // see how many threads are currently waiting, with the count
                    // decreased after. Each time it waits, it'll wait for at most
                    // the idle timeout before giving up.
                    lock (s_queue)
                    {
                        s_availableThreads++;
                        try
                        {
                            while (s_queue.Count == 0)
                            {
                                if (!Monitor.Wait(s_queue, s_idleTimeout))
                                {
                                    if (s_queue.Count > 0)
                                    {
                                        // The wait timed out, but a new item was added to the queue between the time
                                        // this thread entered the ready queue and the point where the lock was
                                        // reacquired. Make sure to process the available item, since there is no
                                        // guarantee another thread will exist or be notified to handle it separately.
                                        //
                                        // The following is one sequence which requires this path handle the queued
                                        // element for correctness:
                                        //
                                        //  1. Thread A calls tryDequeue, and releases the lock in Wait
                                        //  2. Thread B calls enqueue and holds the lock
                                        //  3. Thread A times out and enters the ready thread queue
                                        //  4. Thread B observes that s_queue.Count (1) <= s_availableThreads (1), so it
                                        //     calls Pulse instead of creating a new thread
                                        //  5. Thread B releases the lock
                                        //  6. Thread A acquires the lock, and Monitor.Wait returns false
                                        //
                                        // Since no new thread was created in step 4, we must handle the enqueued
                                        // element or the thread will exit and the item will sit in the queue
                                        // indefinitely.
                                        break;
                                    }

                                    item = default;
                                    return false;
                                }
                            }
                        }
                        finally
                        {
                            s_availableThreads--;
                        }

                        item = s_queue.Dequeue();
                        return true;
                    }
                }
            }
        }
    }
}
