// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class WorkspaceTaskQueue
    {
        private readonly IAsynchronousOperationListener _operationListener;
        private readonly SimpleTaskQueue _queue;

        internal WorkspaceTaskQueue(IAsynchronousOperationListener operationListener, TaskScheduler taskScheduler)
        {
            _operationListener = operationListener;
            _queue = new SimpleTaskQueue(taskScheduler);
        }

        private IAsyncToken BeginOperation(string taskName)
            => _operationListener.BeginAsyncOperation(taskName ?? nameof(WorkspaceTaskQueue) + ".Task");

        private TTask EndOperation<TTask>(IAsyncToken token, TTask task) where TTask : Task
        {
            _ = task.CompletesAsyncOperation(token);
            return task;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods (Task wrappers, not asynchronous methods)
        public Task ScheduleTask(Action operation, string taskName, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), _queue.ScheduleTask(operation, cancellationToken));

        public Task<T> ScheduleTask<T>(Func<T> operation, string taskName, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), _queue.ScheduleTask(operation, cancellationToken));

        public Task ScheduleTask(Func<Task> operation, string taskName, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), _queue.ScheduleTask(operation, cancellationToken));

        public Task<T> ScheduleTask<T>(Func<Task<T>> operation, string taskName, CancellationToken cancellationToken = default)
            => EndOperation(BeginOperation(taskName), _queue.ScheduleTask(operation, cancellationToken));
#pragma warning restore VSTHRD200
    }
}
