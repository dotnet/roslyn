// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// schedules task to run in sequence.
    /// </summary>
    internal sealed class SimpleTaskQueue
    {
        private readonly TaskScheduler _taskScheduler;

        /// <summary>
        /// An object to synchronize reads/writes of all mutable fields of this class.
        /// </summary>
        private readonly object _gate = new object();

        private Task _latestTask;

        public SimpleTaskQueue(TaskScheduler taskScheduler)
        {
            _taskScheduler = taskScheduler;
            _latestTask = Task.CompletedTask;
        }

        public Task LastScheduledTask => _latestTask;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods (Task wrappers, not asynchronous methods)
        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task ScheduleTask(Action operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task<T> ScheduleTask<T>(Func<T> operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task ScheduleTask(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => operation(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive("https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html", AllowCaptures = false)]
        public Task<T> ScheduleTask<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
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
