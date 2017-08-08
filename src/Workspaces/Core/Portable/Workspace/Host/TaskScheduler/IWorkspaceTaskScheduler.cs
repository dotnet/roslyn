// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute the task function on a thread owned by a task scheduler and return the schedule
        /// task that can be used to wait for the result.
        /// </summary>
        Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default);
    }
}
