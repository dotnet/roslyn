using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal class RoslynUITaskScheduler : TaskScheduler
    {
        private static TaskScheduler instance;

        private readonly SynchronizationContext context;
        private readonly ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>();
        private readonly ConcurrentQueue<Task> failed = new ConcurrentQueue<Task>();

        private Task current;

        public static TaskScheduler Create()
        {
            if (instance != null)
            {
                return instance;
            }

            var context = SynchronizationContext.Current;
            if (context == null)
            {
                return Default;
            }

            instance = new RoslynUITaskScheduler(context);
            return instance;
        }

        private RoslynUITaskScheduler(SynchronizationContext context)
        {
            this.context = context;
        }

        protected override void QueueTask(Task task)
        {
            tasks.Enqueue(task);

            context.Post(t =>
            {
                if (tasks.TryDequeue(out current))
                {
                    if (!this.TryExecuteTask(current))
                    {
                        failed.Enqueue(current);
                    }
                }
            }, tasks);
        }

        // A class derived from TaskScheduler implements this function to support inline execution
        // of a task on a thread that initiates a wait on that task object.
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (SynchronizationContext.Current == context)
            {
                return TryExecuteTask(task);
            }
            else
            {
                return false;
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // NOTE(cyrusn): This method is only for debugging purposes.
            return tasks.ToArray();
        }

        public override int MaximumConcurrencyLevel
        {
            get
            {
                return 1;
            }
        }
    }
}
