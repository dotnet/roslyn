// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities;

[SuppressMessage("ApiDesign", "CA1068", Justification = "Matching TPL Signatures")]
internal static partial class TaskExtensions
{
    extension<T>(Task<T> task)
    {
        public T WaitAndGetResult(CancellationToken cancellationToken)
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
        public T WaitAndGetResult_CanCallOnBackground(CancellationToken cancellationToken)
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
    }

    extension(Task task)
    {
        /// <summary>
        /// Asserts the <see cref="Task"/> passed has already been completed.
        /// </summary>
        /// <remarks>
        /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
        /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
        /// way to assert that while silencing any compiler complaints.
        /// </remarks>
        public void VerifyCompleted()
        {
            Contract.ThrowIfFalse(task.IsCompleted);

            // Propagate any exceptions that may have been thrown.
            task.GetAwaiter().GetResult();
        }
    }

    extension<TResult>(Task<TResult> task)
    {
        /// <summary>
        /// Asserts the <see cref="Task"/> passed has already been completed.
        /// </summary>
        /// <remarks>
        /// This is useful for a specific case: sometimes you might be calling an API that is "sometimes" async, and you're
        /// calling it from a synchronous method where you know it should have completed synchronously. This is an easy
        /// way to assert that while silencing any compiler complaints.
        /// </remarks>
        public TResult VerifyCompleted()
        {
            Contract.ThrowIfFalse(task.IsCompleted);

            // Propagate any exceptions that may have been thrown.
            return task.GetAwaiter().GetResult();
        }
    }
}
