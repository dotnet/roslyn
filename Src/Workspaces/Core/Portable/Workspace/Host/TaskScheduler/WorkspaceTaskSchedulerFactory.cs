// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class WorkspaceTaskSchedulerFactory : IWorkspaceTaskSchedulerFactory
    {
        public virtual IWorkspaceTaskScheduler CreateTaskScheduler(TaskScheduler taskScheduler = null)
        {
            if (taskScheduler == null)
            {
                taskScheduler = (SynchronizationContext.Current != null)
                    ? TaskScheduler.FromCurrentSynchronizationContext()
                    : TaskScheduler.Default;
            }

            return new WorkspaceTaskScheduler(this, taskScheduler);
        }

        public virtual IWorkspaceTaskScheduler CreateTaskQueue(TaskScheduler taskScheduler = null)
        {
            if (taskScheduler == null)
            {
                taskScheduler = (SynchronizationContext.Current != null)
                    ? TaskScheduler.FromCurrentSynchronizationContext()
                    : TaskScheduler.Default;
            }

            return new WorkspaceTaskQueue(this, taskScheduler);
        }

        protected virtual object BeginAsyncOperation(string taskName)
        {
            // do nothing ... overridden by services layer
            return null;
        }

        protected virtual void CompleteAsyncOperation(object asyncToken, Task task)
        {
            // do nothing ... overridden by services layer
        }

        private class WorkspaceTaskScheduler : IWorkspaceTaskScheduler
        {
            private readonly WorkspaceTaskSchedulerFactory factory;
            private readonly TaskScheduler taskScheduler;

            public WorkspaceTaskScheduler(WorkspaceTaskSchedulerFactory factory, TaskScheduler taskScheduler)
            {
                this.factory = factory;
                this.taskScheduler = taskScheduler;
            }

            private TTask ScheduleTaskWorker<TTask>(
                string taskName, CancellationToken cancellationToken, Func<TTask> taskCreator)
                where TTask : Task
            {
                taskName = taskName ?? GetType().Name + ".ScheduleTask";
                var asyncToken = factory.BeginAsyncOperation(taskName);

                var task = taskCreator();

                factory.CompleteAsyncOperation(asyncToken, task);
                return task;
            }

            public Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker<Task>(
                    taskName, cancellationToken,
                    () => Task.Factory.SafeStartNew(
                        taskAction, cancellationToken, this.taskScheduler));
            }

            public Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker<Task<T>>(
                    taskName, cancellationToken,
                    () => Task.Factory.SafeStartNew(
                        taskFunc, cancellationToken, this.taskScheduler));
            }

            public Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return ScheduleTaskWorker<Task>(
                    taskName, cancellationToken,
                    () => Task.Factory.SafeStartNewFromAsync(
                        taskFunc, cancellationToken, this.taskScheduler));
            }

            public Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return ScheduleTaskWorker<Task<T>>(
                    taskName, cancellationToken,
                    () => Task.Factory.SafeStartNewFromAsync(
                        taskFunc, cancellationToken, this.taskScheduler));
            }
        }
    }
}