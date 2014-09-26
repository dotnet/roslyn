// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A thread-safe, asynchronously dequeuable queue.
    /// </summary>
    /// <typeparam name="TElement">The type of values kept by the queue.</typeparam>
    public sealed partial class AsyncQueue<TElement>
    {
        private readonly object syncObject = new object();
        private readonly Queue<TElement> data = new Queue<TElement>();
        private readonly Queue<TaskCompletionSourceWithCancellation<TElement>> waiters = new Queue<TaskCompletionSourceWithCancellation<TElement>>();
        private bool completed = false;
        private readonly TaskCompletionSourceWithCancellation<bool> whenCompleted;
        private Exception thrown = null;

        /// <summary>
        /// The number of unconsumed elements in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock(syncObject)
                {
                    return data.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncQueue{TElement}"/> class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for <see cref="WhenCompletedAsync"/> task.</param>
        public AsyncQueue(CancellationToken cancellationToken)
        {
            var whenCompleted = new TaskCompletionSourceWithCancellation<bool>();
            whenCompleted.RegisterForCancellation(cancellationToken);
            this.whenCompleted = whenCompleted;
        }

        /// <summary>
        /// Adds an element to the tail of the queue.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Enqueue(TElement value)
        {
            TaskCompletionSourceWithCancellation<TElement> waiter;
            lock(syncObject)
            {
                if (completed)
                {
                    throw new InvalidOperationException("Enqueue after Complete");
                }

                if (thrown != null)
                {
                    return;
                }

                if (waiters.Count == 0)
                {
                    data.Enqueue(value);
                    return;
                }

                Debug.Assert(data.Count == 0);
                waiter = waiters.Dequeue();
            }

            var arguments = new WaiterTaskArguments(waiter, value, this);
            Task.Factory.StartNew(WaiterTaskArguments.TrySetResultOrEnqueue, arguments);
        }

        private class WaiterTaskArguments
        {
            private readonly TaskCompletionSourceWithCancellation<TElement> waiter;
            private readonly TElement value;
            private readonly AsyncQueue<TElement> @this;

            public WaiterTaskArguments(TaskCompletionSourceWithCancellation<TElement> waiter, TElement value, AsyncQueue<TElement> @this)
            {
                this.waiter = waiter;
                this.value = value;
                this.@this = @this;
            }

            public static void TrySetResultOrEnqueue(object arguments)
            {
                ((WaiterTaskArguments)arguments).TrySetResultOrEnqueue();
            }

            private void TrySetResultOrEnqueue()
            {
                if (!this.waiter.TrySetResult(this.value))
                {
                    // Waiter got cancelled, so try to enqueue again.
                    @this.Enqueue(this.value);
                }
            }
        }

        /// <summary>
        /// Set the queue to an exception state. Once this has been done, every
        /// Dequeue operation will throw this exception.
        /// </summary>
        /// <param name="exception">The exception to be associated with this queue.</param>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            ImmutableArray<TaskCompletionSourceWithCancellation<TElement>> waitersArray;
            lock (syncObject)
            {
                if (completed)
                {
                    throw new InvalidOperationException("Thrown after Completed");
                }

                if (thrown != null)
                {
                    throw new InvalidOperationException("Thrown after Thrown");
                }

                thrown = exception;
                data.Clear();
                waitersArray = waiters.AsImmutable();
                waiters.Clear();
            }

            Task.Run(() =>
            {
                whenCompleted.SetException(exception);
                foreach (var tcs in waitersArray)
                {
                    tcs.SetException(exception);
                }
            });
        }

        public bool TryDequeue(out TElement d)
        {
            d = default(TElement);
            lock(syncObject)
            {
                if (data.Count == 0)
                {
                    return false;
                }

                d = data.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the queue has completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                lock(syncObject)
                {
                    return completed;
                }
            }
        }

        /// <summary>
        /// Signals that no further elements will be enqueued.
        /// </summary>
        public void Complete()
        {
            ImmutableArray<TaskCompletionSourceWithCancellation<TElement>> waitersArray;
            lock(syncObject)
            {
                if (completed)
                {
                    throw new InvalidOperationException("Completed after Completed");
                }

                if (thrown != null)
                {
                    throw new InvalidOperationException("Completed after Thrown");
                }

                waitersArray = waiters.AsImmutable();
                waiters.Clear();
                completed = true;
            }

            Task.Run(() =>
            {
                whenCompleted.SetResult(true);
                foreach (var tcs in waitersArray)
                {
                    tcs.SetCanceled();
                }
            });
        }

        /// <summary>
        /// Gets a task that transitions to a completed state when <see cref="Complete"/> is called.
        /// </summary>
        public Task WhenCompletedAsync
        {
            get
            {
                return whenCompleted.Task;
            }
        }

        /// <summary>
        /// Gets a task whose result is the element at the head of the queue. If the queue
        /// is empty, waits for an element to be enqueued. If <see cref="Complete"/> is called
        /// before an element becomes available, the returned task is cancelled. If
        /// <see cref="SetException"/> is called before an element becomes available, the
        /// returned task throws that exception.
        /// </summary>
        public Task<TElement> DequeueAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task<TElement>(() => default(TElement), cancellationToken);
            }

            TaskCompletionSourceWithCancellation<TElement> waiter0;

            lock(syncObject)
            {
                if (thrown != null)
                {
                    var waiter = new TaskCompletionSource<TElement>();
                    waiter.SetException(thrown);
                    return waiter.Task;
                }

                if (data.Count != 0)
                {
                    Debug.Assert(waiters.Count == 0);
                    var datum = data.Dequeue();
                    return Task.FromResult(datum);
                }

                if (completed)
                {
                    var waiter = new TaskCompletionSource<TElement>();
                    waiter.SetCanceled();
                    return waiter.Task;
                }

                waiter0 = new TaskCompletionSourceWithCancellation<TElement>();
                waiters.Enqueue(waiter0);
            }

            // Register for cancellation outside the lock, as our registration may immediately fire and we want to avoid the reentrancy.
            waiter0.RegisterForCancellation(cancellationToken);
            return waiter0.Task;
        }

        public override string ToString()
        {
            return "AsyncQueue<" + typeof(TElement).Name + ">:" + (IsCompleted ? "Completed" : Count.ToString());
        }
    }
}
