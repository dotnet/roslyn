// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This task scheduler will block queuing new tasks if upper bound has met.
    /// </summary>
    internal class DiagnosticEventTaskScheduler : TaskScheduler
    {
        private readonly Task _mainTask;
        private readonly BlockingCollection<Task> _tasks;

        public DiagnosticEventTaskScheduler(int blockingUpperBound)
        {
            _tasks = new BlockingCollection<Task>(blockingUpperBound);

            // portable layer doesnt support explicit thread creation. use long running task to create and hold onto a thread
            _mainTask = Task.Factory.SafeStartNew(Start, CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Start()
        {
            while (true)
            {
                var task = _tasks.Take();
                bool ret = this.TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // NOTE: TPL will ensure only one task ever run when running scheduled task. and since this is only used
            // in diagnostic events, we know task will always run sequencely. so no worry about reverted order here.
            return this.TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // debugger will use this method to get scheduled tasks for this scheduler
            return _tasks.ToArray();
        }
    }
}