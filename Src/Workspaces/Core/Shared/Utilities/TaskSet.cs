using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Represents an unordered set of tasks
    /// </summary>
    internal static partial class TaskSet
    {
        /// <summary>
        /// Starts a task that completes when the initial action and all secondary actions are complete.
        /// </summary>
        public static Task Run(Action<ITaskSet> initialAction, CancellationToken cancellationToken)
        {
            return new TaskSetT<bool>(initialAction, () => true, cancellationToken).CompletionTask;
        }

        /// <summary>
        /// Starts a task that completes when the initial action and all secondary actions are complete.
        /// </summary>
        public static Task<T> Run<T>(Action<ITaskSet> initialAction, Func<T> computeResult, CancellationToken cancellationToken)
        {
            return new TaskSetT<T>(initialAction, computeResult, cancellationToken).CompletionTask;
        }

        /// <summary>
        /// Starts a task that completes when the initial action and all secondary actions are complete.
        /// </summary>
        public static Task Run(Func<ITaskSet, Task> initialAction, CancellationToken cancellationToken)
        {
            return new TaskSetT<bool>(initialAction, () => true, cancellationToken).CompletionTask;
        }

        /// <summary>
        /// Starts a task that completes when the initial action and all secondary actions are complete.
        /// </summary>
        public static Task<T> Run<T>(Func<ITaskSet, Task> initialAction, Func<T> computeResult, CancellationToken cancellationToken)
        {
            return new TaskSetT<T>(initialAction, computeResult, cancellationToken).CompletionTask;
        }
    }
}