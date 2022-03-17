// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    [SuppressMessage("ApiDesign", "CA1068", Justification = "Matching TPL Signatures")]
    internal static partial class TaskExtensions
    {
        public static T WaitAndGetResult<T>(this Task<T> task, CancellationToken cancellationToken)
        {
#if DEBUG
            if (Thread.CurrentThread.IsThreadPoolThread)
            {
                // If you hit this when running tests then your code is in error.  WaitAndGetResult
                // should only be called from a foreground thread.  There are a few ways you may 
                // want to fix this.
                //
                // First, if you're actually calling this directly *in test code* then you could 
                // either:
                //
                //  1) Mark the test with [WpfFact].  This is not preferred, and should only be
                //     when testing an actual UI feature (like command handlers).
                //  2) Make the test actually async (preferred).
                //
                // If you are calling WaitAndGetResult from product code, then that code must
                // be a foreground thread (i.e. a command handler).  It cannot be from a threadpool
                // thread *ever*.
                throw new InvalidOperationException($"{nameof(WaitAndGetResult)} cannot be called from a thread pool thread.");
            }
#endif

            return WaitAndGetResult_CanCallOnBackground(task, cancellationToken);
        }

        // Only call this *extremely* special situations.  This will synchronously block a threadpool
        // thread.  In the future we are going ot be removing this and disallowing its use.
        public static T WaitAndGetResult_CanCallOnBackground<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            try
            {
                task.Wait(cancellationToken);
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }

            return task.Result;
        }

        // NOTE(cyrusn): Once we switch over to .NET Framework 4.5 we can make our SafeContinueWith overloads
        // simply call into task.ContinueWith(..., TaskContinuationOptions.LazyCancellation, ...) as
        // that will have the semantics that we want.  From the TPL guys:
        //
        //   In this situation:
#if false
        Task A = Task.Run(...);
        Task B = A.ContinueWith(..., cancellationToken);
        Task C = B.ContinueWith(...);
