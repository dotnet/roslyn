// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// This task scheduler will block queuing new tasks if upper bound has met.
    /// </summary>
    internal class DiagnosticEventTaskScheduler : TaskScheduler
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<Task> _tasks;

        public DiagnosticEventTaskScheduler(int blockingUpperBound)
        {
            _tasks = new BlockingCollection<Task>(blockingUpperBound);

            _thread = new Thread(Start)
            {
                Name = "Roslyn Diagnostics",
                IsBackground = true
            };

            _thread.Start();
        }

        private void Start()
        {
            while (true)
            {
                var task = _tasks.Take();
                var ret = TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
            => _tasks.Add(task);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // NOTE: TPL will ensure only one task ever run when running scheduled task. and since this is only used
            // in diagnostic events, we know task will always run sequencely. so no worry about reverted order here.
            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // debugger will use this method to get scheduled tasks for this scheduler
            return _tasks.ToArray();
        }
    }
}
