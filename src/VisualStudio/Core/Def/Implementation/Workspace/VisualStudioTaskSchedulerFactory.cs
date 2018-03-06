// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;
using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioTaskSchedulerFactory : EditorTaskSchedulerFactory
    {
        [ImportingConstructor]
        public VisualStudioTaskSchedulerFactory(IAsynchronousOperationListenerProvider listenerProvider)
            : base(listenerProvider)
        {
        }

        public override IWorkspaceTaskScheduler CreateEventingTaskQueue()
        {
            // When we are creating the workspace, we might not actually have established what the UI thread is, since
            // we might be getting created via MEF. So we'll allow the queue to be created now, and once we actually need
            // to queue something we'll then start using the task queue from there.
            // In Visual Studio, we raise these events on the UI thread. At this point we should know
            // exactly which thread that is.
            return new VisualStudioTaskScheduler(this);
        }

        private class VisualStudioTaskScheduler : IWorkspaceTaskScheduler
        {
            private readonly Lazy<WorkspaceTaskQueue> _queue;
            private readonly WorkspaceTaskSchedulerFactory _factory;

            public VisualStudioTaskScheduler(WorkspaceTaskSchedulerFactory factory)
            {
                _factory = factory;
                _queue = new Lazy<WorkspaceTaskQueue>(CreateQueue);
            }

            private WorkspaceTaskQueue CreateQueue()
            {
                // At this point, we have to know what the UI thread is.
                Contract.ThrowIfTrue(ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.Kind == ForegroundThreadDataKind.Unknown);
                return new WorkspaceTaskQueue(_factory, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler);
            }

            public Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken = default)
            {
                return _queue.Value.ScheduleTask(taskAction, taskName, cancellationToken);
            }

            public Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default)
            {
                return _queue.Value.ScheduleTask(taskFunc, taskName, cancellationToken);
            }

            public Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default)
            {
                return _queue.Value.ScheduleTask(taskFunc, taskName, cancellationToken);
            }

            public Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default)
            {
                return _queue.Value.ScheduleTask(taskFunc, taskName, cancellationToken);
            }
        }
    }
}
