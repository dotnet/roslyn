// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A queue whose enqueue and dequeue operations can be performed in parallel.
    /// </summary>
    /// <typeparam name="TElement">The type of values kept by the queue.</typeparam>
    public sealed class AsyncQueue<TElement>
    {
        private enum State : byte
        {
            Active,
            Completed,
            CompletedWithException,
        }

        private readonly TaskCompletionSource<bool> whenCompleted = new TaskCompletionSource<bool>();

        // Note: All of the below fields are accessed in parallel and may only be accessed
        // when protected by lock (SyncObject)
        private readonly Queue<TElement> data = new Queue<TElement>();
        private Queue<TaskCompletionSource<TElement>> waiters;
        private Exception thrown;
        private State state = State.Active;

        private object SyncObject
        {
            get { return this.data; }
        }

        /// <summary>
        /// The number of unconsumed elements in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock (SyncObject)
                {
                    return this.data.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncQueue{TElement}"/> class.
        /// </summary>
        public AsyncQueue()
        {
        }

        /// <summary>
        /// Adds an element to the tail of the queue.  This method will throw if the queue 
        /// is completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is already completed.</exception>
        /// <param name="value">The value to add.</param>
        public void Enqueue(TElement value)
        {
            if (!EnqueueCore(value))
            {
                throw new InvalidOperationException($"Cannot call {nameof(Enqueue)} when the queue is already completed.");
            }
        }

        /// <summary>
        /// Tries to add an element to the tail of the queue.  This method will return false if the queue
        /// is completed.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public bool TryEnqueue(TElement value)
        {
            return EnqueueCore(value);
        }

        private bool EnqueueCore(TElement value)
        {
            TaskCompletionSource<TElement> waiter;
            lock (SyncObject)
            {
                if (this.state != State.Active)
                {
                    return false;
                }

                if (this.waiters == null || this.waiters.Count == 0)
                {
                    this.data.Enqueue(value);
                    return true;
                }

                Debug.Assert(this.data.Count == 0);
                waiter = this.waiters.Dequeue();
            }

            waiter.SetResult(value);
            return true;
        }

        /// <summary>
        /// Sets the queue to a completed state with the provided <parameref name="exception"/>.  All 
        /// outstanding and future <see cref="AsyncQueue{TElement}.DequeueAsync"/> Task values will 
        /// be resolved to this value. 
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is already completed.</exception>
        /// <param name="exception">The exception to be associated with this queue.</param>
        public void SetException(Exception exception)
        {
            if (!SetExceptionCore(exception))
            {
                throw new InvalidOperationException($"Cannot call ${nameof(SetException)} when the queue is already completed.");
            }
        }

        /// <summary>
        /// This has the same effect as <see cref="AsyncQueue{TElement}.SetException"/> except 
        /// it will not throw an exception if the queue is in a completed state.
        /// </summary>
        /// <param name="exception">The exception to be associated with this queue.</param>
        /// <returns>Whether or not the operation succeeded.</returns>
        public bool TrySetException(Exception exception)
        {
            return SetExceptionCore(exception);
        }

        private bool SetExceptionCore(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            Queue<TaskCompletionSource<TElement>> existingWaiters;
            lock (SyncObject)
            {
                if (this.state != State.Active)
                {
                    return false;
                }

                existingWaiters = this.waiters;
                this.thrown = exception;
                this.state = State.CompletedWithException;
                this.waiters = null;
            }

            Task.Run(() =>
            {
                if (existingWaiters != null)
                {
                    foreach (var tcs in existingWaiters)
                    {
                        tcs.SetException(exception);
                    }
                }

                this.whenCompleted.SetException(exception);
            });

            return true;
        }

        /// <summary>
        /// Attempts to dequeue an existing item and return whether or not it was available.
        /// </summary>
        public bool TryDequeue(out TElement d)
        {
            lock (SyncObject)
            {
                if (this.data.Count == 0)
                {
                    d = default(TElement);
                    return false;
                }

                d = this.data.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the queue has completed.  This is true 
        /// when the method <see cref="AsyncQueue{TElement}.Complete"/>, <see cref="AsyncQueue{TElement}.SetException(Exception)" /> 
        /// or their Try variants have been called.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                lock (SyncObject)
                {
                    return this.state != State.Active;
                }
            }
        }

        /// <summary>
        /// Signals that no further elements will be enqueued.  All outstanding and future
        /// Dequeue Task will be cancelled.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is already completed.</exception>
        public void Complete()
        {
            if (!CompleteCore())
            {
                throw new InvalidOperationException($"Cannot call {nameof(Complete)} when the queue is already completed.");
            }
        }

        /// <summary>
        /// Same operation as <see cref="AsyncQueue{TElement}.Complete"/> except it will not
        /// throw if the queue is already completed.
        /// </summary>
        /// <returns>Whether or not the operation succeeded.</returns>
        public bool TryComplete()
        {
            return CompleteCore();
        }

        private bool CompleteCore()
        {
            Queue<TaskCompletionSource<TElement>> existingWaiters;
            lock (SyncObject)
            {
                if (this.state != State.Active)
                {
                    return false;
                }

                Debug.Assert(this.thrown == null);
                existingWaiters = this.waiters;
                this.state = State.Completed;
                this.waiters = null;
            }

            Task.Run(() =>
            {
                if (existingWaiters != null)
                {
                    foreach (var tcs in existingWaiters)
                    {
                        tcs.SetCanceled();
                    }
                }

                whenCompleted.SetResult(true);
            });

            return true;
        }

        /// <summary>
        /// Gets a task that transitions to a completed state when <see cref="Complete"/>,
        /// <see cref="SetException"/> or their Try variants is called.  This transition 
        /// will not happen synchronously. 
        /// 
        /// This Task will not complete until it has completed all existing values returned
        /// from <see cref="DequeueAsync"/>.
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
        /// is empty, the returned task waits for an element to be enqueued. If <see cref="Complete"/> 
        /// is called before an element becomes available, the returned task is cancelled. If
        /// <see cref="SetException"/> is called before an element becomes available, the
        /// returned task is resolved to that exception.
        /// </summary>
        public Task<TElement> DequeueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return WithCancellation(DequeueAsyncCore(), cancellationToken);
        }

        /// <summary>
        /// 
        /// Note: The early cancellation behavior is intentional.
        /// </summary>
        private static Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new Task<T>(() => default(T), cancellationToken);
            }

            return task.ContinueWith(t => t, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        }

        private Task<TElement> DequeueAsyncCore()
        {
            lock (SyncObject)
            {
                // No matter what the state we allow DequeueAsync to drain the existing items 
                // in the queue.  This keeps the behavior in line with TryDequeue
                if (this.data.Count > 0)
                {
                    return Task.FromResult(this.data.Dequeue());
                }

                switch (this.state)
                {
                    case State.Active:
                        {
                            var waiter = new TaskCompletionSource<TElement>();
                            if (this.waiters == null)
                            {
                                this.waiters = new Queue<TaskCompletionSource<TElement>>();
                            }

                            this.waiters.Enqueue(waiter);
                            return waiter.Task;
                        }

                    case State.Completed:
                        {
                            var tcs = new TaskCompletionSource<TElement>();
                            tcs.SetCanceled();
                            return tcs.Task;
                        }

                    case State.CompletedWithException:
                        {
                            Debug.Assert(this.thrown != null);
                            var tcs = new TaskCompletionSource<TElement>();
                            tcs.SetException(this.thrown);
                            return tcs.Task;
                        }

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }
        }
    }
}
