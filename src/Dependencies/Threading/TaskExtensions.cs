// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Threading;

internal static partial class TaskExtensions
{
    // Following code is copied from Microsoft.VisualStudio.Threading.TplExtensions (renamed to avoid ambiguity)
    // https://github.com/microsoft/vs-threading/blob/main/src/Microsoft.VisualStudio.Threading/TplExtensions.cs

    /// <summary>
    /// Returns an awaitable for the specified task that will never throw, even if the source task
    /// faults or is canceled.
    /// </summary>
    /// <param name="task">The task whose completion should signal the completion of the returned awaitable.</param>
    /// <param name="captureContext">if set to <c>true</c> the continuation will be scheduled on the caller's context; <c>false</c> to always execute the continuation on the threadpool.</param>
    /// <returns>An awaitable.</returns>
    public static NoThrowTaskAwaitable NoThrowAwaitableInternal(this Task task, bool captureContext = true)
    {
        return new NoThrowTaskAwaitable(task, captureContext);
    }

    /// <summary>
    /// An awaitable that wraps a task and never throws an exception when waited on.
    /// </summary>
    public readonly struct NoThrowTaskAwaitable
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly Task _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowTaskAwaitable" /> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">Whether the continuation should be scheduled on the current sync context.</param>
        public NoThrowTaskAwaitable(Task task, bool captureContext)
        {
            _task = task;
            _captureContext = captureContext;
        }

        /// <summary>
        /// Gets the awaiter.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public NoThrowTaskAwaiter GetAwaiter()
        {
            return new NoThrowTaskAwaiter(_task, _captureContext);
        }
    }

    /// <summary>
    /// An awaiter that wraps a task and never throws an exception when waited on.
    /// </summary>
    public readonly struct NoThrowTaskAwaiter : ICriticalNotifyCompletion
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly Task _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowTaskAwaiter"/> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">if set to <c>true</c> [capture context].</param>
        public NoThrowTaskAwaiter(Task task, bool captureContext)
        {
            _task = task;
            _captureContext = captureContext;
        }

        /// <summary>
        /// Gets a value indicating whether the task has completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return _task.IsCompleted; }
        }

        /// <summary>
        /// Schedules a delegate for execution at the conclusion of a task's execution.
        /// </summary>
        /// <param name="continuation">The action.</param>
        public void OnCompleted(Action continuation)
        {
            _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(continuation);
        }

        /// <summary>
        /// Schedules a delegate for execution at the conclusion of a task's execution
        /// without capturing the ExecutionContext.
        /// </summary>
        /// <param name="continuation">The action.</param>
        public void UnsafeOnCompleted(Action continuation)
        {
            _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(continuation);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
        public void GetResult()
#pragma warning restore CA1822 // Mark members as static
        {
            // Never throw here.
        }
    }
}
