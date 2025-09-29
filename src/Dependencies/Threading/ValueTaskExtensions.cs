// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Threading;

internal static class ValueTaskExtensions
{
    // Following code is copied from Microsoft.VisualStudio.Threading.TplExtensions (renamed to avoid ambiguity)
    // https://github.com/microsoft/vs-threading/blob/main/src/Microsoft.VisualStudio.Threading/TplExtensions.cs

    /// <summary>
    /// Returns an awaitable for the specified task that will never throw, even if the source task
    /// faults or is canceled.
    /// </summary>
    /// <param name="task">The task whose completion should signal the completion of the returned awaitable.</param>
    /// <param name="captureContext">if set to <see langword="true"/> the continuation will be scheduled on the caller's context; <see langword="false"/> to always execute the continuation on the threadpool.</param>
    /// <returns>An awaitable.</returns>
    public static NoThrowValueTaskAwaitable NoThrowAwaitableInternal(this ValueTask task, bool captureContext = true)
    {
        return new NoThrowValueTaskAwaitable(task, captureContext);
    }

    /// <summary>
    /// Returns an awaitable for the specified task that will never throw, even if the source task
    /// faults or is canceled.
    /// </summary>
    /// <remarks>
    /// The awaitable returned by this method does not provide access to the result of a successfully-completed
    /// <see cref="ValueTask{TResult}"/>. To await without throwing and use the resulting value, the following
    /// pattern may be used:
    ///
    /// <code>
    /// var methodValueTask = MethodAsync().Preserve();
    /// await methodValueTask.NoThrowAwaitableInternal(true);
    /// if (methodValueTask.IsCompletedSuccessfully)
    /// {
    ///   var result = methodValueTask.Result;
    /// }
    /// else
    /// {
    ///   var exception = methodValueTask.AsTask().Exception.InnerException;
    /// }
    /// </code>
    /// </remarks>
    /// <param name="task">The task whose completion should signal the completion of the returned awaitable.</param>
    /// <param name="captureContext">if set to <see langword="true"/> the continuation will be scheduled on the caller's context; <see langword="false"/> to always execute the continuation on the threadpool.</param>
    /// <returns>An awaitable.</returns>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public static NoThrowValueTaskAwaitable<TResult> NoThrowAwaitableInternal<TResult>(this ValueTask<TResult> task, bool captureContext = true)
    {
        return new NoThrowValueTaskAwaitable<TResult>(task, captureContext);
    }

    /// <summary>
    /// An awaitable that wraps a task and never throws an exception when waited on.
    /// </summary>
    public readonly struct NoThrowValueTaskAwaitable
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly ValueTask _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowValueTaskAwaitable"/> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">Whether the continuation should be scheduled on the current sync context.</param>
        public NoThrowValueTaskAwaitable(ValueTask task, bool captureContext)
        {
            _task = task.Preserve();
            _captureContext = captureContext;
        }

        /// <summary>
        /// Gets the awaiter.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public NoThrowValueTaskAwaiter GetAwaiter()
        {
            return new NoThrowValueTaskAwaiter(_task, _captureContext);
        }
    }

    /// <summary>
    /// An awaiter that wraps a task and never throws an exception when waited on.
    /// </summary>
    public readonly struct NoThrowValueTaskAwaiter : ICriticalNotifyCompletion
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly ValueTask _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowValueTaskAwaiter"/> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">if set to <see langword="true"/> [capture context].</param>
        public NoThrowValueTaskAwaiter(ValueTask task, bool captureContext)
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
        public void GetResult()
        {
            // No need to do anything with '_task' because we already called Preserve on it.
        }
    }

    /// <summary>
    /// An awaitable that wraps a <see cref="ValueTask{TResult}"/> and never throws an exception when waited on.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public readonly struct NoThrowValueTaskAwaitable<TResult>
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly ValueTask<TResult> _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowValueTaskAwaitable{TResult}" /> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">Whether the continuation should be scheduled on the current sync context.</param>
        public NoThrowValueTaskAwaitable(ValueTask<TResult> task, bool captureContext)
        {
            _task = task.Preserve();
            _captureContext = captureContext;
        }

        /// <summary>
        /// Gets the awaiter.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public NoThrowValueTaskAwaiter<TResult> GetAwaiter()
        {
            return new NoThrowValueTaskAwaiter<TResult>(_task, _captureContext);
        }
    }

    /// <summary>
    /// An awaiter that wraps a task and never throws an exception when waited on.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public readonly struct NoThrowValueTaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        /// <summary>
        /// The task.
        /// </summary>
        private readonly ValueTask<TResult> _task;

        /// <summary>
        /// A value indicating whether the continuation should be scheduled on the current sync context.
        /// </summary>
        private readonly bool _captureContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoThrowValueTaskAwaiter{TResult}"/> struct.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="captureContext">if set to <see langword="true"/> [capture context].</param>
        public NoThrowValueTaskAwaiter(ValueTask<TResult> task, bool captureContext)
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
        public void GetResult()
        {
            // No need to do anything with '_task' because we already called Preserve on it.
        }
    }
}
