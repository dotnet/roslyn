using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class TaskExtensions
    {
        private static readonly Func<Task, Task> TaskIdentity = task => task;

        // TODO: remove this when we move to CLR 4.5
        internal static Task<Task[]> WhenAll(this IEnumerable<Task> tasks)
        {
            // Code graciously provided by Stephen Toub.
            var taskArray = tasks.ToArray();
            if (taskArray.Length == 0)
            {
                var source = new TaskCompletionSource<Task[]>();
                source.SetResult(taskArray);
                return source.Task;
            }

            return Task.Factory.ContinueWhenAll(taskArray, completedTasks =>
            {
                var source = new TaskCompletionSource<Task[]>();
                var exceptions = completedTasks.Where(t => t.IsFaulted).Select(t => t.Exception);
                if (exceptions.Any())
                {
                    source.SetException(exceptions);
                }
                else if (completedTasks.Any(t => t.IsCanceled))
                {
                    source.SetCanceled();
                }
                else
                {
                    source.SetResult(completedTasks);
                }

                return source.Task;
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).UnwrapWithDefault();
        }

        [Obsolete]
        [ExcludeFromCodeCoverage]
        internal static Task<Task> WhenAny(this IEnumerable<Task> tasks)
        {
            return Task.Factory.ContinueWhenAny(
                tasks.ToArray(),
                TaskIdentity,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
