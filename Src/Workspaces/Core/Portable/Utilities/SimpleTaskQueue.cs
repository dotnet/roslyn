// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// schedules task to run in sequence.
    /// </summary>
    internal sealed class SimpleTaskQueue
    {
        private readonly TaskScheduler taskScheduler;

        /// <summary>
        /// An object to synchronize reads/writes of all mutable fields of this class.
        /// </summary>
        private readonly object gate = new object();

        private Task latestTask;
        private int taskCount;

        public SimpleTaskQueue(TaskScheduler taskScheduler)
        {
            this.taskScheduler = taskScheduler;

            this.taskCount = 0;
            this.latestTask = SpecializedTasks.EmptyTask;
        }

        private TTask ScheduleTaskWorker<TTask>(CancellationToken cancellationToken, Func<int, TTask> taskCreator)
            where TTask : Task
        {
            lock (gate)
            {
                taskCount++;
                int delay = (taskCount % 100) == 0 ? 1 : 0;

                var task = taskCreator(delay);

                this.latestTask = task;

                return task;
            }
        }

        public Task ScheduleTask(Action taskAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ScheduleTaskWorker<Task>(cancellationToken,
                delay => this.latestTask.ContinueWithAfterDelay(
                    taskAction, cancellationToken, delay, TaskContinuationOptions.None, this.taskScheduler));
        }

        public Task<T> ScheduleTask<T>(Func<T> taskFunc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ScheduleTaskWorker<Task<T>>(cancellationToken,
                delay => this.latestTask.ContinueWithAfterDelay(
                    t => taskFunc(), cancellationToken, delay, TaskContinuationOptions.None, this.taskScheduler));
        }

        public Task ScheduleTask(Func<Task> taskFuncAsync, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ScheduleTaskWorker<Task>(cancellationToken,
                delay => this.latestTask.ContinueWithAfterDelayFromAsync(
                    t => taskFuncAsync(), cancellationToken, delay, TaskContinuationOptions.None, this.taskScheduler));
        }

        public Task<T> ScheduleTask<T>(Func<Task<T>> taskFuncAsync, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ScheduleTaskWorker<Task<T>>(cancellationToken,
                delay => this.latestTask.ContinueWithAfterDelayFromAsync(
                    t => taskFuncAsync(), cancellationToken, delay, TaskContinuationOptions.None, this.taskScheduler));
        }

        public Task LastScheduledTask
        {
            get
            {
                return this.latestTask;
            }
        }
    }
}
