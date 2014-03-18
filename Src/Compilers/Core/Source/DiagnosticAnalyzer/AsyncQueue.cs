// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.DiagnosticAnalyzer
{
    /// <summary>
    /// Possibly this can be replaced by a sealed subclass of Microsoft.Threading.AsyncQueue.
    /// It needs to be a sealed subclass to prevent clients from overriding the methods.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public sealed class AsyncQueue<TElement>
    {
        private object syncLock = new object();
        private Queue<TElement> data = new Queue<TElement>();
        private Queue<TaskCompletionSource<TElement>> waiters = new Queue<TaskCompletionSource<TElement>>();
        private bool done = false;
        private TaskCompletionSource<bool> whenDone = new TaskCompletionSource<bool>();
        private Exception thrown = null;

        public AsyncQueue()
        {
        }

        public void Enqueue(TElement e)
        {
            TaskCompletionSource<TElement> waiter;
            lock (syncLock)
            {
                if (done) throw new InvalidOperationException("Add after Done");
                if (thrown != null) return;
                if (waiters.Count == 0)
                {
                    data.Enqueue(e);
                    return;
                }

                waiter = waiters.Dequeue();
                Debug.Assert(data.Count == 0);
            }

            waiter.SetResult(e);
        }

        public void SetException(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException("ex");
            ImmutableArray<TaskCompletionSource<TElement>> waitersArray;
            lock (syncLock)
            {
                if (done) throw new InvalidOperationException("Thrown after Done");
                if (thrown != null) throw new InvalidOperationException("Thrown after Thrown");
                thrown = ex;
                data.Clear();
                waitersArray = waiters.AsImmutable(); // TODO: move allocation out of the lock
                waiters.Clear();
            }

            whenDone.SetException(ex);
            foreach (var tcs in waitersArray)
            {
                tcs.SetException(ex);
            }
        }

        public bool Done
        {
            get
            {
                return done;
            }
            set
            {
                if (!value) throw new ArgumentException("value");
                ImmutableArray<TaskCompletionSource<TElement>> waitersArray;
                lock (syncLock)
                {
                    if (thrown != null) throw new InvalidOperationException("Done after Thrown");
                    waitersArray = waiters.AsImmutable();
                    waiters.Clear();
                    done = true;
                }

                foreach (var tcs in waitersArray)
                {
                    tcs.SetCanceled();
                }
                whenDone.SetResult(true);
            }
        }

        public Task<bool> WhenDone
        {
            get { return whenDone.Task; }
        }

        public Task<TElement> TryGetElement()
        {
            lock (syncLock)
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
                else if (done)
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
    }
}
