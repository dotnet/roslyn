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

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task ScheduleTask(Action taskAction, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => taskAction(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task<T> ScheduleTask<T>(Func<T> taskFunc, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWith(_ => taskFunc(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task ScheduleTask(Func<Task> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => taskFuncAsync(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task<T> ScheduleTask<T>(Func<Task<T>> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var task = _latestTask.SafeContinueWithFromAsync(_ => taskFuncAsync(), cancellationToken, TaskContinuationOptions.None, _taskScheduler);
                _latestTask = task;
                return task;
            }
        }

        public Task LastScheduledTask => _latestTask;
    }
}
