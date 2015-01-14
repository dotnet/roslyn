// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory that creates either sequential or parallel task schedulers.
    /// </summary>
    internal interface IWorkspaceTaskSchedulerFactory : IWorkspaceService
    {
        /// <summary>
        /// Creates a workspace task scheduler that schedules tasks to run in parallel.
        /// </summary>
        IWorkspaceTaskScheduler CreateTaskScheduler(TaskScheduler taskScheduler = null);

        /// <summary>
        /// Creates a workspace task scheduler that schedules task to run in sequence.
        /// </summary>
        IWorkspaceTaskScheduler CreateTaskQueue(TaskScheduler taskScheduler = null);
    }
}
