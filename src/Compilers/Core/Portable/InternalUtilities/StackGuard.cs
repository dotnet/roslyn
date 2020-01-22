// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Roslyn.Utilities;
#if !COMPILERCORE
using Microsoft.CodeAnalysis.ErrorReporting;
#endif

namespace Microsoft.CodeAnalysis
{
    internal static class StackGuard
    {
        public const int MaxUncheckedRecursionDepth = 20;

        /// <summary>
        ///     Ensures that the remaining stack space is large enough to execute
        ///     the average function.
        /// </summary>
        /// <param name="recursionDepth">how many times the calling function has recursed</param>
        /// <exception cref="InsufficientExecutionStackException">
        ///     The available stack space is insufficient to execute
        ///     the average function.
        /// </exception>
        [DebuggerStepThrough]
        public static void EnsureSufficientExecutionStack(int recursionDepth)
        {
            if (recursionDepth > MaxUncheckedRecursionDepth)
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
        }

        internal static bool TryEnsureSufficientExecutionStack(int recursionDepth, bool throwOnFailure)
        {
            try
            {
                EnsureSufficientExecutionStack(recursionDepth);
                return true;
            }
            catch (InsufficientExecutionStackException) when (!throwOnFailure)
            {
                return false;
            }
        }

        internal static TResult ExecuteOnNewExecutionStack<TResult>(Func<TResult> execute)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    return execute();
                }
                catch (Exception e) when (FatalError.Report(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            });
            // Wait on the task without inlining the task on this thread.
            Task.WhenAny(task).Wait();
            // Return result, propagating any exception.
            return task.GetAwaiter().GetResult();
        }
    }
}
