// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static partial class BasicBlockExtensions
    {
        public static IEnumerable<IOperation> DescendantOperations(this BasicBlock basicBlock)
        {
            foreach (var statement in basicBlock.Operations)
            {
                foreach (var operation in statement.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }

            if (basicBlock.BranchValue != null)
            {
                foreach (var operation in basicBlock.BranchValue.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }
        }
    }
}
