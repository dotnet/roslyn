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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities;

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

    /// <summary>
    /// Asserts the <see cref="Task"/> passed has already been completed.
    /// </summary>
    /// <remarks>
    /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
    /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
    /// way to assert that while silencing any compiler complaints.
    /// </remarks>
    public static TResult VerifyCompleted<TResult>(this Task<TResult> task)
    {
        Contract.ThrowIfFalse(task.IsCompleted);

        // Propagate any exceptions that may have been thrown.
        return task.GetAwaiter().GetResult();
    }
}
