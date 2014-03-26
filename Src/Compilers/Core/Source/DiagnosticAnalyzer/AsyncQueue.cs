// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A thread-safe, asynchronously dequeuable queue.
    /// </summary>
    /// <typeparam name="TElement">The type of values kept by the queue.</typeparam>
    public sealed class AsyncQueue<TElement>
    {
        private object syncObject = new object();
        private Queue<TElement> data = new Queue<TElement>();
        private Queue<TaskCompletionSource<TElement>> waiters = new Queue<TaskCompletionSource<TElement>>();
        private bool completed = false;
        private TaskCompletionSource<bool> whenCompleted = new TaskCompletionSource<bool>();
        private Exception thrown = null;

        /// <summary>
        /// The number of unconsumed elements in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                return data.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncQueue{TElement}"/> class.
        /// </summary>
        public AsyncQueue()
        {
        }

        /// <summary>
        /// Adds an element to the tail of the queue.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Enqueue(TElement value)
        {
            TaskCompletionSource<TElement> waiter;
            lock (syncObject)
            {
                if (completed) throw new InvalidOperationException("Add after Complete");
                if (thrown != null) return;
                if (waiters.Count == 0)
                {
                    data.Enqueue(value);
                    return;
                }

                waiter = waiters.Dequeue();
                Debug.Assert(data.Count == 0);
            }

            waiter.SetResult(value);
        }

        /// <summary>
        /// Set the queue to an exception state. Once this has been done, every Enqueue
        /// and Dequeue operation will throw this exception.
        /// </summary>
        /// <param name="exception">The exception to be associated with this queue.</param>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            ImmutableArray<TaskCompletionSource<TElement>> waitersArray;
            lock (syncObject)
            {
                if (completed) throw new InvalidOperationException("Thrown after Completed");
                if (thrown != null) throw new InvalidOperationException("Thrown after Thrown");
                thrown = exception;
                data.Clear();
                waitersArray = waiters.AsImmutable(); // TODO: move allocation out of the lock
                waiters.Clear();
            }

            whenCompleted.SetException(exception);
            foreach (var tcs in waitersArray)
            {
                tcs.SetException(exception);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the queue has completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return completed;
            }
        }

        /// <summary>
        /// Signals that no further elements will be enqueued.
        /// </summary>
        public void Complete()
        {
            ImmutableArray<TaskCompletionSource<TElement>> waitersArray;
            lock (syncObject)
            {
                if (thrown != null) throw new InvalidOperationException("Done after Thrown");
                waitersArray = waiters.AsImmutable();
                waiters.Clear();
                completed = true;
            }

            foreach (var tcs in waitersArray)
            {
                tcs.SetCanceled();
            }
            whenCompleted.SetResult(true);
        }

        /// <summary>
        /// Gets a task that transitions to a completed state when <see cref="Complete"/> is called.
        /// </summary>
        public Task<bool> WhenCompleted
        {
            get { return whenCompleted.Task; }
        }

        /// <summary>
        /// Gets a task whose result is the element at the head of the queue. If the queue
        /// is empty, waits for an element to be enqueued. If <see cref="Complete"/> is called
        /// before an element becomes available, the returned task is cancelled. If
        /// <see cref="SetException"/> is called before an element becomes available, the
        /// returned task thrown that exception.
        /// </summary>
        public Task<TElement> DequeueAsync()
        {
            lock (syncObject)
            {
                if (thrown != null)
                {
                    throw thrown;
                }
                else if (data.Count != 0)
                {
                    Debug.Assert(waiters.Count == 0);
                    return Task.FromResult(data.Dequeue());
                }
                else if (completed)
                {
                    throw new TaskCanceledException();
                }
                else
                {
                    var waiter = new TaskCompletionSource<TElement>();
                    waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public override string ToString()
        {
            return "AsyncQueue<" + typeof(TElement).Name + ">:" + (this.IsCompleted ? "Completed" : data.Count.ToString());
        }
    }
}
