// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Default)]
    [Shared]
    internal partial class WorkspaceTaskSchedulerFactory : IWorkspaceTaskSchedulerFactory
    {
        [ImportingConstructor]
        public WorkspaceTaskSchedulerFactory()
        {
        }

        protected virtual TaskScheduler GetCurrentContextScheduler()
            => (SynchronizationContext.Current != null) ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;

        public WorkspaceTaskQueue CreateBackgroundTaskScheduler()
        {
            return new WorkspaceTaskQueue(this, TaskScheduler.Default);
        }

        public WorkspaceTaskQueue CreateEventingTaskQueue()
        {
            return new WorkspaceTaskQueue(this, GetCurrentContextScheduler());
        }

        internal virtual object BeginAsyncOperation(string taskName)
        {
            // do nothing ... overridden by services layer
            return null;
        }

        internal virtual void CompleteAsyncOperation(object asyncToken, Task task)
        {
            // do nothing ... overridden by services layer
        }
    }
}
