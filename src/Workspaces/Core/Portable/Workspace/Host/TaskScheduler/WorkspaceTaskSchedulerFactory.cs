// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Default)]
    [Shared]
    internal class WorkspaceTaskSchedulerFactory : IWorkspaceTaskSchedulerFactory
    {
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public WorkspaceTaskSchedulerFactory(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        }

        protected virtual TaskScheduler GetCurrentContextScheduler()
            => (SynchronizationContext.Current != null) ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;

        public WorkspaceTaskQueue CreateBackgroundTaskScheduler()
        {
            return new WorkspaceTaskQueue(_listener, TaskScheduler.Default);
        }

        public WorkspaceTaskQueue CreateEventingTaskQueue()
        {
            return new WorkspaceTaskQueue(_listener, GetCurrentContextScheduler());
        }
    }
}
