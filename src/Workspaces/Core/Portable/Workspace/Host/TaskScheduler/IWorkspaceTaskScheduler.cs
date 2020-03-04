// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// An abstraction for running tasks either in sequence or in parallel.
    /// </summary>
    internal interface IWorkspaceTaskScheduler
    {
        /// <summary>
        /// Execute the task action on a thread owned by a task scheduler.
        /// </summary>
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default);
    }
}
