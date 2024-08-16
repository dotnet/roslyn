// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

[ExportWorkspaceService(typeof(ITaskSchedulerProvider), ServiceLayer.Editor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ThreadingContextTaskSchedulerProvider(IThreadingContext threadingContext) : ITaskSchedulerProvider
{
    public TaskScheduler CurrentContextScheduler { get; } = threadingContext.HasMainThread
            ? new JoinableTaskFactoryTaskScheduler(threadingContext.JoinableTaskFactory)
            : TaskScheduler.Default;

    private sealed class JoinableTaskFactoryTaskScheduler(JoinableTaskFactory joinableTaskFactory) : TaskScheduler
    {
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;

        public override int MaximumConcurrencyLevel => 1;

        protected override IEnumerable<Task> GetScheduledTasks()
            => [];

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
