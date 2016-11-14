// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

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
    }
}
