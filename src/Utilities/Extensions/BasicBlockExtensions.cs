// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Operations.DataFlow;

namespace Analyzer.Utilities.Extensions
{
    internal static class BasicBlockExtensions
    {
        public static IEnumerable<(BasicBlock predecessorBlock, BranchWithInfo branch)> GetPredecessorsWithBranches(this BasicBlock basicBlock, Dictionary<int, BasicBlock> ordinalToBlockMap)
        {
            foreach (var predecessor in basicBlock.Predecessors)
            {
                var fromBlockWithAdjustedFinally = predecessor;
                var predecessorFinallyRegions = predecessor.Next.Branch.FinallyRegions;
                if (predecessorFinallyRegions.Length > 0)
                {
                    var lastFinally = predecessorFinallyRegions[predecessorFinallyRegions.Length - 1];
                    yield return (predecessorBlock: ordinalToBlockMap[lastFinally.LastBlockOrdinal], branch: predecessor.GetNextBranchWithInfo());
                }
                else
                {
                    if (predecessor.Next.Branch.Destination == basicBlock)
                    {
                        yield return (predecessorBlock: predecessor, branch: predecessor.GetNextBranchWithInfo());
                    }

                    if (predecessor.Conditional.Branch.Destination == basicBlock)
                    {
                        yield return (predecessorBlock: predecessor, branch: predecessor.GetConditionalBranchWithInfo());
                    }
                }
            }
        }

        public static ITypeSymbol GetEnclosingRegionExceptionType(this BasicBlock basicBlock)
        {
            var region = basicBlock.Region;
            while (region != null)
            {
                if (region.ExceptionType != null)
                {
                    return region.ExceptionType;
                }

                region = region.Enclosing;
            }

            return null;
        }

        public static BranchWithInfo GetNextBranchWithInfo(this BasicBlock basicBlock)
            => new BranchWithInfo(basicBlock.Next.Branch,
                conditionOpt: basicBlock.Conditional.Condition,
                valueOpt: basicBlock.Next.Value,
                jumpIfTrue: basicBlock.Conditional.Condition != null ? !basicBlock.Conditional.JumpIfTrue : (bool?)null);

        public static BranchWithInfo GetConditionalBranchWithInfo(this BasicBlock basicBlock)
            => new BranchWithInfo(basicBlock.Conditional.Branch,
                conditionOpt: basicBlock.Conditional.Condition,
                valueOpt: null,
                jumpIfTrue: basicBlock.Conditional.JumpIfTrue);

        public static IEnumerable<IOperation> DescendantOperations(this BasicBlock basicBlock)
        {
            foreach (var statement in basicBlock.Statements)
            {
                foreach (var operation in statement.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }

            if (basicBlock.Conditional.Condition != null)
            {
                foreach (var operation in basicBlock.Conditional.Condition.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }

            if (basicBlock.Next.Value != null)
            {
                foreach (var operation in basicBlock.Next.Value.DescendantsAndSelf())
                {
                    yield return operation;
                }
            }
        }
    }
}
