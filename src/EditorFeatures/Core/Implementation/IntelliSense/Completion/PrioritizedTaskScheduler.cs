// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class PrioritizedTaskScheduler : TaskScheduler
    {
        public static readonly TaskScheduler AboveNormalInstance = new PrioritizedTaskScheduler(ThreadPriority.AboveNormal);

        private readonly Thread _thread;
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        private PrioritizedTaskScheduler(ThreadPriority priority)
        {
            _thread = new Thread(ThreadStart)
            {
                Priority = priority,
                IsBackground = true,
                Name = this.GetType().Name + "-" + priority,
            };

            _thread.Start();
        }

        private void ThreadStart()
        {
            while (true)
            {
                var task = _tasks.Take();
                bool ret = this.TryExecuteTask(task);
                Debug.Assert(ret);
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        // A class derived from TaskScheduler implements this function to support inline execution
        // of a task on a thread that initiates a wait on that task object.
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // NOTE(cyrusn): There is no race condition here.  While our dedicated thread may try to 
            // call "TryExecuteTask" on this task above *and* we allow another "Wait"ing thread to 
            // execute it, the TPL ensures that only one will ever get a go.  And, since we have no
            // ordering guarantees (or other constraints) we're happy to let some other thread try
            // to execute this task.  It means less work for us, and it makes that other thread not
            // be blocked.
            return this.TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // NOTE(cyrusn): This method is only for debugging purposes.
            return _tasks.ToArray();
        }
    }
}
