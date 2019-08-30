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
            return new WorkspaceTaskQueue(this, new JoinableTaskFactoryTaskScheduler(_threadingContext.JoinableTaskFactory));
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
