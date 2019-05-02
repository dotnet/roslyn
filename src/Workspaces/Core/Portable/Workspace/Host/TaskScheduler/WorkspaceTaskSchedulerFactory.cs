// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host.Mef;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Default)]
    [Shared]
    internal partial class WorkspaceTaskSchedulerFactory : IWorkspaceTaskSchedulerFactory
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public WorkspaceTaskSchedulerFactory()
        {
        }

        public virtual IWorkspaceTaskScheduler CreateBackgroundTaskScheduler()
        {
            return new WorkspaceTaskScheduler(this, TaskScheduler.Default);
        }

        public virtual IWorkspaceTaskScheduler CreateEventingTaskQueue()
        {
            var taskScheduler = (SynchronizationContext.Current != null)
                ? TaskScheduler.FromCurrentSynchronizationContext()
                : TaskScheduler.Default;

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