#endif
        // If "cancellationToken" is signaled, B completes immediately (if it has not yet started).
        // Which means that C can start before A completes, which would seem to violate the rules of
        // the dependency chain.
        //
        // We've added TaskContinuationOptions.LazyCancellation option to signify "this continuation
        // will not complete due to cancellation until its antecedent has completed".  We considered
        // simply changing the default underlying behavior, but rejected that idea because there was
        // a good chance that existing users had already drawn a dependency on the current behavior.

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            Contract.ThrowIfNull(continuationAction, nameof(continuationAction));

            bool continuationFunction(Task antecedent)
            {
                continuationAction(antecedent);
                return true;
            }

            return task.SafeContinueWith(continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task<TResult> SafeContinueWith<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            Contract.ThrowIfNull(continuationFunction, nameof(continuationFunction));

            return task.SafeContinueWith<TResult>(
                (Task antecedent) => continuationFunction((Task<TInput>)antecedent), cancellationToken, continuationOptions, scheduler);
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task SafeContinueWith<TInput>(
            this Task<TInput> task,
            Action<Task<TInput>> continuationAction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            Contract.ThrowIfNull(continuationAction, nameof(continuationAction));

            return task.SafeContinueWith(
                (Task antecedent) => continuationAction((Task<TInput>)antecedent), cancellationToken, continuationOptions, scheduler);
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task<TResult> SafeContinueWith<TResult>(
            this Task task,
            Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            // So here's the deal.  Say you do the following:
#if false
            // CancellationToken ct1 = ..., ct2 = ...;

            // Task A = Task.Factory.StartNew(..., ct1);
            // Task B = A.ContinueWith(..., ct1);
            // Task C = B.ContinueWith(..., ct2);
#endif
            // If ct1 is cancelled then the following may occur: 
            // 1) Task A can still be running (as it hasn't responded to the cancellation request
            //    yet).
            // 2) Task C can start running.  How?  Well if B hasn't started running, it may
            //    immediately transition to the 'Cancelled/Completed' state.  Moving to that state will
            //    immediately trigger C to run.
            //
            // We do not want this, so we pass the LazyCancellation flag to the TPL which implements
            // the behavior we want.

            Contract.ThrowIfNull(continuationFunction, nameof(continuationFunction));

            TResult outerFunction(Task t)
            {
                try
                {
                    return continuationFunction(t);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            // This is the only place in the code where we're allowed to call ContinueWith.
            return task.ContinueWith(outerFunction, cancellationToken, continuationOptions | TaskContinuationOptions.LazyCancellation, scheduler);
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            CancellationToken cancellationToken,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.None, scheduler);
        }

        public static Task<TResult> SafeContinueWithFromAsync<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, Task<TResult>> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            Contract.ThrowIfNull(continuationFunction, nameof(continuationFunction));

            return task.SafeContinueWithFromAsync<TResult>(
                (Task antecedent) => continuationFunction((Task<TInput>)antecedent), cancellationToken, continuationOptions, scheduler);
        }

        public static Task<TResult> SafeContinueWithFromAsync<TResult>(
            this Task task,
            Func<Task, Task<TResult>> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            // So here's the deal.  Say you do the following:
#if false
            // CancellationToken ct1 = ..., ct2 = ...;

            // Task A = Task.Factory.StartNew(..., ct1);
            // Task B = A.ContinueWith(..., ct1);
            // Task C = B.ContinueWith(..., ct2);
#endif
            // If ct1 is cancelled then the following may occur: 
            // 1) Task A can still be running (as it hasn't responded to the cancellation request
            //    yet).
            // 2) Task C can start running.  How?  Well if B hasn't started running, it may
            //    immediately transition to the 'Cancelled/Completed' state.  Moving to that state will
            //    immediately trigger C to run.
            //
            // We do not want this, so we pass the LazyCancellation flag to the TPL which implements
            // the behavior we want.
            // This is the only place in the code where we're allowed to call ContinueWith.
            var nextTask = task.ContinueWith(continuationFunction, cancellationToken, continuationOptions | TaskContinuationOptions.LazyCancellation, scheduler).Unwrap();

            nextTask.ContinueWith(ReportNonFatalError, continuationFunction,
               CancellationToken.None,
               TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
               TaskScheduler.Default);

            return nextTask;
        }

        public static Task SafeContinueWithFromAsync(
           this Task task,
           Func<Task, Task> continuationFunction,
           CancellationToken cancellationToken,
           TaskScheduler scheduler)
        {
            return task.SafeContinueWithFromAsync(continuationFunction, cancellationToken, TaskContinuationOptions.None, scheduler);
        }

        public static Task SafeContinueWithFromAsync(
            this Task task,
            Func<Task, Task> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            // So here's the deal.  Say you do the following:
#if false
            // CancellationToken ct1 = ..., ct2 = ...;

            // Task A = Task.Factory.StartNew(..., ct1);
            // Task B = A.ContinueWith(..., ct1);
            // Task C = B.ContinueWith(..., ct2);
#endif
            // If ct1 is cancelled then the following may occur: 
            // 1) Task A can still be running (as it hasn't responded to the cancellation request
            //    yet).
            // 2) Task C can start running.  How?  Well if B hasn't started running, it may
            //    immediately transition to the 'Cancelled/Completed' state.  Moving to that state will
            //    immediately trigger C to run.
            //
            // We do not want this, so we pass the LazyCancellation flag to the TPL which implements
            // the behavior we want.
            // This is the only place in the code where we're allowed to call ContinueWith.
            var nextTask = task.ContinueWith(continuationFunction, cancellationToken, continuationOptions | TaskContinuationOptions.LazyCancellation, scheduler).Unwrap();
            ReportNonFatalError(nextTask, continuationFunction);
            return nextTask;
        }

        public static Task SafeContinueWithFromAsync<TInput>(
            this Task<TInput> task,
            Func<Task<TInput>, Task> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            // So here's the deal.  Say you do the following:
#if false
            // CancellationToken ct1 = ..., ct2 = ...;

            // Task A = Task.Factory.StartNew(..., ct1);
            // Task B = A.ContinueWith(..., ct1);
            // Task C = B.ContinueWith(..., ct2);
#endif
            // If ct1 is cancelled then the following may occur: 
            // 1) Task A can still be running (as it hasn't responded to the cancellation request
            //    yet).
            // 2) Task C can start running.  How?  Well if B hasn't started running, it may
            //    immediately transition to the 'Cancelled/Completed' state.  Moving to that state will
            //    immediately trigger C to run.
            //
            // We do not want this, so we pass the LazyCancellation flag to the TPL which implements
            // the behavior we want.
            // This is the only place in the code where we're allowed to call ContinueWith.
            var nextTask = task.ContinueWith(continuationFunction, cancellationToken, continuationOptions | TaskContinuationOptions.LazyCancellation, scheduler).Unwrap();
            ReportNonFatalError(nextTask, continuationFunction);
            return nextTask;
        }

        public static Task ContinueWithAfterDelayFromAsync(
            this Task task,
            Func<Task, Task> continuationFunction,
            CancellationToken cancellationToken,
            TimeSpan delay,
            IExpeditableDelaySource delaySource,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            Contract.ThrowIfNull(continuationFunction, nameof(continuationFunction));

            return task.SafeContinueWith(t =>
                delaySource.Delay(delay, cancellationToken).SafeContinueWithFromAsync(
                    _ => continuationFunction(t), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        internal static void ReportNonFatalError(Task task, object? continuationFunction)
        {
            task.ContinueWith(ReportNonFatalErrorWorker, continuationFunction,
               CancellationToken.None,
               TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
               TaskScheduler.Default);
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void ReportNonFatalErrorWorker(Task task, object? continuationFunction)
        {
            var exception = task.Exception!;
            var methodInfo = ((Delegate)continuationFunction!).GetMethodInfo();
            exception.Data["ContinuationFunction"] = (methodInfo?.DeclaringType?.FullName ?? "?") + "::" + (methodInfo?.Name ?? "?");

            // In case of a crash with ExecutionEngineException w/o call stack it might be possible to get the stack trace using WinDbg:
            // > !threads // find thread with System.ExecutionEngineException
            //   ...
            //   67   65 4760 692b5d60   1029220 Preemptive  CD9AE70C:FFFFFFFF 012ad0f8 0     MTA (Threadpool Worker) System.ExecutionEngineException 03c51108 
            //   ...
            // > ~67s     // switch to thread 67
            // > !dso     // dump stack objects
            FatalError.ReportAndCatch(exception);
        }

        public static Task ReportNonFatalErrorAsync(this Task task)
        {
            task.ContinueWith(p => FatalError.ReportAndCatchUnlessCanceled(p.Exception!),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return task;
        }

        public static Task ReportNonFatalErrorUnlessCancelledAsync(this Task task, CancellationToken cancellationToken)
        {
            task.ContinueWith(p => FatalError.ReportAndCatchUnlessCanceled(p.Exception!, cancellationToken),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return task;
        }

        /// <summary>
        /// Asserts the <see cref="Task"/> passed has already been completed.
        /// </summary>
        /// <remarks>
        /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
        /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
        /// way to assert that while silencing any compiler complaints.
        /// </remarks>
        public static void VerifyCompleted(this Task task)
        {
            Contract.ThrowIfFalse(task.IsCompleted);

            // Propagate any exceptions that may have been thrown.
            task.GetAwaiter().GetResult();
        }
    }
}
