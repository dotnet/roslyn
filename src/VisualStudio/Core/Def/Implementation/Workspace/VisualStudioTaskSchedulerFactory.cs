// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioTaskSchedulerFactory : EditorTaskSchedulerFactory
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTaskSchedulerFactory(IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider)
            : base(listenerProvider)
        {
            _threadingContext = threadingContext;
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
            private readonly VisualStudioTaskSchedulerFactory _factory;

            public VisualStudioTaskScheduler(VisualStudioTaskSchedulerFactory factory)
            {
                _factory = factory;
                _queue = new Lazy<WorkspaceTaskQueue>(CreateQueue);
            }

            private WorkspaceTaskQueue CreateQueue()
            {
                // At this point, we have to know what the UI thread is.
                Contract.ThrowIfFalse(_factory._threadingContext.HasMainThread);
                return new WorkspaceTaskQueue(_factory, new JoinableTaskFactoryTaskScheduler(_factory._threadingContext.JoinableTaskFactory));
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

        private class JoinableTaskFactoryTaskScheduler : TaskScheduler
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;

            public JoinableTaskFactoryTaskScheduler(JoinableTaskFactory joinableTaskFactory)
            {
                _joinableTaskFactory = joinableTaskFactory;
            }

            public override int MaximumConcurrencyLevel => 1;

            protected override IEnumerable<Task> GetScheduledTasks() => null;

            protected override void QueueTask(Task task)
            {
                _joinableTaskFactory.RunAsync(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
                    TryExecuteTask(task);
                });
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                if (_joinableTaskFactory.Context.IsOnMainThread)
                {
                    return TryExecuteTask(task);
                }

                return false;
            }
        }
    }
}
