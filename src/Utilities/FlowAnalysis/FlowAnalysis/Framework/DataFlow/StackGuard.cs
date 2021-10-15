// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Stack guard for <see cref="DataFlowOperationVisitor{TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue}"/> to ensure sufficient stack while recursively visiting the operation tree.
    /// </summary>
    internal static class StackGuard
    {
        public const int MaxUncheckedRecursionDepth = 20;

        public static void EnsureSufficientExecutionStack(int recursionDepth)
        {
            if (recursionDepth > MaxUncheckedRecursionDepth)
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
        }

        public static bool IsInsufficientExecutionStackException(Exception ex)
        {
            return ex.GetType().Name == "InsufficientExecutionStackException";
        }
    }
}