// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Threading
{
    // A helper class primarily used for the AsynchronousTagger that can handle the job of
    // scheduling work to be done on the UI thread and on a background thread.  This class wraps the
    // TPL and implements things in special ways to provide certain nice bits of functionality.
    // Specifically: 
    //
    // 1) Background actions are run serially.  This allows you to enqueue a whole host of background
    // work to do, without having to worry about those same background tasks running simultaneously 
    // and colliding with each other.
    //
    // 2) You can start a 'chain' of actions starting with an action that fires after a delay. After
    // that point you can continue adding actions to that chain.  You can then ask to wait until that
    // chain of actions completes (very useful for testing purposes). This chain can also be
    // cancelled very simply.
    internal class AsynchronousSerialWorkQueue : ForegroundThreadAffinitizedObject
    {
        #region Fields that can be accessed from either thread

        // The task schedulers that we can use to schedule work on the UI thread.
        private readonly IAsynchronousOperationListener _asyncListener;

        // Lock for serializing access to these objects.
        private readonly object _gate = new object();

        // The current task we are executing on the background.  Kept around so we can serialize
        // background tasks by continually calling 'SafeContinueWith' on this task.
        private Task _currentBackgroundTask;

        // The cancellation source for the current chain of work.
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #endregion

        public AsynchronousSerialWorkQueue(IAsynchronousOperationListener asyncListener)
            : base(assertIsForeground: false)
        {
            Contract.ThrowIfNull(asyncListener);
            _asyncListener = asyncListener;

            // Initialize so we don't have to check for null below. Force the background task to run
            // on the threadpool. 
            _currentBackgroundTask = SpecializedTasks.EmptyTask;
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        public void CancelCurrentWork()
        {
            lock (_gate)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        public void EnqueueBackgroundWork(Action action, string name, CancellationToken cancellationToken)
        {
            EnqueueBackgroundWork(action, name, afterDelay: 0, cancellationToken: cancellationToken);
        }

        public void EnqueueBackgroundWork(Action action, string name, int afterDelay, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(action);

            var asyncToken = _asyncListener.BeginAsyncOperation(name);

            lock (_gate)
            {
                if (afterDelay == 0)
                {
                    // Note that we serialized background tasks so that consumers of this type can issue
                    // multiple background tasks without having to worry about them running
                    // simultaneously.
                    _currentBackgroundTask = _currentBackgroundTask.SafeContinueWith(
                        _ => action(),
                        cancellationToken,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
                }
                else
                {
                    _currentBackgroundTask = _currentBackgroundTask.ContinueWithAfterDelay(
                        action, cancellationToken, afterDelay, TaskContinuationOptions.None, TaskScheduler.Default);
                }

                _currentBackgroundTask.CompletesAsyncOperation(asyncToken);
            }
        }

        public void EnqueueBackgroundTask(
            Func<CancellationToken, Task> taskGeneratingFunctionAsync,
            string name,
            CancellationToken cancellationToken)
        {
            EnqueueBackgroundTask(taskGeneratingFunctionAsync, name, afterDelay: 0, cancellationToken: cancellationToken);
        }

        public void EnqueueBackgroundTask(
            Func<CancellationToken, Task> taskGeneratingFunctionAsync,
            string name, int afterDelay, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(taskGeneratingFunctionAsync);

            var asyncToken = _asyncListener.BeginAsyncOperation(name);

            lock (_gate)
            {
                if (afterDelay == 0)
                {
                    // Note that we serialized background tasks so that consumers of this type can issue
                    // multiple background tasks without having to worry about them running
                    // simultaneously.
                    _currentBackgroundTask = _currentBackgroundTask.SafeContinueWithFromAsync(
                        _ => taskGeneratingFunctionAsync(cancellationToken),
                        cancellationToken,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
                }
                else
                {
                    _currentBackgroundTask = _currentBackgroundTask.ContinueWithAfterDelayFromAsync(
                        _ => taskGeneratingFunctionAsync(cancellationToken), cancellationToken, afterDelay, TaskContinuationOptions.None, TaskScheduler.Default);
                }

                _currentBackgroundTask.CompletesAsyncOperation(asyncToken);
            }
        }

        /// <summary>
        /// Wait until all queued background tasks have been completed.  NOTE: This will NOT pump,
        /// and it won't wait for any timer foreground tasks to actually enqueue their respective
        /// background tasks - it just waits for the already enqueued background tasks to finish.
        /// </summary>
        public void WaitForPendingBackgroundWork()
        {
            AssertIsForeground();

            _currentBackgroundTask.Wait();
        }

        /// <summary>
        /// Wait until all tasks have been completed.  NOTE that this will do a pumping wait if
        /// called on the UI thread. Also, it isn't guaranteed to be stable in the case of tasks
        /// enqueuing other tasks in arbitrary orders, though it does support our common pattern of
        /// "timer task->background task->foreground task with results"
        /// 
        /// Use this method very judiciously.  Most of the time, we should be able to just use 
        /// IAsynchronousOperationListener for tests.
        /// </summary>
        public void WaitUntilCompletion_ForTestingPurposesOnly()
        {
            AssertIsForeground();

            WaitForPendingBackgroundWork();
        }
    }
}
