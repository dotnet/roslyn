using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class TaskSet
    {
        private class TaskSetT<T> : ITaskSet
        {
            private readonly ConcurrentQueue<Task> workQueue = new ConcurrentQueue<Task>();
            private readonly TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            private readonly Func<T> computeResult;

            public TaskSetT(Action<ITaskSet> action, Func<T> computeResult, CancellationToken cancellationToken)
            {
                this.computeResult = computeResult;
                this.AddTask(action, cancellationToken);
                this.StartProcessingQueue();
            }

            public TaskSetT(Func<ITaskSet, Task> action, Func<T> computeResult, CancellationToken cancellationToken)
            {
                this.computeResult = computeResult;
                this.AddTask(action, cancellationToken);
                this.StartProcessingQueue();
            }

            public Task<T> CompletionTask
            {
                get { return this.taskCompletionSource.Task; }
            }

            public Task AddTask(Action<ITaskSet> action, CancellationToken cancellationToken)
            {
                var task = Task.Factory.SafeStartNew(() => action(this), cancellationToken, TaskScheduler.Default);
                this.workQueue.Enqueue(task);
                return task;
            }

            public Task AddTask(Func<ITaskSet, Task> action, CancellationToken cancellationToken)
            {
                var task = Task.Factory.SafeStartNew(() => action(this), cancellationToken, TaskScheduler.Default).Unwrap();
                this.workQueue.Enqueue(task);
                return task;
            }

            // Code graciously provided by Stephen Toub.
            private void StartProcessingQueue()
            {
                List<Exception> exceptions = null;

                // Now we need to figure out when we're "done".  We keep on pulling tasks out of the
                // queue and waiting for them to complete.  If the task is already completed, we just
                // move onto the next task.  Otherwise, we ask to be notified when it completes and we
                // continue trying to pull off tasks.  Once there are no more tasks to pull off, then
                // we're actually done. 

                // Note: in C# 5.0 we can rewrite this as:
#if false
            Task task;
            while (workQueue.TryDequeue(out task))
            {
                await task;
            }
#endif

                Action<Task> waiter = null;
                waiter = completedTask =>
                {
                    while (true)
                    {
                        // If the completed task had any exceptions, then store then around so they can
                        // eventually be passed back to the caller.
                        if (completedTask != null)
                        {
                            if (completedTask.IsFaulted)
                            {
                                exceptions = exceptions ?? new List<Exception>();
                                exceptions.AddRange(completedTask.Exception.InnerExceptions);
                            }
                            else if (completedTask.IsCanceled)
                            {
                                taskCompletionSource.SetCanceled();
                                return;
                            }
                        }

                        // Keep trying to pull tasks out of the work queue.
                        Task nextTask = null;
                        if (!this.workQueue.TryDequeue(out nextTask))
                        {
                            // There was no more work to be done. Signal to the task completion that
                            // we're finished
                            if (exceptions != null)
                            {
                                taskCompletionSource.SetException(exceptions);
                            }
                            else
                            {
                                var result = this.computeResult();
                                taskCompletionSource.SetResult(result);
                            }

                            // and stop looping.
                            return;
                        }
                        else if (nextTask.IsCompleted)
                        {
                            // There was still work left to be done in the queue.  If it's already completed,
                            // then just continue looping here (so we don't waste time with a SafeContinueWith
                            // call).  Otherwise, hook ourselves up to be called back into when this task
                            // completes.
                            completedTask = nextTask;

                            // Note: this continue is redundant since we'll just fall out of the if/else
                            // and hit the end of the while loop.  however, I'm leaving it in to make it
                            // explicit that this is the behavior we want.
                            continue;
                        }
                        else
                        {
                            // Try this again once the task is actually done.
                            nextTask.SafeContinueWith(waiter, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                            return;
                        }
                    }
                };

                waiter(null);
            }
        }
    }
}