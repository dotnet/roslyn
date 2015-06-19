// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities
{
    [SuppressMessage("ApiDesign", "RS0011", Justification = "Matching TPL Signatures")]
    internal static partial class TaskExtensions
    {
        public static T WaitAndGetResult<T>(this Task<T> task, CancellationToken cancellationToken)
        {
#if false  // eventually this will go live for check-in
#if DEBUG
            if (Microsoft.CodeAnalysis.Workspace.PrimaryWorkspace != null &&  // only care if we are in a UI situation.. this keeps normal unit tests from failing                                
                Thread.CurrentThread.IsThreadPoolThread)
            {
                // This check is meant to catch improper waits on background threads when integration tests are run.
                System.Diagnostics.Debug.Fail("WaitAndGetResult called from thread pool thread.");
            }
#endif
#endif
            task.Wait(cancellationToken);
            return task.Result;
        }

        // NOTE(cyrusn): Once we switch over to .Net 4.5 we can make our SafeContinueWith overloads
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

        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            Func<Task, bool> continuationFunction = antecedent =>
            {
                continuationAction(antecedent);
                return true;
            };

            return task.SafeContinueWith(continuationFunction, cancellationToken, continuationOptions, scheduler);
        }

        public static Task<TResult> SafeContinueWith<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuationFunction,
            CancellationToken cancellationToken,
            TaskScheduler scheduler)
        {
            return SafeContinueWith<TInput, TResult>(
                task, continuationFunction, cancellationToken, TaskContinuationOptions.None, scheduler);
        }

        public static Task<TResult> SafeContinueWith<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith<TResult>(
                (Task antecedent) => continuationFunction((Task<TInput>)antecedent), cancellationToken, continuationOptions, scheduler);
        }

        public static Task SafeContinueWith<TInput>(
            this Task<TInput> task,
            Action<Task<TInput>> continuationAction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(
                (Task antecedent) => continuationAction((Task<TInput>)antecedent), cancellationToken, continuationOptions, scheduler);
        }

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

            Func<Task, TResult> outerFunction = t =>
            {
                try
                {
                    return continuationFunction(t);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            };

            // This is the only place in the code where we're allowed to call ContinueWith.
            return task.ContinueWith(outerFunction, cancellationToken, continuationOptions | TaskContinuationOptions.LazyCancellation, scheduler);
        }

        public static Task<TResult> SafeContinueWith<TResult>(
            this Task task,
            Func<Task, TResult> continuationFunction,
            CancellationToken cancellationToken,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.None, scheduler);
        }

        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }

        public static Task SafeContinueWith<TInput>(
            this Task<TInput> task,
            Action<Task<TInput>> continuationFunction,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }

        public static Task<TResult> SafeContinueWith<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuationFunction,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationFunction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }

        public static Task SafeContinueWith(
            this Task task,
            Action<Task> continuationAction,
            CancellationToken cancellationToken,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.None, scheduler);
        }

        // Code provided by Stephen Toub.
        public static Task<TResult> ContinueWithAfterDelay<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuationFunction,
            CancellationToken cancellationToken,
            int millisecondsDelay,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(t =>
                Task.Delay(millisecondsDelay, cancellationToken).SafeContinueWith(
                    _ => continuationFunction(t), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        public static Task<TNResult> ContinueWithAfterDelay<TNResult>(
            this Task task,
            Func<Task, TNResult> continuationFunction,
            CancellationToken cancellationToken,
            int millisecondsDelay,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(t =>
                Task.Delay(millisecondsDelay, cancellationToken).SafeContinueWith(
                    _ => continuationFunction(t), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        public static Task ContinueWithAfterDelay(
            this Task task,
            Action continuationAction,
            CancellationToken cancellationToken,
            int millisecondsDelay,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(t =>
                Task.Delay(millisecondsDelay, cancellationToken).SafeContinueWith(
                    _ => continuationAction(), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        public static Task<TResult> SafeContinueWithFromAsync<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, Task<TResult>> continuationFunction,
            CancellationToken cancellationToken,
            TaskContinuationOptions continuationOptions,
            TaskScheduler scheduler)
        {
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

            nextTask.ContinueWith(ReportFatalError, continuationFunction,
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
            ReportFatalError(nextTask, continuationFunction);
            return nextTask;
        }

        public static Task<TNResult> ContinueWithAfterDelayFromAsync<TNResult>(
            this Task task,
            Func<Task, Task<TNResult>> continuationFunction,
            CancellationToken cancellationToken,
            int millisecondsDelay,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(t =>
                Task.Delay(millisecondsDelay, cancellationToken).SafeContinueWithFromAsync(
                    _ => continuationFunction(t), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        public static Task ContinueWithAfterDelayFromAsync(
            this Task task,
            Func<Task, Task> continuationFunction,
            CancellationToken cancellationToken,
            int millisecondsDelay,
            TaskContinuationOptions taskContinuationOptions,
            TaskScheduler scheduler)
        {
            return task.SafeContinueWith(t =>
                Task.Delay(millisecondsDelay, cancellationToken).SafeContinueWithFromAsync(
                    _ => continuationFunction(t), cancellationToken, TaskContinuationOptions.None, scheduler),
                cancellationToken, taskContinuationOptions, scheduler).Unwrap();
        }

        internal static void ReportFatalError(Task task, object continuationFunction)
        {
            task.ContinueWith(ReportFatalErrorWorker, continuationFunction,
               CancellationToken.None,
               TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
               TaskScheduler.Default);
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void ReportFatalErrorWorker(Task task, object continuationFunction)
        {
            var exception = task.Exception;
            var methodInfo = ((Delegate)continuationFunction).GetMethodInfo();
            exception.Data["ContinuationFunction"] = methodInfo.DeclaringType.FullName + "::" + methodInfo.Name;

            // In case of a crash with ExecutionEngineException w/o call stack it might be possible to get the stack trace using WinDbg:
            // > !threads // find thread with System.ExecutionEngineException
            //   ...
            //   67   65 4760 692b5d60   1029220 Preemptive  CD9AE70C:FFFFFFFF 012ad0f8 0     MTA (Threadpool Worker) System.ExecutionEngineException 03c51108 
            //   ...
            // > ~67s     // switch to thread 67
            // > !dso     // dump stack objects
            FatalError.Report(exception);
        }
    }
}
