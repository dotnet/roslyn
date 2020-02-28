// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private int _taskCount;

        public SimpleTaskQueue(TaskScheduler taskScheduler)
        {
            _taskScheduler = taskScheduler;

            _taskCount = 0;
            _latestTask = Task.CompletedTask;
        }

        private TTask ScheduleTaskWorker<TArg, TTask>(Func<int, TArg, TTask> taskCreator, TArg arg)
            where TTask : Task
        {
            lock (_gate)
            {
                _taskCount++;
                var delay = (_taskCount % 100) == 0 ? 1 : 0;

                var task = taskCreator(delay, arg);

                _latestTask = task;

                return task;
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task ScheduleTask(Action taskAction, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker(
                (delay, arg) => arg.Item1._latestTask.ContinueWithAfterDelay(arg.taskAction, arg.cancellationToken, delay, TaskContinuationOptions.None, arg.Item1._taskScheduler),
                (this, taskAction, cancellationToken));
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task<T> ScheduleTask<T>(Func<T> taskFunc, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker(
                (delay, arg) => arg.Item1._latestTask.ContinueWithAfterDelay(arg.taskFunc, arg.cancellationToken, delay, TaskContinuationOptions.None, arg.Item1._taskScheduler),
                (this, taskFunc, cancellationToken));
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task ScheduleTask(Func<Task> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker(
                (delay, arg) => arg.Item1._latestTask.ContinueWithAfterDelayFromAsync(arg.taskFuncAsync, arg.cancellationToken, delay, TaskContinuationOptions.None, arg.Item1._taskScheduler),
                (this, taskFuncAsync, cancellationToken));
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/854696/changing-target-framework-takes-10-minutes-with-10.html",
            AllowCaptures = false)]
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public Task<T> ScheduleTask<T>(Func<Task<T>> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker(
                (delay, arg) => arg.Item1._latestTask.ContinueWithAfterDelayFromAsync(arg.taskFuncAsync, arg.cancellationToken, delay, TaskContinuationOptions.None, arg.Item1._taskScheduler),
                (this, taskFuncAsync, cancellationToken));
        }

        public Task LastScheduledTask => _latestTask;
    }
}
