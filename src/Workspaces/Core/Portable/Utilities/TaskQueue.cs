// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implements a queue of asynchronously executed tasks.
    /// </summary>
    internal sealed class TaskQueue
    {
        public IAsynchronousOperationListener Listener { get; }
        public TaskScheduler Scheduler { get; }

        private readonly object _gate = new();
        private Task _latestTask;

        public TaskQueue(IAsynchronousOperationListener operationListener, TaskScheduler taskScheduler)
        {
            Contract.ThrowIfNull(operationListener);
            Contract.ThrowIfNull(taskScheduler);

            Listener = operationListener;
            Scheduler = taskScheduler;
            _latestTask = Task.CompletedTask;
        }

        public Task LastScheduledTask => _latestTask;

        private IAsyncToken BeginOperation(string taskName)
            => Listener.BeginAsyncOperation(taskName);

        private static TTask EndOperation<TTask>(IAsyncToken token, TTask task) where TTask : Task
        {
            // send the notification on operation being complete but do not wait for the notification to be delivered
            _ = task.CompletesAsyncOperation(token);

            return task;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods (Task wrappers, not asynchronous methods)

        /// <summary>
        /// Enqueue specified <paramref name="operation"/> and notify <see cref="Listener"/> of its start and completion.
        /// </summary>
        /// <returns>The <see cref="Task"/> that executes the operation.</returns>
        public Task ScheduleTask(string taskName, Action operation, CancellationToken cancellationToken)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        /// <inheritdoc cref="ScheduleTask(string, Action, CancellationToken)"/>
        public Task<T> ScheduleTask<T>(string taskName, Func<T> operation, CancellationToken cancellationToken)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        /// <inheritdoc cref="ScheduleTask(string, Action, CancellationToken)"/>
        public Task ScheduleTask(string taskName, Func<Task> operation, CancellationToken cancellationToken)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        /// <inheritdoc cref="ScheduleTask(string, Action, CancellationToken)"/>
        public Task<T> ScheduleTask<T>(string taskName, Func<Task<T>> operation, CancellationToken cancellationToken)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        /// <summary>
        /// Enqueue specified <paramref name="operation"/>.
        /// Assumes <see cref="Listener"/> has already been notified of its start and will be notified when it completes.
        /// </summary>
        /// <returns>The <see cref="Task"/> that executes the operation.</returns>
        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        private Task ScheduleTaskInProgress(Action operation, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, Scheduler);
                _latestTask = task;
                return task;
            }
        }

        /// <inheritdoc cref="ScheduleTaskInProgress(Action, CancellationToken)"/>
        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        private Task<T> ScheduleTaskInProgress<T>(Func<T> operation, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, Scheduler);
                _latestTask = task;
                return task;
            }
        }

        /// <inheritdoc cref="ScheduleTaskInProgress(Action, CancellationToken)"/>
        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        private Task ScheduleTaskInProgress(Func<Task> operation, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => operation(), cancellationToken, TaskContinuationOptions.None, Scheduler);
                _latestTask = task;
                return task;
            }
        }

        /// <inheritdoc cref="ScheduleTaskInProgress(Action, CancellationToken)"/>
        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        private Task<T> ScheduleTaskInProgress<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => operation(), cancellationToken, TaskContinuationOptions.None, Scheduler);
                _latestTask = task;
                return task;
            }
        }

#pragma warning restore
    }
}
