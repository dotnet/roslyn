// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private TTask ScheduleTaskWorker<TTask>(Func<int, TTask> taskCreator, CancellationToken cancellationToken)
            where TTask : Task
        {
            lock (_gate)
            {
                _taskCount++;
                int delay = (_taskCount % 100) == 0 ? 1 : 0;

                var task = taskCreator(delay);

                _latestTask = task;

                return task;
            }
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public Task ScheduleTask(Action taskAction, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker<Task>(delay => _latestTask.ContinueWithAfterDelay(
                taskAction, cancellationToken, delay, TaskContinuationOptions.None, _taskScheduler),
                cancellationToken);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public Task<T> ScheduleTask<T>(Func<T> taskFunc, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker<Task<T>>(delay => _latestTask.ContinueWithAfterDelay(
                t => taskFunc(), cancellationToken, delay, TaskContinuationOptions.None, _taskScheduler),
                cancellationToken);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public Task ScheduleTask(Func<Task> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker<Task>(delay => _latestTask.ContinueWithAfterDelayFromAsync(
                t => taskFuncAsync(), cancellationToken, delay, TaskContinuationOptions.None, _taskScheduler),
                cancellationToken);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public Task<T> ScheduleTask<T>(Func<Task<T>> taskFuncAsync, CancellationToken cancellationToken = default)
        {
            return ScheduleTaskWorker<Task<T>>(delay => _latestTask.ContinueWithAfterDelayFromAsync(
                t => taskFuncAsync(), cancellationToken, delay, TaskContinuationOptions.None, _taskScheduler),
                cancellationToken);
        }

        public Task LastScheduledTask => _latestTask;
    }
}
