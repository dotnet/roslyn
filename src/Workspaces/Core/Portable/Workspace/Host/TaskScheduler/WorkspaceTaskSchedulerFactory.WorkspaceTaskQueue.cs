// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class WorkspaceTaskQueue
    {
        private readonly WorkspaceTaskSchedulerFactory _factory;
        private readonly SimpleTaskQueue _queue;

        internal WorkspaceTaskQueue(WorkspaceTaskSchedulerFactory factory, TaskScheduler taskScheduler)
        {
            _factory = factory;
            _queue = new SimpleTaskQueue(taskScheduler);
        }

        private TTask ScheduleTask<TOperation, TTask>(Func<TOperation, CancellationToken, TTask> taskScheduler, string taskName, TOperation operation, CancellationToken cancellationToken)
            where TTask : Task
        {
            var asyncToken = _factory.BeginAsyncOperation(taskName ?? GetType().Name + ".Task");

            var task = taskScheduler(operation, cancellationToken);

            _factory.CompleteAsyncOperation(asyncToken, task);
            return task;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods (Task wrappers, not asynchronous methods)
        public Task ScheduleTask(Action operation, string taskName, CancellationToken cancellationToken = default)
        {
            return ScheduleTask((operation, cancellationToken) => _queue.ScheduleTask(operation, cancellationToken), taskName, operation, cancellationToken);
        }

        public Task<T> ScheduleTask<T>(Func<T> operation, string taskName, CancellationToken cancellationToken = default)
        {
            return ScheduleTask((operation, cancellationToken) => _queue.ScheduleTask(operation, cancellationToken), taskName, operation, cancellationToken);
        }

        public Task ScheduleTask(Func<Task> operation, string taskName, CancellationToken cancellationToken = default)
        {
            return ScheduleTask((operation, cancellationToken) => _queue.ScheduleTask(operation, cancellationToken), taskName, operation, cancellationToken);
        }

        public Task<T> ScheduleTask<T>(Func<Task<T>> operation, string taskName, CancellationToken cancellationToken = default)
        {
            return ScheduleTask((operation, cancellationToken) => _queue.ScheduleTask(operation, cancellationToken), taskName, operation, cancellationToken);
        }
#pragma warning restore VSTHRD200
    }
}
