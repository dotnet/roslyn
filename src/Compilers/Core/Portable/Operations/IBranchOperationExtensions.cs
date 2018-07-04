// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Operations
{
    public static class IBranchOperationExtensions
    {
        /// <summary>
        /// Gets branch nearest parent loop.
        /// </summary>
        /// <param name="branchOperation">the branch operation for which a loop parent is looked up</param>
        /// <returns>the nearest parent loop operation or <c>null</c> in case not found</returns>
        public static ILoopOperation GetParentLoop(this IBranchOperation branchOperation)
        {
            if (branchOperation.BranchKind != BranchKind.Break && branchOperation.BranchKind != BranchKind.Continue)
            {
                throw new InvalidOperationException("Invalid branch kind type. Finding loop parent requires 'break' " +
                    $"or 'continue' kinds, but the current branch kind provided is '{branchOperation.Kind}'.");
            }

            return FindParentOperation<ILoopOperation>(branchOperation);
        }

        /// <summary>
        /// Gets branch nearest parent switch.
        /// </summary>
        /// <param name="branchOperation">the branch operation for which a switch parent is looked up</param>
        /// <returns>the nearest parent switch operation or <c>null</c> in case not found</returns>
        public static ISwitchOperation GetParentSwitch(this IBranchOperation branchOperation)
        {
            if (branchOperation.BranchKind != BranchKind.Break)
            {
                throw new InvalidOperationException("Invalid branch kind type. Finding switch parent requires 'break' " +
                    $" kind, but the current branch kind provided is '{branchOperation.Kind}'.");
            }

            return FindParentOperation<ISwitchOperation>(branchOperation);
        }

        private static T FindParentOperation<T>(IOperation operation) where T : class, IOperation
        {
            if (operation is ILoopOperation || operation is ISwitchOperation)
            {
                return operation as T;
            }

            if (operation.Parent == null)
            {
                return default;
            }

            return FindParentOperation<T>(operation.Parent);
        }
    }
}

