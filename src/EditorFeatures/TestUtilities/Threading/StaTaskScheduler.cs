// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities
{
    public sealed class StaTaskScheduler : TaskScheduler, IDisposable
    {
        /// <summary>Gets a <see cref="StaTaskScheduler"/> for the current <see cref="AppDomain"/>.</summary>
        /// <remarks>We use a count of 1, because the editor ends up re-using <see cref="DispatcherObject"/>
        /// instances between tests, so we need to always use the same thread for our Sta tests.</remarks>
        public static StaTaskScheduler DefaultSta { get; } = new StaTaskScheduler();

        /// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
        private BlockingCollection<Task> QueuedTasks;

        /// <summary>The STA threads used by the scheduler.</summary>
        public Thread StaThread { get; }

        public bool IsRunningInScheduler => StaThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

        /// <summary>Initializes a new instance of the <see cref="StaTaskScheduler"/> class.</summary>
        public StaTaskScheduler()
        {
            // Initialize the tasks collection
            QueuedTasks = new BlockingCollection<Task>();

            // Create the threads to be used by this scheduler
            StaThread = new Thread(() =>
            {
                // Continually get the next task and try to execute it.
                // This will continue until the scheduler is disposed and no more tasks remain.
                foreach (var t in QueuedTasks.GetConsumingEnumerable())
                {
                    if (!TryExecuteTask(t))
                    {
                        Debug.Assert(t.IsCompleted, "Can't run, not completed");
                    }
                }
            })
            {
                Name = $"{nameof(StaTaskScheduler)} thread",
                IsBackground = true
            };
            StaThread.SetApartmentState(ApartmentState.STA);
            StaThread.Start();
        }

        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            // Push it into the blocking collection of tasks
            QueuedTasks.Add(task);
        }

        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Serialize the contents of the blocking collection of tasks for the debugger
            return QueuedTasks.ToArray();
        }

        /// <summary>Determines whether a Task may be inlined.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Try to inline if the current thread is STA
            return
                Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
                TryExecuteTask(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => 1;

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (QueuedTasks != null)
            {
                // Indicate that no new tasks will be coming in
                QueuedTasks.CompleteAdding();

                // Wait for all threads to finish processing tasks
                StaThread.Join();

                // Cleanup
                QueuedTasks.Dispose();
                QueuedTasks = null;
            }
        }
    }
}
