// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly WorkspaceTaskSchedulerFactory _factory;
            private readonly TaskScheduler _taskScheduler;

            public WorkspaceTaskScheduler(WorkspaceTaskSchedulerFactory factory, TaskScheduler taskScheduler)
            {
                _factory = factory;
                _taskScheduler = taskScheduler;
            }

            private TTask ScheduleTaskWorker<TTask>(
                string taskName, Func<TTask> taskCreator, CancellationToken cancellationToken)
                where TTask : Task
            {
                taskName = taskName ?? GetType().Name + ".ScheduleTask";
                var asyncToken = _factory.BeginAsyncOperation(taskName);

                var task = taskCreator();

                _factory.CompleteAsyncOperation(asyncToken, task);
                return task;
            }

            public Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker<Task>(
                    taskName, () => Task.Factory.SafeStartNew(
    taskAction, cancellationToken, _taskScheduler),
                    cancellationToken);
            }

            public Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker<Task<T>>(
                    taskName, () => Task.Factory.SafeStartNew(
    taskFunc, cancellationToken, _taskScheduler),
                    cancellationToken);
            }

            public Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return ScheduleTaskWorker<Task>(
                    taskName, () => Task.Factory.SafeStartNewFromAsync(
    taskFunc, cancellationToken, _taskScheduler),
                    cancellationToken);
            }

            public Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
            {
                return ScheduleTaskWorker<Task<T>>(
                    taskName, () => Task.Factory.SafeStartNewFromAsync(
    taskFunc, cancellationToken, _taskScheduler),
                    cancellationToken);
            }
        }
    }
}
