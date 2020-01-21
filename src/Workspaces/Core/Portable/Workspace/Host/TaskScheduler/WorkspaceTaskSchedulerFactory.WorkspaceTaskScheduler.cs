// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class WorkspaceTaskSchedulerFactory
    {
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
                string taskName, Func<TTask> taskCreator)
                where TTask : Task
            {
                taskName ??= GetType().Name + ".ScheduleTask";
                var asyncToken = _factory.BeginAsyncOperation(taskName);

                var task = taskCreator();

                _factory.CompleteAsyncOperation(asyncToken, task);
                return task;
            }

            public Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker(
                    taskName, () => Task.Factory.SafeStartNew(taskAction, cancellationToken, _taskScheduler));
            }

            public Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken)
            {
                return ScheduleTaskWorker(
                    taskName, () => Task.Factory.SafeStartNew(taskFunc, cancellationToken, _taskScheduler));
            }

            public Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default)
            {
                return ScheduleTaskWorker(
                    taskName, () => Task.Factory.SafeStartNewFromAsync(taskFunc, cancellationToken, _taskScheduler));
            }

            public Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default)
            {
                return ScheduleTaskWorker(
                    taskName, () => Task.Factory.SafeStartNewFromAsync(taskFunc, cancellationToken, _taskScheduler));
            }
        }
    }
}
