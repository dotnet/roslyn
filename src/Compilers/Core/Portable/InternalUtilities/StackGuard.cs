// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        private static bool TryEnsureSufficientExecutionStack<TArg>(int recursionDepth, Func<TArg, bool> throwOnFailure, TArg arg)
        {
            try
            {
                EnsureSufficientExecutionStack(recursionDepth);
                return true;
            }
            catch (InsufficientExecutionStackException) when (!throwOnFailure(arg))
            {
                return false;
            }
        }

        internal static TResult Execute<TResult, TArg1, TArg2>(ref int recursionDepth, Func<TArg1, bool> throwOnFailure, Func<TArg1, TArg2, TResult> execute, TArg1 arg1, TArg2 arg2)
        {
            recursionDepth++;
            TResult result;
            if (TryEnsureSufficientExecutionStack(recursionDepth, throwOnFailure, arg1))
            {
                result = execute(arg1, arg2);
            }
            else
            {
                var task = Task.Run(() => execute(arg1, arg2));
                // Wait on the task without inlining the task on this thread.
                Task.WhenAny(task).Wait();
                // Return result, propagating any exception.
                result = task.GetAwaiter().GetResult();
            }
            recursionDepth--;
            return result;
        }
    }
}
