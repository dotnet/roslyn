// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Operations
{
    public static class IBranchOperationExtensions
    {
        /// <summary>
        /// Gets a loop operation that corresponds to the given branch operation.
        /// </summary>
        /// <param name="branchOperation">the branch operation for which a corresponding loop is looked up</param>
        /// <returns>the corresponding loop operation or <c>null</c> in case not found (e.g. no loop syntax or the branch
        /// belongs switch instead of loop operation)</returns>
        public static ILoopOperation GetCorrespondingLoop(this IBranchOperation branchOperation)
        {
            if (branchOperation.BranchKind != BranchKind.Break && branchOperation.BranchKind != BranchKind.Continue)
            {
                throw new InvalidOperationException("Invalid branch kind type. Finding a corresponding loop requires " +
                    "'break' or 'continue' kinds, but the current branch kind provided is '{branchOperation.Kind}'.");
            }

            return FindCorrespondingOperation<ILoopOperation>(branchOperation);
        }

        /// <summary>
        /// Gets a switch operation that corresponds to the given branch operation.
        /// </summary>
        /// <param name="branchOperation">the branch operation for which a corresponding switch is looked up</param>
        /// <returns>the corresponding switch operation or <c>null</c> in case not found (e.g. no switch syntax or the branch
        /// belongs loop instead of switch operation)</returns>
        public static ISwitchOperation GetCorrespondingSwitch(this IBranchOperation branchOperation)
        {
            if (branchOperation.BranchKind != BranchKind.Break)
            {
                throw new InvalidOperationException("Invalid branch kind type. Finding a corresponding switch requires " +
                    "'break' kind, but the current branch kind provided is '{branchOperation.Kind}'.");
            }

            return FindCorrespondingOperation<ISwitchOperation>(branchOperation);
        }

        private static T FindCorrespondingOperation<T>(IOperation operation) where T : class, IOperation
        {
            if (operation is ILoopOperation || operation is ISwitchOperation)
            {
                return operation as T;
            }

            if (operation.Parent == null)
            {
                return default;
            }

            return FindCorrespondingOperation<T>(operation.Parent);
        }
    }
}

