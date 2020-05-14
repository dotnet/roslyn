// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(ITaskSchedulerProvider), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioTaskSchedulerProvider : ITaskSchedulerProvider
    {
        public TaskScheduler CurrentContextScheduler { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTaskSchedulerProvider(IThreadingContext threadingContext)
            => CurrentContextScheduler = new JoinableTaskFactoryTaskScheduler(threadingContext.JoinableTaskFactory);

        private sealed class JoinableTaskFactoryTaskScheduler : TaskScheduler
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;

            public JoinableTaskFactoryTaskScheduler(JoinableTaskFactory joinableTaskFactory)
                => _joinableTaskFactory = joinableTaskFactory;

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
