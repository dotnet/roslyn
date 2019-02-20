// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    public static class BasicBlockExtensions
    {
        internal static IEnumerable<(BasicBlock predecessorBlock, BranchWithInfo branchWithInfo)> GetPredecessorsWithBranches(this BasicBlock basicBlock, ControlFlowGraph cfg)
        {
            foreach (ControlFlowBranch predecessorBranch in basicBlock.Predecessors)
            {
                var branchWithInfo = new BranchWithInfo(predecessorBranch);
                if (predecessorBranch.FinallyRegions.Length > 0)
                {
                    var lastFinally = predecessorBranch.FinallyRegions[predecessorBranch.FinallyRegions.Length - 1];
                    yield return (predecessorBlock: cfg.Blocks[lastFinally.LastBlockOrdinal], branchWithInfo);
                }
                else
                {
                    yield return (predecessorBlock: predecessorBranch.Source, branchWithInfo);
                }
            }
        }

        internal static ITypeSymbol GetEnclosingRegionExceptionType(this BasicBlock basicBlock)
        {
            var region = basicBlock.EnclosingRegion;
            while (region != null)
            {
                if (region.ExceptionType != null)
                {
                    return region.ExceptionType;
                }

                region = region.EnclosingRegion;
            }

            return null;
        }

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
