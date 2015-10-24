// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification
{
    [Export(typeof(IForegroundNotificationService))]
    internal class ForegroundNotificationService : ForegroundThreadAffinitizedObject, IForegroundNotificationService
    {
        // how much time we will give notifications to run on the UI thread
        private const int DefaultTimeSliceInMS = 15;

        // Don't call NotifyOnForeground more than once per 50ms
        private const int MinimumDelayBetweenProcessing = 50;

        private static readonly Func<int, string> s_notifyOnForegroundLogger = c => string.Format("Processed : {0}", c);
        private readonly PriorityQueue _workQueue;

        private int _lastProcessedTimeInMS;

        [ImportingConstructor]
        public ForegroundNotificationService()
        {
            _workQueue = new PriorityQueue();
            _lastProcessedTimeInMS = Environment.TickCount;

            Debug.Assert(IsValid());
            Debug.Assert(IsForeground());
            Task.Factory.SafeStartNewFromAsync(ProcessAsync, CancellationToken.None, TaskScheduler.Default);
        }

        public void RegisterNotification(Action action, IAsyncToken asyncToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            RegisterNotification(action, DefaultTimeSliceInMS, asyncToken, cancellationToken);
        }

        public void RegisterNotification(Func<bool> action, IAsyncToken asyncToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            RegisterNotification(action, DefaultTimeSliceInMS, asyncToken, cancellationToken);
        }

        public void RegisterNotification(Action action, int delay, IAsyncToken asyncToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(delay >= 0);

            var current = Environment.TickCount;

            _workQueue.Enqueue(new PendingWork(current + delay, action, asyncToken, cancellationToken));
        }

        public void RegisterNotification(Func<bool> action, int delay, IAsyncToken asyncToken, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(delay >= 0);

            var current = Environment.TickCount;

            _workQueue.Enqueue(new PendingWork(current + delay, action, asyncToken, cancellationToken));
        }

        public bool IsEmpty_TestOnly
        {
            get
            {
                return _workQueue.IsEmpty;
            }
        }

        private async Task ProcessAsync()
        {
            try
            {
                AssertIsBackground();

                while (true)
                {
                    // wait until it is time to run next item
                    await WaitForPendingWorkAsync().ConfigureAwait(continueOnCapturedContext: false);

                    // run them in UI thread
                    await InvokeBelowInputPriority(NotifyOnForeground).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
            {
                System.Diagnostics.Debug.Assert(false, ex.Message);
            }
        }

        private void NotifyOnForeground()
        {
            NotifyOnForegroundWorker();

            _workQueue.Touch();
        }

        private void NotifyOnForegroundWorker()
        {
            AssertIsForeground();

            using (Logger.LogBlock(FunctionId.ForegroundNotificationService_NotifyOnForeground, CancellationToken.None))
            {
                var processedCount = 0;
                var startProcessingTime = Environment.TickCount;

                PendingWork pendingWork;
                while (_workQueue.TryGetWorkItem(startProcessingTime, out pendingWork))
                {
                    var done = true;

                    // don't process one that is already cancelled
                    if (!pendingWork.CancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (pendingWork.DoWorkAction != null)
                            {
                                pendingWork.DoWorkAction();
                            }
                            else if (pendingWork.DoWorkFunc != null)
                            {
                                if (pendingWork.DoWorkFunc())
                                {
                                    done = false;
                                    _workQueue.Enqueue(pendingWork.UpdateToCurrentTime());
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // eat up cancellation
                        }
                    }

                    if (done)
                    {
                        pendingWork.AsyncToken.Dispose();
                    }

                    processedCount++;

                    // there is input to process, or we've exceeded a time slice, postpone the remaining work
                    if (IsInputPending() || Environment.TickCount - startProcessingTime > DefaultTimeSliceInMS)
                    {
                        return;
                    }
                }

                // Record the current timestamp so we don't immediately process newly added items.
                _lastProcessedTimeInMS = Environment.TickCount;
                Logger.Log(FunctionId.ForegroundNotificationService_Processed, s_notifyOnForegroundLogger, processedCount);
            }
        }

        private async Task WaitForPendingWorkAsync()
        {
            await _workQueue.WaitForItemsAsync().ConfigureAwait(false);
            while (true)
            {
                var current = Environment.TickCount;
                var nextItem = _workQueue.PeekNextItemTime();

                // The next item is ready to run
                if (nextItem - current <= 0)
                {
                    break;
                }

                // wait some and re-check since there could be another one inserted before the first one while we were waiting.
                await Task.Delay(MinimumDelayBetweenProcessing).ConfigureAwait(continueOnCapturedContext: false);
            }

            // Throttle how often we run by waiting MinimumDelayBetweenProcessing since the last time we processed notifications
            if (Environment.TickCount - _lastProcessedTimeInMS < MinimumDelayBetweenProcessing)
            {
                await Task.Delay(MinimumDelayBetweenProcessing).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private struct PendingWork
        {
            public readonly int MinimumRunPointInMS;
            public readonly Action DoWorkAction;
            public readonly Func<bool> DoWorkFunc;
            public readonly IAsyncToken AsyncToken;
            public readonly CancellationToken CancellationToken;

            private PendingWork(int minimumRunPointInMS, Action action, Func<bool> func, IAsyncToken asyncToken, CancellationToken cancellationToken)
            {
                this.MinimumRunPointInMS = minimumRunPointInMS;
                this.DoWorkAction = action;
                this.DoWorkFunc = func;
                this.AsyncToken = asyncToken;
                this.CancellationToken = cancellationToken;
            }

            public PendingWork(int minimumRunPointInMS, Action work, IAsyncToken asyncToken, CancellationToken cancellationToken)
                : this(minimumRunPointInMS, work, null, asyncToken, cancellationToken)
            {
            }

            public PendingWork(int minimumRunPointInMS, Func<bool> work, IAsyncToken asyncToken, CancellationToken cancellationToken)
                : this(minimumRunPointInMS, null, work, asyncToken, cancellationToken)
            {
            }

            public PendingWork UpdateToCurrentTime()
            {
                return new PendingWork(Environment.TickCount, DoWorkAction, DoWorkFunc, AsyncToken, CancellationToken);
            }
        }

        private class PriorityQueue
        {
            // use pool to share linked list nodes rather than re-create them every time
            private static readonly ObjectPool<LinkedListNode<PendingWork>> s_pool =
                new ObjectPool<LinkedListNode<PendingWork>>(() => new LinkedListNode<PendingWork>(default(PendingWork)), 100);

            private readonly object _gate = new object();
            private readonly LinkedList<PendingWork> _list = new LinkedList<PendingWork>();
            private readonly SemaphoreSlim _hasItemsGate = new SemaphoreSlim(initialCount: 0);

            public Task WaitForItemsAsync()
            {
                if (!IsEmpty)
                {
                    return SpecializedTasks.True;
                }

                return WaitForNewItemsAsync();
            }

            private async Task WaitForNewItemsAsync()
            {
                // Use a while loop, since the inserted item may have been processed by TryGetWorkItem
                // leaving the semaphore count at 1 even though we're empty again.
                while (IsEmpty)
                {
                    await _hasItemsGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }

            public bool IsEmpty
            {
                get
                {
                    lock (_gate)
                    {
                        return _list.Count == 0;
                    }
                }
            }

            public void Touch()
            {
                lock (_gate)
                {
                    if (_list.Count > 0)
                    {
                        // mark that we have more items to process
                        _hasItemsGate.Release();
                    }
                }
            }

            public void Enqueue(PendingWork work)
            {
                var entry = s_pool.Allocate();
                entry.Value = work;

                lock (_gate)
                {
                    Enqueue_NoLock(entry);
                }
            }

            private void Enqueue_NoLock(LinkedListNode<PendingWork> entry)
            {
                // TODO: if this cost shows up in the trace, either use tree based implementation
                // or just have separate lists for each delay (short, medium, long)
                if (_list.Count == 0)
                {
                    _list.AddLast(entry);
                    _hasItemsGate.Release();
                    return;
                }

                var current = _list.Last;
                while (current != null)
                {
                    if (current.Value.MinimumRunPointInMS <= entry.Value.MinimumRunPointInMS)
                    {
                        _list.AddAfter(current, entry);
                        return;
                    }

                    current = current.Previous;
                }

                _list.AddFirst(entry);
                _hasItemsGate.Release();
                return;
            }

            public int PeekNextItemTime()
            {
                lock (_gate)
                {
                    Contract.Requires(_list.Count > 0);
                    return _list.First.Value.MinimumRunPointInMS;
                }
            }

            public bool TryGetWorkItem(int currentTime, out PendingWork pendingWork)
            {
                pendingWork = default(PendingWork);

                lock (_gate)
                {
                    if (!ContainsMoreWork_NoLock(currentTime))
                    {
                        return false;
                    }

                    pendingWork = Dequeue_NoLock();
                    return true;
                }
            }

            private bool ContainsMoreWork_NoLock(int currentTime)
            {
                return _list.Count > 0 && _list.First.Value.MinimumRunPointInMS <= currentTime;
            }

            private PendingWork Dequeue_NoLock()
            {
                var entry = _list.First;
                var work = entry.Value;

                _list.RemoveFirst();

                // reset the value and put it back to pool
                entry.Value = default(PendingWork);
                s_pool.Free(entry);

                return work;
            }
        }
    }
}
