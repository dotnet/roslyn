// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal class PrioritizedTaskScheduler : TaskScheduler
    {
        public static readonly TaskScheduler AboveNormalInstance = new PrioritizedTaskScheduler(ThreadPriority.AboveNormal);
        public static readonly TaskScheduler BelowNormalInstance = new PrioritizedTaskScheduler(ThreadPriority.BelowNormal);

        private readonly Thread thread;
        private readonly BlockingCollection<Task> tasks = new BlockingCollection<Task>();

        private PrioritizedTaskScheduler(ThreadPriority priority)
        {
            this.thread = new Thread(ThreadStart)
            {
                Priority = priority,
                IsBackground = true,
                Name = this.GetType().Name + "-" + priority,
            };

            thread.Start();
        }

        private void ThreadStart()
        {
            while (true)
            {
                var task = tasks.Take();
                this.TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            tasks.Add(task);
        }

        // A class derived from TaskScheduler implements this function to support inline execution
        // of a task on a thread that initiates a wait on that task object.
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
#if false
            // If this task wasn't queued into us, then it's completely safe for the thread that is
            // waiting on it run the task.
            if (!taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }

            // This task was queued into us.  If we're trying to inline onto *the* thread that we 
            // own, the this is ok.  However, if this is a different thread, then we should not try
            // to inline this task as then our thread might execute it at the same time that that
            // other thread inlines it.
            if (this.thread == Thread.CurrentThread)
            {
                return TryExecuteTask(task);
            }

            // We can't inline this task.
            return false;
#endif

            // NOTE(cyrusn): There is no race condition here.  While our dedicated thread may try to 
            // call "TryExecuteTask" on this task above *and* we allow another "Wait"ing thread to 
            // execute it, the TPL ensures that only one will ever get a go.  And, since we have no
            // ordering guarantees (or other constraints) we're happy to let some other thread try
            // to execute this task.  It means less work for us, and it makes that other thred not
            // be blocked.
            return this.TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // NOTE(cyrusn): This method is only for debugging purposes.
            return tasks.ToArray();
        }
    }
}