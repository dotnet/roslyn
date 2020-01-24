// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory that creates either sequential or parallel task schedulers.
    /// </summary>
    internal interface IWorkspaceTaskSchedulerFactory : IWorkspaceService
    {
        /// <summary>
        /// Creates a workspace task scheduler that schedules tasks to run in parallel on the background.
        /// </summary>
        IWorkspaceTaskScheduler CreateBackgroundTaskScheduler();

        /// <summary>
        /// Creates a workspace task scheduler that schedules task to run in sequence to be used for raising
        /// workspace events.
        /// </summary>
        IWorkspaceTaskScheduler CreateEventingTaskQueue();
    }
}
