// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static partial class BasicBlockExtensions
    {
        internal static IEnumerable<(BasicBlock predecessorBlock, BranchWithInfo branchWithInfo)> GetPredecessorsWithBranches(this BasicBlock basicBlock, ControlFlowGraph cfg)
        {
            foreach (ControlFlowBranch predecessorBranch in basicBlock.Predecessors)
            {
                var branchWithInfo = new BranchWithInfo(predecessorBranch);
                if (!predecessorBranch.FinallyRegions.IsEmpty)
                {
                    var lastFinally = predecessorBranch.FinallyRegions[^1];
                    yield return (predecessorBlock: cfg.Blocks[lastFinally.LastBlockOrdinal], branchWithInfo);
                }
                else
                {
                    yield return (predecessorBlock: predecessorBranch.Source, branchWithInfo);
                }
            }
        }

        internal static ITypeSymbol? GetEnclosingRegionExceptionType(this BasicBlock basicBlock)
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

        /// <summary>
        /// Returns true if the given <paramref name="basicBlock"/> is contained in a control flow region with the given <paramref name="regionKind"/>.
        /// </summary>
        public static bool IsContainedInRegionOfKind(this BasicBlock basicBlock, ControlFlowRegionKind regionKind)
            => basicBlock.GetContainingRegionOfKind(regionKind) != null;

        /// <summary>
        /// Returns the innermost control flow region of the given <paramref name="regionKind"/> that contains the given <paramref name="basicBlock"/>.
        /// </summary>
        public static ControlFlowRegion? GetContainingRegionOfKind(this BasicBlock basicBlock, ControlFlowRegionKind regionKind)
        {
            var enclosingRegion = basicBlock.EnclosingRegion;
            while (enclosingRegion != null)
            {
                if (enclosingRegion.Kind == regionKind)
                {
                    return enclosingRegion;
                }

                enclosingRegion = enclosingRegion.EnclosingRegion;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given basic block is the first block of a finally region.
        /// </summary>
        public static bool IsFirstBlockOfFinally(this BasicBlock basicBlock, [NotNullWhen(returnValue: true)] out ControlFlowRegion? finallyRegion)
            => basicBlock.IsFirstBlockOfRegionKind(ControlFlowRegionKind.Finally, out finallyRegion);

        /// <summary>
        /// Returns true if the given basic block is the last block of a finally region.
        /// </summary>
        public static bool IsLastBlockOfFinally(this BasicBlock basicBlock, [NotNullWhen(returnValue: true)] out ControlFlowRegion? finallyRegion)
            => basicBlock.IsLastBlockOfRegionKind(ControlFlowRegionKind.Finally, out finallyRegion);

        /// <summary>
        /// Returns true if the given basic block is the first block of a region of the given regionKind.
        /// </summary>
        public static bool IsFirstBlockOfRegionKind(this BasicBlock basicBlock, ControlFlowRegionKind regionKind, [NotNullWhen(returnValue: true)] out ControlFlowRegion? region)
            => basicBlock.IsFirstOrLastBlockOfRegionKind(regionKind, first: true, out region);

        /// <summary>
        /// Returns true if the given basic block is the last block of a region of the given regionKind.
        /// </summary>
        public static bool IsLastBlockOfRegionKind(this BasicBlock basicBlock, ControlFlowRegionKind regionKind, [NotNullWhen(returnValue: true)] out ControlFlowRegion? region)
            => basicBlock.IsFirstOrLastBlockOfRegionKind(regionKind, first: false, out region);

        private static bool IsFirstOrLastBlockOfRegionKind(this BasicBlock basicBlock, ControlFlowRegionKind regionKind, bool first, [NotNullWhen(returnValue: true)] out ControlFlowRegion? foundRegion)
        {
            foundRegion = null;

            var enclosingRegion = basicBlock.EnclosingRegion;
            while (enclosingRegion != null)
            {
                var ordinalToCompare = first ? enclosingRegion.FirstBlockOrdinal : enclosingRegion.LastBlockOrdinal;
                if (ordinalToCompare != basicBlock.Ordinal)
                {
                    return false;
                }

                if (enclosingRegion.Kind == regionKind)
                {
                    foundRegion = enclosingRegion;
                    return true;
                }

                enclosingRegion = enclosingRegion.EnclosingRegion;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given basic block is the first block of a compiler generated finally region.
        /// </summary>
        public static bool IsFirstBlockOfCompilerGeneratedFinally(this BasicBlock basicBlock, ControlFlowGraph cfg)
        {
            if (!basicBlock.IsFirstBlockOfRegionKind(ControlFlowRegionKind.Finally, out var finallyRegion))
            {
                return false;
            }

            foreach (var operation in finallyRegion.DescendantOperations(cfg))
            {
                if (!operation.IsImplicit)
                {
                    return false;
                }
            }

            return true;
        }

        internal static ControlFlowRegion? GetInnermostRegionStartedByBlock(this BasicBlock basicBlock, ControlFlowRegionKind regionKind)
        {
            if (basicBlock.EnclosingRegion?.FirstBlockOrdinal != basicBlock.Ordinal)
            {
                return null;
            }

            var enclosingRegion = basicBlock.EnclosingRegion;
            while (enclosingRegion.Kind != regionKind)
            {
                enclosingRegion = enclosingRegion.EnclosingRegion;
                if (enclosingRegion?.FirstBlockOrdinal != basicBlock.Ordinal)
                {
                    return null;
                }
            }

            return enclosingRegion;
        }

        /// <summary>
        /// Gets the maximum ordinal of a conditional or fall through successor of the given basic block.
        /// Returns -1 if the block has no conditional or fall through successor,
        /// for example, if the block only has a structured exception handling branch for throw operation.
        /// </summary>
        /// <param name="basicBlock"></param>
        /// <returns></returns>
        internal static int GetMaxSuccessorOrdinal(this BasicBlock basicBlock)
            => Math.Max(basicBlock.FallThroughSuccessor?.Destination?.Ordinal ?? -1,
                        basicBlock.ConditionalSuccessor?.Destination?.Ordinal ?? -1);

        internal static bool DominatesPredecessors(this BasicBlock? basicBlock, ControlFlowGraph cfg)
        {
            if (basicBlock == null ||
                basicBlock.Predecessors.IsEmpty)
            {
                return false;
            }

            using var _1 = PooledHashSet<int>.GetInstance(out var processedOrdinals);
            using var _2 = ArrayBuilder<int>.GetInstance(out var unprocessedOrdinals);
            foreach (var predecessor in basicBlock.Predecessors)
            {
                var sourceBlock = predecessor.Source;
                if (!DominatesBlock(sourceBlock, basicBlock, processedOrdinals, unprocessedOrdinals))
                {
                    return false;
                }

                processedOrdinals.Add(sourceBlock.Ordinal);
            }

            while (unprocessedOrdinals.Count > 0)
            {
                var ordinal = unprocessedOrdinals[0];
                Debug.Assert(ordinal < basicBlock.Ordinal);
                unprocessedOrdinals.RemoveAt(0);

                if (processedOrdinals.Add(ordinal))
                {
                    var sourceBlock = cfg.Blocks[ordinal];
                    if (!DominatesBlock(sourceBlock, basicBlock, processedOrdinals, unprocessedOrdinals))
                    {
                        return false;
                    }
                }
            }

            return true;

            static bool DominatesBlock(BasicBlock sourceBlock, BasicBlock basicBlock, PooledHashSet<int> processedOrdinals, ArrayBuilder<int> unprocessedOrdinals)
                => DominatesBranch(sourceBlock.ConditionalSuccessor, basicBlock, processedOrdinals, unprocessedOrdinals) &&
                   DominatesBranch(sourceBlock.FallThroughSuccessor, basicBlock, processedOrdinals, unprocessedOrdinals);

            static bool DominatesBranch(ControlFlowBranch? branch, BasicBlock basicBlock, PooledHashSet<int> processedOrdinals, ArrayBuilder<int> unprocessedOrdinals)
            {
                var destinationBlock = branch?.Destination;
                if (destinationBlock == null ||
                    destinationBlock == basicBlock)
                {
                    return true;
                }

                if (!processedOrdinals.Contains(destinationBlock.Ordinal))
                {
                    unprocessedOrdinals.Add(destinationBlock.Ordinal);
                }

                return destinationBlock.Ordinal <= basicBlock.Ordinal;
            }
        }
    }
}
