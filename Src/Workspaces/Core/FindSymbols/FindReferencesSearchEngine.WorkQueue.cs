#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Roslyn.Services.FindReferences
{
    internal partial class FindReferencesSearchEngine
    {
        // Code graciously provided by Stephen Toub.
        private Task<IEnumerable<ReferencedSymbol>> ProcessQueueAsync()
        {
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

            return this.foundReferences.Select(kvp => new ReferencedSymbol(kvp.Key, kvp.Value.ToImmutableList())).ToImmutableList();
#endif

            var taskCompletionSource = new TaskCompletionSource<IEnumerable<ReferencedSymbol>>();
            List<Exception> exceptions = null;

            Action<Task> waiter = null;
            waiter = completedTask =>
            {
                while (true)
                {
                    // If the completed task had any exceptions, then store then around so they can
                    // eventually be passed back to the caller.
                    if (completedTask != null && completedTask.IsFaulted)
                    {
                        exceptions = exceptions ?? new List<Exception>();
                        exceptions.AddRange(completedTask.Exception.InnerExceptions);
                    }

                    // Keep trying to pull tasks out of the workqueue.
                    Task task;
                    if (!workQueue.TryDequeue(out task))
                    {
                        // There was no more work to be done. Signal to the task completion that
                        // we're finished
                        if (exceptions != null)
                        {
                            taskCompletionSource.SetException(exceptions);
                        }
                        else
                        {
                            var results = this.foundReferences.Select(kvp => new ReferencedSymbol(kvp.Key, kvp.Value.ToImmutableList())).ToImmutableList();
                            taskCompletionSource.SetResult(results);
                        }

                        // and stop looping.
                        return;
                    }
                    else if (task.IsCompleted)
                    {
                        // There was still work left to be done in the queue.  If it's already completed,
                        // then just continue looping here (so we don't waste time with a SafeContinueWith
                        // call).  Otherwise, hook ourselves up to be called back into when this task
                        // completes.
                        completedTask = task;

                        // Note: this continue is redundant since we'll just fall out of the if/else
                        // and hit the end of the while loop.  however, i'm leaving it in to make it
                        // explicit that this is the behavior we want.
                        continue;
                    }
                    else
                    {
                        // Try this again once the task is actually done.
                        task.SafeContinueWith(waiter, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
                        return;
                    }
                }
            };

            waiter(null);

            return taskCompletionSource.Task;
        }
    }
}
#endif