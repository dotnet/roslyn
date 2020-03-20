// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    internal sealed class TaskQueue
    {
        private readonly IAsynchronousOperationListener _operationListener;
        private readonly TaskScheduler _taskScheduler;

        private readonly object _gate = new object();
        private Task _latestTask;

        internal TaskQueue(IAsynchronousOperationListener operationListener, TaskScheduler taskScheduler)
        {
            Contract.ThrowIfNull(operationListener);
            Contract.ThrowIfNull(taskScheduler);

            _operationListener = operationListener;
            _taskScheduler = taskScheduler;
            _latestTask = Task.CompletedTask;
        }

        public Task LastScheduledTask => _latestTask;

        private IAsyncToken BeginOperation(string taskName)
            => _operationListener.BeginAsyncOperation(taskName);

        private TTask EndOperation<TTask>(IAsyncToken token, TTask task) where TTask : Task
        {
            _ = task.CompletesAsyncOperation(token);
            return task;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods (Task wrappers, not asynchronous methods)
        public Task ScheduleTask(string taskName, Action operation, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        public Task<T> ScheduleTask<T>(string taskName, Func<T> operation, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        public Task ScheduleTask(string taskName, Func<Task> operation, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        public Task<T> ScheduleTask<T>(string taskName, Func<Task<T>> operation, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), ScheduleTaskInProgress(operation, cancellationToken));

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task ScheduleTaskInProgress(Action operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task<T> ScheduleTaskInProgress<T>(Func<T> operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task ScheduleTaskInProgress(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task<T> ScheduleTaskInProgress<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }
#pragma warning restore VSTHRD200
    }
}
