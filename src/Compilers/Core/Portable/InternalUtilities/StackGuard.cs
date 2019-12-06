// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    }
}
