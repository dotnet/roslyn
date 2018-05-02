// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class ControlFlowGraphBuilder : OperationVisitor<int?, IOperation>
    {
        // PROTOTYPE(dataflow): does it have to be a field?
        private readonly BasicBlock _entry = new BasicBlock(BasicBlockKind.Entry);

        // PROTOTYPE(dataflow): does it have to be a field?
        private readonly BasicBlock _exit = new BasicBlock(BasicBlockKind.Exit);

        private ArrayBuilder<BasicBlock> _blocks;
        private PooledDictionary<BasicBlock, RegionBuilder> _regionMap;
        private BasicBlock _currentBasicBlock;
        private RegionBuilder _currentRegion;
        private PooledDictionary<ILabelSymbol, BasicBlock> _labeledBlocks;

        private IOperation _currentStatement;
        private ArrayBuilder<IOperation> _evalStack;
        private IOperation _currentConditionalAccessInstance;
        private IOperation _currentSwitchOperationExpression;

        // PROTOTYPE(dataflow): does the public API IFlowCaptureOperation.Id specify how identifiers are created or assigned?
        // Should we use uint to exclude negative integers? Should we randomize them in any way to avoid dependencies 
        // being taken?
        private int _availableCaptureId = 0;

        private ControlFlowGraphBuilder()
        { }

        public static ControlFlowGraph Create(IBlockOperation body)
        {
            // PROTOTYPE(dataflow): Consider getting the SemanticModel and Compilation from the root node, 
            //                      storing them in readonly fields in ControlFlowGraphBuilder, and reusing
            //                      throughout the process rather than getting them from individual nodes.

            var builder = new ControlFlowGraphBuilder();
            var blocks = ArrayBuilder<BasicBlock>.GetInstance();
            builder._blocks = blocks;
            builder._evalStack = ArrayBuilder<IOperation>.GetInstance();
            builder._regionMap = PooledDictionary<BasicBlock, RegionBuilder>.GetInstance();

            var root = new RegionBuilder(ControlFlowGraph.RegionKind.Root);
            builder.EnterRegion(root);
            builder.AppendNewBlock(builder._entry, linkToPrevious: false);
            builder._currentBasicBlock = null;
            builder.VisitStatement(body);
            builder.AppendNewBlock(builder._exit);
            builder.LeaveRegion();
            Debug.Assert(builder._currentRegion == null);

            CheckUnresolvedBranches(blocks, builder._labeledBlocks);
            Pack(blocks, root, builder._regionMap);
            ControlFlowGraph.Region region = root.ToImmutableRegionAndFree(blocks);
            root = null;
            CalculateBranchLeaveEnterLists(blocks);
            MarkReachableBlocks(blocks);

            Debug.Assert(builder._evalStack.Count == 0);
            builder._evalStack.Free();
            builder._regionMap.Free();
            builder._labeledBlocks?.Free();

            return new ControlFlowGraph(blocks.ToImmutableAndFree(), region);
        }

        private static void MarkReachableBlocks(ArrayBuilder<BasicBlock> blocks)
        {
            var continueDispatchAfterFinally = PooledDictionary<ControlFlowGraph.Region, bool>.GetInstance();
            var dispatchedExceptionsFromRegions = PooledHashSet<ControlFlowGraph.Region>.GetInstance();
            MarkReachableBlocks(blocks, firstBlockOrdinal: 0, lastBlockOrdinal: blocks.Count - 1,
                                outOfRangeBlocksToVisit: null,
                                continueDispatchAfterFinally,
                                dispatchedExceptionsFromRegions,
                                out _);
            continueDispatchAfterFinally.Free();
            dispatchedExceptionsFromRegions.Free();
        }

        private static BitVector MarkReachableBlocks(
            ArrayBuilder<BasicBlock> blocks,
            int firstBlockOrdinal,
            int lastBlockOrdinal,
            ArrayBuilder<BasicBlock> outOfRangeBlocksToVisit,
            PooledDictionary<ControlFlowGraph.Region, bool> continueDispatchAfterFinally,
            PooledHashSet<ControlFlowGraph.Region> dispatchedExceptionsFromRegions,
            out bool fellThrough)
        {
            var visited = BitVector.Empty;
            var toVisit = ArrayBuilder<BasicBlock>.GetInstance();

            fellThrough = false;
            toVisit.Push(blocks[firstBlockOrdinal]);

            do
            {
                BasicBlock current = toVisit.Pop();

                if (current.Ordinal < firstBlockOrdinal || current.Ordinal > lastBlockOrdinal)
                {
                    outOfRangeBlocksToVisit.Push(current);
                    continue;
                }

                if (visited[current.Ordinal])
                {
                    continue;
                }

                visited[current.Ordinal] = true;
                current.IsReachable = true;
                bool fallThrough = true;

                (IOperation Condition, bool JumpIfTrue, BasicBlock.Branch Branch) conditional = current.Conditional;
                if (conditional.Condition != null)
                {
                    if (conditional.Condition.ConstantValue.HasValue && conditional.Condition.ConstantValue.Value is bool constant)
                    {
                        if (constant == conditional.JumpIfTrue)
                        {
                            followBranch(current, conditional.Branch);
                            fallThrough = false;
                        }
                    }
                    else
                    {
                        followBranch(current, conditional.Branch);
                    }
                }

                if (fallThrough)
                {
                    BasicBlock.Branch branch = current.Next.Branch;
                    followBranch(current, branch);

                    if (current.Ordinal == lastBlockOrdinal && branch.Kind != BasicBlock.BranchKind.Throw && branch.Kind != BasicBlock.BranchKind.ReThrow)
                    {
                        fellThrough = true;
                    }
                }

                // We are using very simple approach: 
                // If try block is reachable, we should dispatch an exception from it, even if it is empty.
                // To simplify implementation, we dispatch exception from every reachable basic block and rely
                // on dispatchedExceptionsFromRegions cache to avoid doing duplicate work.
                dispatchException(current.Region);
            }
            while (toVisit.Count != 0);

            toVisit.Free();
            return visited;

            void followBranch(BasicBlock current, BasicBlock.Branch branch)
            {
                switch (branch.Kind)
                {
                    case BasicBlock.BranchKind.None:
                    case BasicBlock.BranchKind.ProgramTermination:
                    case BasicBlock.BranchKind.StructuredExceptionHandling:
                    case BasicBlock.BranchKind.Throw:
                    case BasicBlock.BranchKind.ReThrow:
                        Debug.Assert(branch.Destination == null);
                        return;

                    case BasicBlock.BranchKind.Regular:
                    case BasicBlock.BranchKind.Return:
                        Debug.Assert(branch.Destination != null);

                        if (stepThroughFinally(current.Region, branch.Destination))
                        {
                            toVisit.Add(branch.Destination);
                        }

                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(branch.Kind);
                }
            }

            // Returns whether we should proceed to the destination after finallies were taken care of.
            bool stepThroughFinally(ControlFlowGraph.Region region, BasicBlock destination)
            {
                int destinationOrdinal = destination.Ordinal;
                while (!region.ContainsBlock(destinationOrdinal))
                {
                    ControlFlowGraph.Region enclosing = region.Enclosing;
                    if (region.Kind == ControlFlowGraph.RegionKind.Try && enclosing.Kind == ControlFlowGraph.RegionKind.TryAndFinally)
                    {
                        Debug.Assert(enclosing.Regions[0] == region);
                        Debug.Assert(enclosing.Regions[1].Kind == ControlFlowGraph.RegionKind.Finally);
                        if (!stepThroughSingleFinally(enclosing.Regions[1]))
                        {
                            // The point that continues dispatch is not reachable. Cancel the dispatch.
                            return false;
                        }
                    }

                    region = enclosing;
                }

                return true;
            }

            // Returns whether we should proceed with dispatch after finally was taken care of.
            bool stepThroughSingleFinally(ControlFlowGraph.Region @finally)
            {
                Debug.Assert(@finally.Kind == ControlFlowGraph.RegionKind.Finally);

                if (!continueDispatchAfterFinally.TryGetValue(@finally, out bool continueDispatch))
                {
                    // For simplicity, we do a complete walk of the finally/filter region in isolation
                    // to make sure that the resume dispatch point is reachable from its beginning.
                    // It could also be reachable through invalid branches into the finally and we don't want to consider 
                    // these cases for regular finally handling.
                    BitVector isolated = MarkReachableBlocks(blocks,
                                                             @finally.FirstBlockOrdinal,
                                                             @finally.LastBlockOrdinal,
                                                             outOfRangeBlocksToVisit: toVisit,
                                                             continueDispatchAfterFinally,
                                                             dispatchedExceptionsFromRegions,
                                                             out bool isolatedFellThrough);
                    visited.UnionWith(isolated);

                    continueDispatch = isolatedFellThrough &&
                                       blocks[@finally.LastBlockOrdinal].Next.Branch.Kind == BasicBlock.BranchKind.StructuredExceptionHandling;

                    continueDispatchAfterFinally.Add(@finally, continueDispatch);
                }

                return continueDispatch;
            }

            void dispatchException(ControlFlowGraph.Region fromRegion)
            {
                do
                {
                    if (!dispatchedExceptionsFromRegions.Add(fromRegion))
                    {
                        return;
                    }

                    ControlFlowGraph.Region enclosing = fromRegion.Enclosing;
                    if (fromRegion.Kind == ControlFlowGraph.RegionKind.Try)
                    {
                        switch (enclosing.Kind)
                        {
                            case ControlFlowGraph.RegionKind.TryAndFinally:
                                Debug.Assert(enclosing.Regions[0] == fromRegion);
                                Debug.Assert(enclosing.Regions[1].Kind == ControlFlowGraph.RegionKind.Finally);
                                if (!stepThroughSingleFinally(enclosing.Regions[1]))
                                {
                                    // The point that continues dispatch is not reachable. Cancel the dispatch.
                                    return;
                                }
                                break;

                            case ControlFlowGraph.RegionKind.TryAndCatch:
                                Debug.Assert(enclosing.Regions[0] == fromRegion);
                                dispatchExceptionThroughCatches(enclosing, startAt: 1);
                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(enclosing.Kind);
                        }
                    }
                    else if (fromRegion.Kind == ControlFlowGraph.RegionKind.Filter)
                    {
                        // If filter throws, dispatch is resumed at the next catch with an original exception
                        Debug.Assert(enclosing.Kind == ControlFlowGraph.RegionKind.FilterAndHandler);
                        ControlFlowGraph.Region tryAndCatch = enclosing.Enclosing;
                        Debug.Assert(tryAndCatch.Kind == ControlFlowGraph.RegionKind.TryAndCatch);

                        int index = tryAndCatch.Regions.IndexOf(enclosing, startIndex: 1);

                        if (index > 0)
                        {
                            dispatchExceptionThroughCatches(tryAndCatch, startAt: index + 1);
                            fromRegion = tryAndCatch;
                            continue;
                        }

                        throw ExceptionUtilities.Unreachable;
                    }

                    fromRegion = enclosing;
                }
                while (fromRegion != null);
            }

            void dispatchExceptionThroughCatches(ControlFlowGraph.Region tryAndCatch, int startAt)
            {
                // For simplicity, we do not try to figure out whether a catch clause definitely
                // handles all exceptions.

                Debug.Assert(tryAndCatch.Kind == ControlFlowGraph.RegionKind.TryAndCatch);
                Debug.Assert(startAt > 0);
                Debug.Assert(startAt <= tryAndCatch.Regions.Length);

                for (int i = startAt; i < tryAndCatch.Regions.Length; i++)
                {
                    ControlFlowGraph.Region @catch = tryAndCatch.Regions[i];

                    switch (@catch.Kind)
                    {
                        case ControlFlowGraph.RegionKind.Catch:
                            toVisit.Add(blocks[@catch.FirstBlockOrdinal]);
                            break;

                        case ControlFlowGraph.RegionKind.FilterAndHandler:
                            BasicBlock entryBlock = blocks[@catch.FirstBlockOrdinal];
                            Debug.Assert(@catch.Regions[0].Kind == ControlFlowGraph.RegionKind.Filter);
                            Debug.Assert(entryBlock.Ordinal == @catch.Regions[0].FirstBlockOrdinal);

                            toVisit.Add(entryBlock);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(@catch.Kind);
                    }
                }
            }
        }

        private static void CalculateBranchLeaveEnterLists(ArrayBuilder<BasicBlock> blocks)
        {
            var builder = ArrayBuilder<ControlFlowGraph.Region>.GetInstance();

            foreach (BasicBlock b in blocks)
            {
                calculateBranchLeaveEnterLists(ref b.InternalConditional.Branch, b);
                calculateBranchLeaveEnterLists(ref b.InternalNext.Branch, b);
            }

            builder.Free();
            return;

            void calculateBranchLeaveEnterLists(ref BasicBlock.Branch branch, BasicBlock source)
            {
                if (branch.Destination == null)
                {
                    branch.LeavingRegions = ImmutableArray<ControlFlowGraph.Region>.Empty;
                    branch.EnteringRegions = ImmutableArray<ControlFlowGraph.Region>.Empty;
                }
                else
                {
                    branch.LeavingRegions = calculateLeaveList(source, branch.Destination);
                    branch.EnteringRegions = calculateEnterList(source, branch.Destination);
                }
            }

            ImmutableArray<ControlFlowGraph.Region> calculateLeaveList(BasicBlock source, BasicBlock destination)
            {
                collectRegions(destination.Ordinal, source.Region);
                return builder.ToImmutable();
            }

            ImmutableArray<ControlFlowGraph.Region> calculateEnterList(BasicBlock source, BasicBlock destination)
            {
                collectRegions(source.Ordinal, destination.Region);
                builder.ReverseContents();
                return builder.ToImmutable();
            }

            void collectRegions(int destinationOrdinal, ControlFlowGraph.Region source)
            {
                builder.Clear();

                while (!source.ContainsBlock(destinationOrdinal))
                {
                    builder.Add(source);
                    source = source.Enclosing;
                }
            }
        }

        /// <summary>
        /// Do a pass to eliminate blocks without statements that can be merged with predecessor(s) and
        /// to eliminate regions that can be merged with parents.
        /// </summary>
        private static void Pack(ArrayBuilder<BasicBlock> blocks, RegionBuilder root, PooledDictionary<BasicBlock, RegionBuilder> regionMap)
        {
            bool regionsChanged = true;

            while (true)
            {
                regionsChanged |= PackRegions(root, blocks, regionMap);

                if (!regionsChanged || !PackBlocks(blocks, regionMap))
                {
                    break;
                }

                regionsChanged = false;
            }
        }

        private static bool PackRegions(RegionBuilder root, ArrayBuilder<BasicBlock> blocks, PooledDictionary<BasicBlock, RegionBuilder> regionMap)
        {
            return PackRegion(root);

            bool PackRegion(RegionBuilder region)
            {
                Debug.Assert(!region.IsEmpty);
                bool result = false;

                if (region.HasRegions)
                {
                    foreach (RegionBuilder r in region.Regions)
                    {
                        if (PackRegion(r))
                        {
                            result = true;
                        }
                    }
                }

                switch (region.Kind)
                {
                    case ControlFlowGraph.RegionKind.Root:
                    case ControlFlowGraph.RegionKind.Filter:
                    case ControlFlowGraph.RegionKind.Try:
                    case ControlFlowGraph.RegionKind.Catch:
                    case ControlFlowGraph.RegionKind.Finally:
                    case ControlFlowGraph.RegionKind.Locals:
                    case ControlFlowGraph.RegionKind.StaticLocalInitializer:

                        if (region.Regions?.Count == 1)
                        {
                            RegionBuilder subRegion = region.Regions[0];
                            if (subRegion.Kind == ControlFlowGraph.RegionKind.Locals && subRegion.FirstBlock == region.FirstBlock && subRegion.LastBlock == region.LastBlock)
                            {
                                Debug.Assert(region.Kind != ControlFlowGraph.RegionKind.Root);

                                // Transfer all content of the sub-region into the current region
                                region.Locals = region.Locals.Concat(subRegion.Locals);
                                MergeSubRegionAndFree(subRegion, blocks, regionMap);
                                result = true;
                                break;
                            }
                        }

                        if (region.HasRegions)
                        {
                            for (int i = region.Regions.Count - 1; i >= 0; i--)
                            {
                                RegionBuilder subRegion = region.Regions[i];

                                if (subRegion.Kind == ControlFlowGraph.RegionKind.Locals && !subRegion.HasRegions && subRegion.FirstBlock == subRegion.LastBlock)
                                {
                                    BasicBlock block = subRegion.FirstBlock;

                                    if (block.Statements.IsEmpty && block.InternalConditional.Condition == null && block.InternalNext.Value == null)
                                    {
                                        // This sub-region has no executable code, merge block into the parent and drop the sub-region
                                        Debug.Assert(regionMap[block] == subRegion);
                                        regionMap[block] = region;
#if DEBUG
                                        subRegion.AboutToFree();
#endif 
                                        subRegion.Free();
                                        region.Regions.RemoveAt(i);
                                        result = true;
                                    }
                                }
                            }
                        }

                        break;

                    case ControlFlowGraph.RegionKind.TryAndCatch:
                    case ControlFlowGraph.RegionKind.TryAndFinally:
                    case ControlFlowGraph.RegionKind.FilterAndHandler:
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(region.Kind);
                }

                return result;
            }
        }

        /// <summary>
        /// Merge content of <paramref name="subRegion"/> into its enclosing region and free it.
        /// </summary>
        private static void MergeSubRegionAndFree(RegionBuilder subRegion, ArrayBuilder<BasicBlock> blocks, PooledDictionary<BasicBlock, RegionBuilder> regionMap)
        {
            Debug.Assert(subRegion.Kind != ControlFlowGraph.RegionKind.Root);
            RegionBuilder enclosing = subRegion.Enclosing;

#if DEBUG
            subRegion.AboutToFree();
#endif

            int firstBlockToMove = subRegion.FirstBlock.Ordinal;

            if (subRegion.HasRegions)
            {
                foreach (RegionBuilder r in subRegion.Regions)
                {
                    for (int i = firstBlockToMove; i < r.FirstBlock.Ordinal; i++)
                    {
                        Debug.Assert(regionMap[blocks[i]] == subRegion);
                        regionMap[blocks[i]] = enclosing;
                    }

                    firstBlockToMove = r.LastBlock.Ordinal + 1;
                }

                enclosing.ReplaceRegion(subRegion, subRegion.Regions);
            }
            else
            {
                enclosing.Remove(subRegion);
            }

            for (int i = firstBlockToMove; i <= subRegion.LastBlock.Ordinal; i++)
            {
                Debug.Assert(regionMap[blocks[i]] == subRegion);
                regionMap[blocks[i]] = enclosing;
            }

            subRegion.Free();
        }

        /// <summary>
        /// Do a pass to eliminate blocks without statements that can be merged with predecessor(s).
        /// Returns true if any blocks were eliminated
        /// </summary>
        private static bool PackBlocks(ArrayBuilder<BasicBlock> blocks, PooledDictionary<BasicBlock, RegionBuilder> regionMap)
        {
            ArrayBuilder<RegionBuilder> fromCurrent = null;
            ArrayBuilder<RegionBuilder> fromDestination = null;
            ArrayBuilder<RegionBuilder> fromPredecessor = null;

            bool anyRemoved = false;
            bool retry;

            do
            {
                // We set this local to true during the loop below when we make some changes that might enable 
                // transformations for basic blocks that were already looked at. We simply keep repeating the
                // pass untill no such changes are made.
                retry = false;

                int count = blocks.Count - 1;
                for (int i = 1; i < count; i++)
                {
                    BasicBlock block = blocks[i];
                    block.Ordinal = i;

                    // PROTOTYPE(dataflow): Consider if we want to do the following transformation.
                    //                      Should we care if condition has a constant value?
                    // If conditional and fallthrough branches have the same kind and destination,
                    // move condition to the statement list and clear the conditional branch
                    //if (block.InternalConditional.Condition != null &&
                    //    block.InternalConditional.Branch.Destination == block.InternalNext.Branch.Destination &&
                    //    block.InternalConditional.Branch.Kind == block.InternalNext.Branch.Kind)
                    //{
                    //    Debug.Assert(block.InternalNext.Value == null);
                    //    block.AddStatement(block.InternalConditional.Condition);
                    //    block.InternalConditional = default;
                    //    retry = true;
                    //}

                    if (!block.Statements.IsEmpty)
                    {
                        // See if we can move all statements to the previous block
                        ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;
                        BasicBlock predecessor;
                        if (predecessors.Count == 1 &&
                            (predecessor = predecessors.Single()).InternalConditional.Condition == null &&
                            predecessor.Kind != BasicBlockKind.Entry &&
                            predecessor.InternalNext.Branch.Destination == block &&
                            regionMap[predecessor] == regionMap[block])
                        {
                            Debug.Assert(predecessor.InternalNext.Value == null);
                            Debug.Assert(predecessor.InternalNext.Branch.Kind == BasicBlock.BranchKind.Regular);

                            predecessor.AddStatements(block.Statements);
                            block.RemoveStatements();
                        }
                        else
                        {
                            continue;
                        }
                    }

                    ref BasicBlock.Branch next = ref block.InternalNext.Branch;

                    Debug.Assert((block.InternalNext.Value != null) == (next.Kind == BasicBlock.BranchKind.Return || next.Kind == BasicBlock.BranchKind.Throw));
                    Debug.Assert((next.Destination == null) ==
                                 (next.Kind == BasicBlock.BranchKind.ProgramTermination ||
                                  next.Kind == BasicBlock.BranchKind.Throw ||
                                  next.Kind == BasicBlock.BranchKind.ReThrow ||
                                  next.Kind == BasicBlock.BranchKind.StructuredExceptionHandling));

#if DEBUG
                    if (next.Kind == BasicBlock.BranchKind.StructuredExceptionHandling)
                    {
                        RegionBuilder currentRegion = regionMap[block];
                        Debug.Assert(currentRegion.Kind == ControlFlowGraph.RegionKind.Filter ||
                                     currentRegion.Kind == ControlFlowGraph.RegionKind.Finally);
                        Debug.Assert(block == currentRegion.LastBlock);
                    }
#endif

                    if (block.InternalConditional.Condition == null)
                    {
                        if (next.Destination == block)
                        {
                            continue;
                        }

                        RegionBuilder currentRegion = regionMap[block];

                        // Is this the only block in the region
                        if (currentRegion.FirstBlock == currentRegion.LastBlock)
                        {
                            Debug.Assert(currentRegion.FirstBlock == block);
                            Debug.Assert(!currentRegion.HasRegions);

                            // Remove Try/Finally if Finally is empty
                            if (currentRegion.Kind == ControlFlowGraph.RegionKind.Finally &&
                                next.Destination == null && next.Kind == BasicBlock.BranchKind.StructuredExceptionHandling &&
                                block.Predecessors.IsEmpty)
                            {
                                // Nothing useful is happening in this finally, let's remove it
                                RegionBuilder tryAndFinally = currentRegion.Enclosing;
                                Debug.Assert(tryAndFinally.Kind == ControlFlowGraph.RegionKind.TryAndFinally);
                                Debug.Assert(tryAndFinally.Regions.Count == 2);

                                RegionBuilder @try = tryAndFinally.Regions.First();
                                Debug.Assert(@try.Kind == ControlFlowGraph.RegionKind.Try);
                                Debug.Assert(tryAndFinally.Regions.Last() == currentRegion);

                                // If .try region has locals, let's convert it to .locals, otherwise drop it
                                if (@try.Locals.IsEmpty)
                                {
                                    i = @try.FirstBlock.Ordinal - 1; // restart at the first block of removed .try region
                                    MergeSubRegionAndFree(@try, blocks, regionMap);
                                }
                                else
                                {
                                    @try.Kind = ControlFlowGraph.RegionKind.Locals;
                                    i--; // restart at the block that was following the tryAndFinally
                                }

                                MergeSubRegionAndFree(currentRegion, blocks, regionMap);

                                RegionBuilder tryAndFinallyEnclosing = tryAndFinally.Enclosing;
                                MergeSubRegionAndFree(tryAndFinally, blocks, regionMap);

                                count--;
                                Debug.Assert(regionMap[block] == tryAndFinallyEnclosing);
                                removeBlock(block, tryAndFinallyEnclosing);
                                anyRemoved = true;
                                retry = true;
                            }

                            continue;
                        }

                        if (next.Kind == BasicBlock.BranchKind.StructuredExceptionHandling)
                        {
                            Debug.Assert(block.InternalNext.Value == null);
                            Debug.Assert(next.Destination == null);

                            ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;

                            // It is safe to drop an unreachable empty basic block
                            if (predecessors.Count > 0)
                            {
                                if (predecessors.Count != 1)
                                {
                                    continue;
                                }

                                BasicBlock predecessor = predecessors.Single();

                                if (predecessor.Ordinal != i - 1 ||
                                    predecessor.InternalNext.Branch.Destination != block ||
                                    predecessor.InternalConditional.Branch.Destination == block ||
                                    regionMap[predecessor] != currentRegion)
                                {
                                    // Do not merge StructuredExceptionHandling into the middle of the filter or finally,
                                    // Do not merge StructuredExceptionHandling into conditional branch
                                    // Do not merge StructuredExceptionHandling into a different region
                                    // It is much easier to walk the graph when we can rely on the fact that a StructuredExceptionHandling
                                    // branch is only in the last block in the region, if it is present.
                                    continue;
                                }

                                predecessor.InternalNext = block.InternalNext;
                            }
                        }
                        else
                        {
                            Debug.Assert(next.Kind == BasicBlock.BranchKind.Regular ||
                                         next.Kind == BasicBlock.BranchKind.Return ||
                                         next.Kind == BasicBlock.BranchKind.Throw ||
                                         next.Kind == BasicBlock.BranchKind.ReThrow ||
                                         next.Kind == BasicBlock.BranchKind.ProgramTermination);

                            ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;
                            IOperation value = block.InternalNext.Value;

                            RegionBuilder implicitEntryRegion = tryGetImplicitEntryRegion(block, currentRegion);

                            if (implicitEntryRegion != null)
                            {
                                // First blocks in filter/catch/finally do not capture all possible predecessors
                                // Do not try to merge them, unless they are simply linked to the next block
                                if (value != null ||
                                    next.Destination != blocks[i + 1])
                                {
                                    continue;
                                }

                                Debug.Assert(implicitEntryRegion.LastBlock.Ordinal >= next.Destination.Ordinal);
                            }

                            if (value != null)
                            {
                                BasicBlock predecessor;
                                int predecessorsCount = predecessors.Count;

                                if (predecessorsCount == 0 && next.Kind == BasicBlock.BranchKind.Return)
                                {
                                    // Let's drop an unreachable compiler generated return that VB optimistically adds at the end of a method body
                                    if (next.Destination.Kind != BasicBlockKind.Exit ||
                                        !value.IsImplicit ||
                                        value.Kind != OperationKind.LocalReference ||
                                        !((ILocalReferenceOperation)value).Local.IsFunctionValue)
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (predecessorsCount != 1 ||
                                      (predecessor = predecessors.Single()).InternalConditional.Branch.Destination == block ||
                                      predecessor.Kind == BasicBlockKind.Entry ||
                                      regionMap[predecessor] != currentRegion)
                                    {
                                        // Do not merge return/throw with expression with more than one predecessor
                                        // Do not merge return/throw with expression with conditional branch
                                        // Do not merge return/throw with expression with an entry block
                                        // Do not merge return/throw with expression into a different region
                                        continue;
                                    }

                                    Debug.Assert(predecessor.InternalNext.Branch.Destination == block);
                                }
                            }

                            // For throw/re-throw assume there is no specific destination region
                            RegionBuilder destinationRegionOpt = next.Destination == null ? null : regionMap[next.Destination];

                            // If source and destination are in different regions, it might
                            // be unsafe to merge branches.
                            if (currentRegion != destinationRegionOpt)
                            {
                                fromCurrent?.Clear();
                                fromDestination?.Clear();

                                if (!checkBranchesFromPredecessors(block, currentRegion, destinationRegionOpt))
                                {
                                    continue;
                                }
                            }

                            foreach (BasicBlock predecessor in predecessors)
                            {
                                if (tryMergeBranch(predecessor, ref predecessor.InternalNext.Branch, block))
                                {
                                    Debug.Assert(predecessor.InternalNext.Value == null);
                                    predecessor.InternalNext.Value = value;
                                }

                                if (tryMergeBranch(predecessor, ref predecessor.InternalConditional.Branch, block))
                                {
                                    Debug.Assert(value == null);
                                }
                            }

                            next.Destination?.RemovePredecessor(block);
                        }

                        i--;
                        count--;
                        removeBlock(block, currentRegion);
                        anyRemoved = true;
                        retry = true;
                    }
                    else
                    {
                        if (next.Kind == BasicBlock.BranchKind.StructuredExceptionHandling)
                        {
                            continue;
                        }

                        Debug.Assert(next.Kind == BasicBlock.BranchKind.Regular ||
                                     next.Kind == BasicBlock.BranchKind.Return ||
                                     next.Kind == BasicBlock.BranchKind.Throw ||
                                     next.Kind == BasicBlock.BranchKind.ReThrow ||
                                     next.Kind == BasicBlock.BranchKind.ProgramTermination);

                        ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;

                        if (predecessors.Count != 1)
                        {
                            continue;
                        }

                        RegionBuilder currentRegion = regionMap[block];
                        if (tryGetImplicitEntryRegion(block, currentRegion) != null)
                        {
                            // First blocks in filter/catch/finally do not capture all possible predecessors
                            // Do not try to merge conditional branches in them
                            continue;
                        }

                        BasicBlock predecessor = predecessors.Single();

                        if (predecessor.Kind != BasicBlockKind.Entry &&
                            predecessor.InternalNext.Branch.Destination == block &&
                            predecessor.InternalConditional.Condition == null &&
                            regionMap[predecessor] == currentRegion)
                        {
                            Debug.Assert(predecessor != block);
                            Debug.Assert(predecessor.InternalNext.Value == null);

                            mergeBranch(predecessor, ref predecessor.InternalNext.Branch, ref next);

                            predecessor.InternalNext.Value = block.InternalNext.Value;
                            next.Destination?.RemovePredecessor(block);

                            predecessor.InternalConditional = block.InternalConditional;
                            BasicBlock destination = block.InternalConditional.Branch.Destination;
                            if (destination != null)
                            {
                                destination.AddPredecessor(predecessor);
                                destination.RemovePredecessor(block);
                            }

                            i--;
                            count--;
                            removeBlock(block, currentRegion);
                            anyRemoved = true;
                            retry = true;
                        }
                    }
                }

                blocks[0].Ordinal = 0;
                blocks[count].Ordinal = count;
            }
            while (retry);

            fromCurrent?.Free();
            fromDestination?.Free();
            fromPredecessor?.Free();

            return anyRemoved;

            RegionBuilder tryGetImplicitEntryRegion(BasicBlock block, RegionBuilder currentRegion)
            {
                do
                {
                    if (currentRegion.FirstBlock != block)
                    {
                        return null;
                    }

                    switch (currentRegion.Kind)
                    {
                        case ControlFlowGraph.RegionKind.Filter:
                        case ControlFlowGraph.RegionKind.Catch:
                        case ControlFlowGraph.RegionKind.Finally:
                            return currentRegion;
                    }

                    currentRegion = currentRegion.Enclosing;
                }
                while (currentRegion != null);

                return null;
            }

            void removeBlock(BasicBlock block, RegionBuilder region)
            {
                Debug.Assert(region.FirstBlock.Ordinal >= 0);
                Debug.Assert(region.FirstBlock.Ordinal <= region.LastBlock.Ordinal);
                Debug.Assert(region.FirstBlock.Ordinal <= block.Ordinal);
                Debug.Assert(block.Ordinal <= region.LastBlock.Ordinal);

                if (region.FirstBlock == block)
                {
                    BasicBlock newFirst = blocks[block.Ordinal + 1];
                    region.FirstBlock = newFirst;
                    RegionBuilder enclosing = region.Enclosing;
                    while (enclosing != null && enclosing.FirstBlock == block)
                    {
                        enclosing.FirstBlock = newFirst;
                        enclosing = enclosing.Enclosing;
                    }
                }
                else if (region.LastBlock == block)
                {
                    BasicBlock newLast = blocks[block.Ordinal - 1];
                    region.LastBlock = newLast;
                    RegionBuilder enclosing = region.Enclosing;
                    while (enclosing != null && enclosing.LastBlock == block)
                    {
                        enclosing.LastBlock = newLast;
                        enclosing = enclosing.Enclosing;
                    }
                }

                Debug.Assert(region.FirstBlock.Ordinal <= region.LastBlock.Ordinal);

                bool removed = regionMap.Remove(block);
                Debug.Assert(removed);
                Debug.Assert(blocks[block.Ordinal] == block);
                blocks.RemoveAt(block.Ordinal);
                block.Ordinal = -1;
            }

            bool tryMergeBranch(BasicBlock predecessor, ref BasicBlock.Branch predecessorBranch, BasicBlock successor)
            {
                if (predecessorBranch.Destination == successor)
                {
                    mergeBranch(predecessor, ref predecessorBranch, ref successor.InternalNext.Branch);
                    return true;
                }

                return false;
            }

            void mergeBranch(BasicBlock predecessor, ref BasicBlock.Branch predecessorBranch, ref BasicBlock.Branch successorBranch)
            {
                predecessorBranch.Destination = successorBranch.Destination;
                successorBranch.Destination?.AddPredecessor(predecessor);
                Debug.Assert(predecessorBranch.Kind == BasicBlock.BranchKind.Regular);
                predecessorBranch.Kind = successorBranch.Kind;
            }

            bool checkBranchesFromPredecessors(BasicBlock block, RegionBuilder currentRegion, RegionBuilder destinationRegionOpt)
            {
                foreach (BasicBlock predecessor in block.Predecessors)
                {
                    RegionBuilder predecessorRegion = regionMap[predecessor];

                    // If source and destination are in different regions, it might
                    // be unsafe to merge branches.
                    if (predecessorRegion != currentRegion)
                    {
                        if (destinationRegionOpt == null)
                        {
                            // destination is unknown and predecessor is in different region, do not merge
                            return false;
                        }

                        fromPredecessor?.Clear();
                        collectAncestorsAndSelf(currentRegion, ref fromCurrent);
                        collectAncestorsAndSelf(destinationRegionOpt, ref fromDestination);
                        collectAncestorsAndSelf(predecessorRegion, ref fromPredecessor);

                        // On the way from predecessor directly to the destination, are we going leave the same regions as on the way
                        // from predecessor to the current block and then to the destination?
                        int lastLeftRegionOnTheWayFromCurrentToDestination = getIndexOfLastLeftRegion(fromCurrent, fromDestination);
                        int lastLeftRegionOnTheWayFromPredecessorToDestination = getIndexOfLastLeftRegion(fromPredecessor, fromDestination);
                        int lastLeftRegionOnTheWayFromPredecessorToCurrentBlock = getIndexOfLastLeftRegion(fromPredecessor, fromCurrent);

                        // Since we are navigating up and down the tree and only movements up are significant, if we made the same number 
                        // of movements up during direct and indirect transition, we must have made the same movements up.
                        if ((fromPredecessor.Count - lastLeftRegionOnTheWayFromPredecessorToCurrentBlock +
                            fromCurrent.Count - lastLeftRegionOnTheWayFromCurrentToDestination) !=
                            (fromPredecessor.Count - lastLeftRegionOnTheWayFromPredecessorToDestination))
                        {
                            // We have different transitions 
                            return false;
                        }
                    }
                    else if (predecessor.Kind == BasicBlockKind.Entry && destinationRegionOpt == null)
                    {
                        // Do not merge throw into an entry block
                        return false;
                    }
                }

                return true;
            }

            void collectAncestorsAndSelf(RegionBuilder from, ref ArrayBuilder<RegionBuilder> builder)
            {
                if (builder == null)
                {
                    builder = ArrayBuilder<RegionBuilder>.GetInstance();
                }
                else if (builder.Count != 0)
                {
                    return;
                }

                do
                {
                    builder.Add(from);
                    from = from.Enclosing;
                }
                while (from != null);

                builder.ReverseContents();
            }

            // Can return index beyond bounds of "from" when no regions will be left.
            int getIndexOfLastLeftRegion(ArrayBuilder<RegionBuilder> from, ArrayBuilder<RegionBuilder> to)
            {
                int mismatch = 0;

                while (mismatch < from.Count && mismatch < to.Count && from[mismatch] == to[mismatch])
                {
                    mismatch++;
                }

                return mismatch;
            }
        }

        /// <summary>
        /// Deal with labeled blocks that were not added to the graph because labels were never found
        /// </summary>
        private static void CheckUnresolvedBranches(ArrayBuilder<BasicBlock> blocks, PooledDictionary<ILabelSymbol, BasicBlock> labeledBlocks)
        {
            if (labeledBlocks == null)
            {
                return;
            }

            foreach (BasicBlock labeled in labeledBlocks.Values)
            {
                if (labeled.Ordinal == -1)
                {
                    // Neither VB nor C# produce trees with unresolved branches 
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private void VisitStatement(IOperation operation)
        {
            IOperation saveCurrentStatement = _currentStatement;
            _currentStatement = operation;
            Debug.Assert(_evalStack.Count == 0);

            AddStatement(Visit(operation, null));
            Debug.Assert(_evalStack.Count == 0);
            _currentStatement = saveCurrentStatement;
        }

        private BasicBlock CurrentBasicBlock
        {
            get
            {
                if (_currentBasicBlock == null)
                {
                    AppendNewBlock(new BasicBlock(BasicBlockKind.Block));
                }

                return _currentBasicBlock;
            }
        }

        private void AddStatement(
            IOperation statement
#if DEBUG
            , bool spillingTheStack = false
#endif
            )
        {
#if DEBUG
            Debug.Assert(spillingTheStack || _evalStack.All(o => o.Kind == OperationKind.FlowCaptureReference || o.Kind == OperationKind.DeclarationExpression));
#endif
            if (statement == null)
            {
                return;
            }

            Operation.SetParentOperation(statement, null);
            CurrentBasicBlock.AddStatement(statement);
        }

        private void AppendNewBlock(BasicBlock block, bool linkToPrevious = true)
        {
            Debug.Assert(block != null);

            if (linkToPrevious)
            {
                BasicBlock prevBlock = _blocks.Last();

                if (prevBlock.InternalNext.Branch.Destination == null)
                {
                    LinkBlocks(prevBlock, block);
                }
            }

            block.Ordinal = _blocks.Count;
            _blocks.Add(block);
            _currentBasicBlock = block;
            _currentRegion.ExtendToInclude(block);
            _regionMap.Add(block, _currentRegion);
        }

        private void EnterRegion(RegionBuilder region)
        {
            _currentRegion?.Add(region);
            _currentRegion = region;
            _currentBasicBlock = null;
        }

        private void LeaveRegion()
        {
            // Ensure there is at least one block in the region
            if (_currentRegion.IsEmpty)
            {
                AppendNewBlock(new BasicBlock(BasicBlockKind.Block));
            }

            RegionBuilder enclosed = _currentRegion;
            _currentRegion = _currentRegion.Enclosing;
            _currentRegion?.ExtendToInclude(enclosed.LastBlock);
            _currentBasicBlock = null;
        }

        private static void LinkBlocks(BasicBlock prevBlock, BasicBlock nextBlock, BasicBlock.BranchKind branchKind = BasicBlock.BranchKind.Regular)
        {
            Debug.Assert(prevBlock.InternalNext.Value == null);
            Debug.Assert(prevBlock.InternalNext.Branch.Destination == null);
            prevBlock.InternalNext.Branch.Destination = nextBlock;
            prevBlock.InternalNext.Branch.Kind = branchKind;
            nextBlock.AddPredecessor(prevBlock);
        }

        public override IOperation VisitBlock(IBlockOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            bool haveLocals = !operation.Locals.IsEmpty;
            if (haveLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.Locals));
            }

            VisitStatements(operation.Operations);

            if (haveLocals)
            {
                LeaveRegion();
            }

            return null;
        }

        private void VisitStatements(ImmutableArray<IOperation> statements)
        {
            foreach (var statement in statements)
            {
                VisitStatement(statement);
            }
        }

        private void VisitStatements(IEnumerable<IOperation> statements)
        {
            foreach (var statement in statements)
            {
                VisitStatement(statement);
            }
        }

        public override IOperation VisitConditional(IConditionalOperation operation, int? captureIdForResult)
        {
            if (operation == _currentStatement)
            {
                if (operation.WhenFalse == null)
                {
                    // if (condition) 
                    //   consequence;  
                    //
                    // becomes
                    //
                    // GotoIfFalse condition afterif;
                    // consequence;
                    // afterif:

                    BasicBlock afterIf = null;
                    VisitConditionalBranch(operation.Condition, ref afterIf, sense: false);
                    VisitStatement(operation.WhenTrue);
                    AppendNewBlock(afterIf);
                }
                else
                {
                    // if (condition)
                    //     consequence;
                    // else 
                    //     alternative
                    //
                    // becomes
                    //
                    // GotoIfFalse condition alt;
                    // consequence
                    // goto afterif;
                    // alt:
                    // alternative;
                    // afterif:

                    BasicBlock whenFalse = null;
                    VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                    VisitStatement(operation.WhenTrue);

                    var afterIf = new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, afterIf);
                    _currentBasicBlock = null;

                    AppendNewBlock(whenFalse);
                    VisitStatement(operation.WhenFalse);

                    AppendNewBlock(afterIf);
                }

                return null;
            }
            else
            {
                // condition ? consequence : alternative
                //
                // becomes
                //
                // GotoIfFalse condition alt;
                // capture = consequence
                // goto afterif;
                // alt:
                // capture = alternative;
                // afterif:
                // result - capture

                SpillEvalStack();
                int captureId = captureIdForResult ?? _availableCaptureId++;

                BasicBlock whenFalse = null;
                VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                VisitAndCapture(operation.WhenTrue, captureId);

                var afterIf = new BasicBlock(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterIf);
                _currentBasicBlock = null;

                AppendNewBlock(whenFalse);

                VisitAndCapture(operation.WhenFalse, captureId);

                AppendNewBlock(afterIf);

                return new FlowCaptureReference(captureId, operation.Syntax, operation.Type, operation.ConstantValue);
            }
        }

        private void VisitAndCapture(IOperation operation, int captureId)
        {
            IOperation result = Visit(operation, captureId);
            CaptureResultIfNotAlready(operation.Syntax, captureId, result);
        }

        private int VisitAndCapture(IOperation operation)
        {
            int saveAvailableCaptureId = _availableCaptureId;
            IOperation rewritten = Visit(operation);

            int captureId;
            if (rewritten.Kind != OperationKind.FlowCaptureReference ||
                saveAvailableCaptureId > (captureId = ((IFlowCaptureReferenceOperation)rewritten).Id))
            {
                captureId = _availableCaptureId++;
                AddStatement(new FlowCapture(captureId, operation.Syntax, rewritten));
            }

            return captureId;
        }

        private void CaptureResultIfNotAlready(SyntaxNode syntax, int captureId, IOperation result)
        {
            if (result.Kind != OperationKind.FlowCaptureReference ||
                captureId != ((IFlowCaptureReferenceOperation)result).Id)
            {
                AddStatement(new FlowCapture(captureId, syntax, result));
            }
        }

        private void SpillEvalStack()
        {
            for (int i = 0; i < _evalStack.Count; i++)
            {
                IOperation operation = _evalStack[i];
                // Declarations cannot have control flow, so we don't need to spill them.
                if (operation.Kind != OperationKind.FlowCaptureReference && operation.Kind != OperationKind.DeclarationExpression)
                {
                    int captureId = _availableCaptureId++;

                    AddStatement(new FlowCapture(captureId, operation.Syntax, operation)
#if DEBUG
                                 , spillingTheStack: true
#endif
                                );

                    _evalStack[i] = new FlowCaptureReference(captureId, operation.Syntax, operation.Type, operation.ConstantValue);
                }
            }
        }

        // PROTOTYPE(dataflow): Revisit and determine if this is too much abstraction, or if it's fine as it is.
        private void PushArray<T>(ImmutableArray<T> array, Func<T, IOperation> unwrapper = null) where T : IOperation
        {
            Debug.Assert(unwrapper != null || typeof(T) == typeof(IOperation));
            foreach (var element in array)
            {
                _evalStack.Push(Visit(unwrapper == null ? element : unwrapper(element)));
            }
        }

        private ImmutableArray<T> PopArray<T>(ImmutableArray<T> originalArray, Func<IOperation, int, ImmutableArray<T>, T> wrapper = null) where T : IOperation
        {
            Debug.Assert(wrapper != null || typeof(T) == typeof(IOperation));
            int numElements = originalArray.Length;
            if (numElements == 0)
            {
                return ImmutableArray<T>.Empty;
            }
            else
            {
                var builder = ArrayBuilder<T>.GetInstance(numElements);
                // Iterate in reverse order so the index corresponds to the original index when pushed onto the stack
                for (int i = numElements - 1; i >= 0; i--)
                {
                    IOperation visitedElement = _evalStack.Pop();
                    builder.Add(wrapper != null ? wrapper(visitedElement, i, originalArray) : (T)visitedElement);
                }
                builder.ReverseContents();
                return builder.ToImmutableAndFree();
            }
        }

        private ImmutableArray<IArgumentOperation> VisitArguments(ImmutableArray<IArgumentOperation> arguments)
        {
            PushArray(arguments, UnwrapArgument);
            return PopArray(arguments, RewriteArgumentFromArray);

            IOperation UnwrapArgument(IArgumentOperation argument)
            {
                return argument.Value;
            }

            IArgumentOperation RewriteArgumentFromArray(IOperation visitedArgument, int index, ImmutableArray<IArgumentOperation> args)
            {
                Debug.Assert(index >= 0 && index < args.Length);
                var originalArgument = (BaseArgument)args[index];
                return new ArgumentOperation(visitedArgument, originalArgument.ArgumentKind, originalArgument.Parameter,
                                             originalArgument.InConversionConvertibleOpt, originalArgument.OutConversionConvertibleOpt,
                                             semanticModel: null, originalArgument.Syntax, originalArgument.IsImplicit);
            }
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, int? captureIdForResult)
        {
            _evalStack.Push(Visit(operation.Target));
            IOperation value = Visit(operation.Value);
            return new SimpleAssignmentExpression(_evalStack.Pop(), operation.IsRef, value, null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, int? captureIdForResult)
        {
            var compoundAssignment = (BaseCompoundAssignmentExpression)operation;
            _evalStack.Push(Visit(compoundAssignment.Target));
            IOperation value = Visit(compoundAssignment.Value);

            return new CompoundAssignmentOperation(_evalStack.Pop(), value, compoundAssignment.InConversionConvertible, compoundAssignment.OutConversionConvertible, operation.OperatorKind, operation.IsLifted, operation.IsChecked, operation.OperatorMethod, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        // PROTOTYPE(dataflow):
        //public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, int? captureIdForResult)
        //{
        //    _evalStack.Push(Visit(operation.ArrayReference));
        //    foreach (var index in operation.Indices)
        //    return new ArrayElementReferenceExpression(Visit(operation.ArrayReference), VisitArray(operation.Indices), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        //}

        private static bool IsConditional(IBinaryOperation operation)
        {
            switch (operation.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                case BinaryOperatorKind.ConditionalAnd:
                    return true;
            }

            return false;
        }

        public override IOperation VisitBinaryOperator(IBinaryOperation operation, int? captureIdForResult)
        {
            if (IsConditional(operation) &&
                operation.OperatorMethod == null && // PROTOTYPE(dataflow): cannot properly handle user-defined conditional operators yet.
                operation.Type.SpecialType == SpecialType.System_Boolean) // PROTOTYPE(dataflow): cannot properly handle nullable conditional operators yet.
            {
                return VisitBinaryConditionalOperator(operation, sense: true, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
            }

            _evalStack.Push(Visit(operation.LeftOperand));
            IOperation rightOperand = Visit(operation.RightOperand);
            return new BinaryOperatorExpression(operation.OperatorKind, _evalStack.Pop(), rightOperand, operation.IsLifted, operation.IsChecked, operation.IsCompareText, operation.OperatorMethod, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): ensure we properly detect logical Not
            if (operation.OperatorKind == UnaryOperatorKind.Not)
            {
                return VisitConditionalExpression(operation.Operand, sense: false, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
            }

            return new UnaryOperatorExpression(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.OperatorMethod, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        private IOperation VisitBinaryConditionalOperator(IBinaryOperation binOp, bool sense, int? captureIdForResult,
                                                          BasicBlock fallToTrueOpt, BasicBlock fallToFalseOpt)
        {
            bool andOrSense = sense;

            switch (binOp.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                    Debug.Assert(binOp.LeftOperand.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.RightOperand.Type.SpecialType == SpecialType.System_Boolean);

                    // Rewrite (a || b) as ~(~a && ~b)
                    andOrSense = !andOrSense;
                    // Fall through
                    goto case BinaryOperatorKind.ConditionalAnd;

                case BinaryOperatorKind.ConditionalAnd:
                    Debug.Assert(binOp.LeftOperand.Type.SpecialType == SpecialType.System_Boolean);
                    Debug.Assert(binOp.RightOperand.Type.SpecialType == SpecialType.System_Boolean);

                    // ~(a && b) is equivalent to (~a || ~b)
                    if (!andOrSense)
                    {
                        // generate (~a || ~b)
                        return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: sense, stopValue: true, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
                    }
                    else
                    {
                        // generate (a && b)
                        return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: !sense, stopValue: false, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(binOp.OperatorKind);
            }
        }

        private IOperation VisitShortCircuitingOperator(IBinaryOperation condition, bool sense, bool stopSense, bool stopValue,
                                                        int? captureIdForResult, BasicBlock fallToTrueOpt, BasicBlock fallToFalseOpt)
        {
            // we generate:
            //
            // gotoif (a == stopSense) fallThrough
            // b == sense
            // goto labEnd
            // fallThrough:
            // stopValue
            // labEnd:
            //                 AND       OR
            //            +-  ------    -----
            // stopSense  |   !sense    sense
            // stopValue  |     0         1

            SpillEvalStack();
            int captureId = captureIdForResult ?? _availableCaptureId++;

            ref BasicBlock lazyFallThrough = ref stopSense ? ref fallToTrueOpt : ref fallToFalseOpt;
            bool newFallThroughBlock = (lazyFallThrough == null);

            VisitConditionalBranch(condition.LeftOperand, ref lazyFallThrough, stopSense);

            IOperation resultFromRight = VisitConditionalExpression(condition.RightOperand, sense, captureId, fallToTrueOpt, fallToFalseOpt);

            CaptureResultIfNotAlready(condition.RightOperand.Syntax, captureId, resultFromRight);

            if (newFallThroughBlock)
            {
                var labEnd = new BasicBlock(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, labEnd);
                _currentBasicBlock = null;

                AppendNewBlock(lazyFallThrough);

                var constantValue = new Optional<object>(stopValue);
                SyntaxNode leftSyntax = (lazyFallThrough.Predecessors.Count == 1 ? condition.LeftOperand : condition).Syntax;
                AddStatement(new FlowCapture(captureId, leftSyntax, new LiteralExpression(semanticModel: null, leftSyntax, condition.Type, constantValue, isImplicit: true)));

                AppendNewBlock(labEnd);
            }

            return new FlowCaptureReference(captureId, condition.Syntax, condition.Type, condition.ConstantValue);
        }

        private IOperation VisitConditionalExpression(IOperation condition, bool sense, int? captureIdForResult, BasicBlock fallToTrueOpt, BasicBlock fallToFalseOpt)
        {
            // PROTOTYPE(dataflow): Do not erase UnaryOperatorKind.Not if ProduceIsSense below will have to add it back.
            while (condition.Kind == OperationKind.UnaryOperator)
            {
                var unOp = (IUnaryOperation)condition;
                // PROTOTYPE(dataflow): ensure we properly detect logical Not
                if (unOp.OperatorKind != UnaryOperatorKind.Not)
                {
                    break;
                }
                condition = unOp.Operand;
                sense = !sense;
            }

            Debug.Assert(condition.Type.SpecialType == SpecialType.System_Boolean);

            if (condition.Kind == OperationKind.BinaryOperator)
            {
                var binOp = (IBinaryOperation)condition;
                if (IsConditional(binOp))
                {
                    return VisitBinaryConditionalOperator(binOp, sense, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
                }
            }

            return ProduceIsSense(Visit(condition), sense);
        }

        private IOperation ProduceIsSense(IOperation condition, bool sense)
        {
            if (!sense)
            {
                return new UnaryOperatorExpression(UnaryOperatorKind.Not,
                                                   condition,
                                                   isLifted: false, // PROTOTYPE(dataflow): Deal with nullable
                                                   isChecked: false,
                                                   operatorMethod: null,
                                                   semanticModel: null,
                                                   condition.Syntax,
                                                   condition.Type,
                                                   constantValue: default, // revert constant value if we have one.
                                                   isImplicit: true);
            }

            return condition;
        }

        private void VisitConditionalBranch(IOperation condition, ref BasicBlock dest, bool sense)
        {
oneMoreTime:

            while (condition.Kind == OperationKind.Parenthesized)
            {
                condition = ((IParenthesizedOperation)condition).Operand;
            }

            switch (condition.Kind)
            {
                case OperationKind.BinaryOperator:
                    var binOp = (IBinaryOperation)condition;
                    bool testBothArgs = sense;

                    switch (binOp.OperatorKind)
                    {
                        case BinaryOperatorKind.ConditionalOr:
                            testBothArgs = !testBothArgs;
                            // Fall through
                            goto case BinaryOperatorKind.ConditionalAnd;

                        case BinaryOperatorKind.ConditionalAnd:
                            if (testBothArgs)
                            {
                                // gotoif(a != sense) fallThrough
                                // gotoif(b == sense) dest
                                // fallThrough:

                                BasicBlock fallThrough = null;

                                VisitConditionalBranch(binOp.LeftOperand, ref fallThrough, !sense);
                                VisitConditionalBranch(binOp.RightOperand, ref dest, sense);
                                AppendNewBlock(fallThrough);
                            }
                            else
                            {
                                // gotoif(a == sense) labDest
                                // gotoif(b == sense) labDest

                                VisitConditionalBranch(binOp.LeftOperand, ref dest, sense);
                                condition = binOp.RightOperand;
                                goto oneMoreTime;
                            }
                            return;
                    }

                    // none of above. 
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

                case OperationKind.UnaryOperator:
                    var unOp = (IUnaryOperation)condition;

                    if (unOp.OperatorKind == UnaryOperatorKind.Not && unOp.Operand.Type.SpecialType == SpecialType.System_Boolean)
                    {
                        sense = !sense;
                        condition = unOp.Operand;
                        goto oneMoreTime;
                    }
                    goto default;

                case OperationKind.Conditional:
                    if (condition.Type.SpecialType == SpecialType.System_Boolean)
                    {
                        var conditional = (IConditionalOperation)condition;

                        BasicBlock whenFalse = null;
                        VisitConditionalBranch(conditional.Condition, ref whenFalse, sense: false);
                        VisitConditionalBranch(conditional.WhenTrue, ref dest, sense);

                        var afterIf = new BasicBlock(BasicBlockKind.Block);
                        LinkBlocks(CurrentBasicBlock, afterIf);
                        _currentBasicBlock = null;

                        AppendNewBlock(whenFalse);
                        VisitConditionalBranch(conditional.WhenFalse, ref dest, sense);
                        AppendNewBlock(afterIf);

                        return;
                    }
                    goto default;

                case OperationKind.Coalesce:
                    if (condition.Type.SpecialType == SpecialType.System_Boolean)
                    {
                        var coalesce = (ICoalesceOperation)condition;

                        var whenNull = new BasicBlock(BasicBlockKind.Block);
                        IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(coalesce, whenNull);

                        convertedTestExpression = Operation.SetParentOperation(convertedTestExpression, null);
                        dest = dest ?? new BasicBlock(BasicBlockKind.Block);
                        LinkBlocks(CurrentBasicBlock, (convertedTestExpression, sense, RegularBranch(dest)));
                        _currentBasicBlock = null;

                        var afterCoalesce = new BasicBlock(BasicBlockKind.Block);
                        LinkBlocks(CurrentBasicBlock, afterCoalesce);
                        _currentBasicBlock = null;

                        AppendNewBlock(whenNull);
                        VisitConditionalBranch(coalesce.WhenNull, ref dest, sense);

                        AppendNewBlock(afterCoalesce);

                        return;
                    }
                    goto default;

                case OperationKind.Conversion:
                    var conversion = (IConversionOperation)condition;

                    if (conversion.Operand.Kind == OperationKind.Throw)
                    {
                        Visit(conversion.Operand);
                        dest = dest ?? new BasicBlock(BasicBlockKind.Block);
                        return;
                    }
                    goto default;

                default:
                    condition = Operation.SetParentOperation(Visit(condition), null);
                    dest = dest ?? new BasicBlock(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, (condition, sense, RegularBranch(dest)));
                    _currentBasicBlock = null;
                    return;
            }
        }

        private static void LinkBlocks(BasicBlock previous, (IOperation Condition, bool JumpIfTrue, BasicBlock.Branch Branch) next)
        {
            Debug.Assert(previous.InternalConditional.Condition == null);
            Debug.Assert(next.Condition != null);
            Debug.Assert(next.Condition.Parent == null);
            next.Branch.Destination.AddPredecessor(previous);
            previous.InternalConditional = next;
        }

        /// <summary>
        /// Returns converted test expression
        /// </summary>
        private IOperation NullCheckAndConvertCoalesceValue(ICoalesceOperation operation, BasicBlock whenNull)
        {
            SyntaxNode valueSyntax = operation.Value.Syntax;
            ITypeSymbol valueTypeOpt = operation.Value.Type;

            SpillEvalStack();
            int testExpressionCaptureId = VisitAndCapture(operation.Value);

            Optional<object> constantValue = operation.Value.ConstantValue;

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            LinkBlocks(CurrentBasicBlock,
                       (Operation.SetParentOperation(MakeIsNullOperation(new FlowCaptureReference(testExpressionCaptureId, valueSyntax, valueTypeOpt, constantValue),
                                                                         compilation),
                                                     null),
                        true,
                        RegularBranch(whenNull)));
            _currentBasicBlock = null;

            CommonConversion testConversion = operation.ValueConversion;
            var capturedValue = new FlowCaptureReference(testExpressionCaptureId, valueSyntax, valueTypeOpt, constantValue);
            IOperation convertedTestExpression = null;

            if (testConversion.Exists)
            {
                IOperation possiblyUnwrappedValue;

                if (ITypeSymbolHelpers.IsNullableType(valueTypeOpt) &&
                    (!testConversion.IsIdentity || !ITypeSymbolHelpers.IsNullableType(operation.Type)))
                {
                    possiblyUnwrappedValue = TryUnwrapNullableValue(capturedValue, compilation);
                    // PROTOTYPE(dataflow): The scenario with missing GetValueOrDefault is not covered by unit-tests.
                }
                else
                {
                    possiblyUnwrappedValue = capturedValue;
                }

                if (possiblyUnwrappedValue != null)
                {
                    if (testConversion.IsIdentity)
                    {
                        convertedTestExpression = possiblyUnwrappedValue;
                    }
                    else
                    {
                        convertedTestExpression = new ConversionOperation(possiblyUnwrappedValue, ((BaseCoalesceExpression)operation).ConvertibleValueConversion,
                                                                          isTryCast: false, isChecked: false, semanticModel: null, valueSyntax, operation.Type,
                                                                          constantValue: default, isImplicit: true);
                    }
                }
            }

            if (convertedTestExpression == null)
            {
                convertedTestExpression = MakeInvalidOperation(operation.Type, capturedValue);
            }

            return convertedTestExpression;
        }

        public override IOperation VisitCoalesce(ICoalesceOperation operation, int? captureIdForResult)
        {
            var whenNull = new BasicBlock(BasicBlockKind.Block);
            IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(operation, whenNull);

            int resultCaptureId = captureIdForResult ?? _availableCaptureId++;

            AddStatement(new FlowCapture(resultCaptureId, operation.Value.Syntax, convertedTestExpression));

            var afterCoalesce = new BasicBlock(BasicBlockKind.Block);
            LinkBlocks(CurrentBasicBlock, afterCoalesce);
            _currentBasicBlock = null;

            AppendNewBlock(whenNull);

            VisitAndCapture(operation.WhenNull, resultCaptureId);

            AppendNewBlock(afterCoalesce);

            return new FlowCaptureReference(resultCaptureId, operation.Syntax, operation.Type, operation.ConstantValue);
        }

        private static BasicBlock.Branch RegularBranch(BasicBlock destination)
        {
            return new BasicBlock.Branch() { Destination = destination, Kind = BasicBlock.BranchKind.Regular };
        }

        private static IOperation MakeInvalidOperation(ITypeSymbol type, IOperation child)
        {
            return new InvalidOperation(ImmutableArray.Create<IOperation>(child),
                                        semanticModel: null, child.Syntax, type,
                                        constantValue: default, isImplicit: true);
        }

        private static IsNullOperation MakeIsNullOperation(IOperation operand, Compilation compilation)
        {
            Optional<object> constantValue = operand.ConstantValue;
            return new IsNullOperation(operand.Syntax, operand,
                                       compilation.GetSpecialType(SpecialType.System_Boolean),
                                       constantValue.HasValue ? new Optional<object>(constantValue.Value == null) : default);
        }

        private static IOperation TryUnwrapNullableValue(IOperation value, Compilation compilation)
        {
            ITypeSymbol valueType = value.Type;

            Debug.Assert(ITypeSymbolHelpers.IsNullableType(valueType));

            var method = (IMethodSymbol)compilation.CommonGetSpecialTypeMember(SpecialMember.System_Nullable_T_GetValueOrDefault);

            if (method != null)
            {
                foreach (ISymbol candidate in valueType.GetMembers(method.Name))
                {
                    if (candidate.OriginalDefinition.Equals(method))
                    {
                        method = (IMethodSymbol)candidate;
                        return new InvocationExpression(method, value, isVirtual: false,
                                                        ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, value.Syntax,
                                                        method.ReturnType, constantValue: default, isImplicit: true);
                    }
                }
            }

            return null;
        }

        public override IOperation VisitConditionalAccess(IConditionalAccessOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): Consider to avoid nullable wrap/unwrap operations by merging conditional access
            //                      with containing:
            //                      - binary operator
            //                      - coalesce expression
            //                      - nullable conversion
            //                      - etc. see references to UpdateConditionalAccess in local rewriter

            SpillEvalStack();

            var whenNull = new BasicBlock(BasicBlockKind.Block);

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            IConditionalAccessOperation currentConditionalAccess = operation;
            IOperation testExpression;

            while (true)
            {
                testExpression = currentConditionalAccess.Operation;
                SyntaxNode testExpressionSyntax = testExpression.Syntax;
                ITypeSymbol testExpressionType = testExpression.Type;

                int testExpressionCaptureId = VisitAndCapture(testExpression);
                Optional<object> constantValue = testExpression.ConstantValue;

                LinkBlocks(CurrentBasicBlock,
                           (Operation.SetParentOperation(MakeIsNullOperation(new FlowCaptureReference(testExpressionCaptureId, testExpressionSyntax, testExpressionType, constantValue),
                                                                             compilation),
                                                         null),
                            true,
                            RegularBranch(whenNull)));
                _currentBasicBlock = null;

                IOperation receiver = new FlowCaptureReference(testExpressionCaptureId, testExpressionSyntax, testExpressionType, constantValue);

                if (ITypeSymbolHelpers.IsNullableType(testExpressionType))
                {
                    receiver = TryUnwrapNullableValue(receiver, compilation) ??
                               // PROTOTYPE(dataflow): The scenario with missing GetValueOrDefault is not covered by unit-tests.
                               MakeInvalidOperation(((INamedTypeSymbol)testExpressionType).TypeArguments[0], receiver);
                }

                Debug.Assert(_currentConditionalAccessInstance == null);
                _currentConditionalAccessInstance = receiver;

                if (currentConditionalAccess.WhenNotNull.Kind != OperationKind.ConditionalAccess)
                {
                    break;
                }

                currentConditionalAccess = (IConditionalAccessOperation)currentConditionalAccess.WhenNotNull;
            }

            // Avoid creation of default values and FlowCapture for conditional access on a statement level.
            if (_currentStatement == operation ||
                (_currentStatement == operation.Parent && _currentStatement?.Kind == OperationKind.ExpressionStatement))
            {
                Debug.Assert(captureIdForResult == null);

                IOperation result = Visit(currentConditionalAccess.WhenNotNull);
                Debug.Assert(_currentConditionalAccessInstance == null);

                if (_currentStatement != operation)
                {
                    var expressionStatement = (IExpressionStatementOperation)_currentStatement;
                    result = new ExpressionStatement(result, semanticModel: null, expressionStatement.Syntax,
                                                     expressionStatement.Type, expressionStatement.ConstantValue,
                                                     expressionStatement.IsImplicit);
                }

                AddStatement(result);
                AppendNewBlock(whenNull);
                return null;
            }
            else
            {
                int resultCaptureId = captureIdForResult ?? _availableCaptureId++;

                if (ITypeSymbolHelpers.IsNullableType(operation.Type) && !ITypeSymbolHelpers.IsNullableType(currentConditionalAccess.WhenNotNull.Type))
                {
                    IOperation access = Visit(currentConditionalAccess.WhenNotNull);
                    AddStatement(new FlowCapture(resultCaptureId, currentConditionalAccess.WhenNotNull.Syntax,
                                                 TryMakeNullableValue((INamedTypeSymbol)operation.Type, access, compilation) ??
                                                 // PROTOTYPE(dataflow): The scenario with missing constructor is not covered by unit-tests.
                                                 MakeInvalidOperation(operation.Type, access)));
                }
                else
                {
                    VisitAndCapture(currentConditionalAccess.WhenNotNull, resultCaptureId);
                }

                Debug.Assert(_currentConditionalAccessInstance == null);

                var afterAccess = new BasicBlock(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterAccess);
                _currentBasicBlock = null;

                AppendNewBlock(whenNull);

                SyntaxNode defaultValueSyntax = (operation.Operation == testExpression ? testExpression : operation).Syntax;

                AddStatement(new FlowCapture(resultCaptureId,
                                             defaultValueSyntax,
                                             new DefaultValueExpression(semanticModel: null, defaultValueSyntax, operation.Type,
                                                                        (operation.Type.IsReferenceType && !ITypeSymbolHelpers.IsNullableType(operation.Type)) ?
                                                                            new Optional<object>(null) : default,
                                                                        isImplicit: true)));

                AppendNewBlock(afterAccess);

                return new FlowCaptureReference(resultCaptureId, operation.Syntax, operation.Type, operation.ConstantValue);
            }
        }

        private static IOperation TryMakeNullableValue(INamedTypeSymbol type, IOperation underlyingValue, Compilation compilation)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(type));

            var method = (IMethodSymbol)compilation.CommonGetSpecialTypeMember(SpecialMember.System_Nullable_T__ctor);

            if (method != null)
            {
                foreach (ISymbol candidate in type.InstanceConstructors)
                {
                    if (candidate.OriginalDefinition.Equals(method))
                    {
                        method = (IMethodSymbol)candidate;
                        return new ObjectCreationExpression(method, initializer: null,
                                                            ImmutableArray.Create<IArgumentOperation>(
                                                                        new ArgumentOperation(underlyingValue,
                                                                                              ArgumentKind.Explicit,
                                                                                              method.Parameters[0],
                                                                                              inConversionOpt: null,
                                                                                              outConversionOpt: null,
                                                                                              semanticModel: null,
                                                                                              underlyingValue.Syntax,
                                                                                              isImplicit: true)),
                                                            semanticModel: null,
                                                            underlyingValue.Syntax,
                                                            type,
                                                            constantValue: default,
                                                            isImplicit: true);
                    }
                }
            }

            return null;
        }

        public override IOperation VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentConditionalAccessInstance != null);
            IOperation result = _currentConditionalAccessInstance;
            _currentConditionalAccessInstance = null;
            return result;
        }

        public override IOperation VisitExpressionStatement(IExpressionStatementOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            IOperation underlying = Visit(operation.Operation);

            if (underlying == null)
            {
                Debug.Assert(operation.Operation.Kind == OperationKind.ConditionalAccess);
                return null;
            }

            return new ExpressionStatement(underlying, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            RegionBuilder locals = null;
            bool haveLocals = !operation.Locals.IsEmpty;
            if (haveLocals)
            {
                locals = new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.Locals);
            }

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            var @break = GetLabeledOrNewBlock(operation.ExitLabel);

            if (operation.ConditionIsTop)
            {
                // while (condition) 
                //   body;
                //
                // becomes
                //
                // continue:
                // {
                //     GotoIfFalse condition break;
                //     body
                //     goto continue;
                // }
                // break:

                AppendNewBlock(@continue);

                if (haveLocals)
                {
                    EnterRegion(locals);
                }

                VisitConditionalBranch(operation.Condition, ref @break, sense: operation.ConditionIsUntil);

                VisitStatement(operation.Body);
                LinkBlocks(CurrentBasicBlock, @continue);
            }
            else
            {
                // do
                //   body
                // while (condition);
                //
                // becomes
                //
                // start: 
                // {
                //   body
                //   continue:
                //   GotoIfTrue condition start;
                // }
                // break:

                var start = new BasicBlock(BasicBlockKind.Block);
                AppendNewBlock(start);

                if (haveLocals)
                {
                    EnterRegion(locals);
                }

                VisitStatement(operation.Body);

                AppendNewBlock(@continue);

                VisitConditionalBranch(operation.Condition, ref start, sense: !operation.ConditionIsUntil);
            }

            if (haveLocals)
            {
                Debug.Assert(_currentRegion == locals);
                LeaveRegion();
            }

            AppendNewBlock(@break);
            return null;
        }

        public override IOperation VisitTry(ITryOperation operation, int? captureIdForResult)
        {
            if (operation.Catches.IsEmpty && operation.Finally == null)
            {
                // Malformed node without handlers
                throw ExceptionUtilities.Unreachable;
            }

            RegionBuilder tryAndFinallyRegion = null;
            bool haveFinally = operation.Finally != null;
            if (haveFinally)
            {
                tryAndFinallyRegion = new RegionBuilder(ControlFlowGraph.RegionKind.TryAndFinally);
                EnterRegion(tryAndFinallyRegion);
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Try));
            }

            bool haveCatches = !operation.Catches.IsEmpty;
            if (haveCatches)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.TryAndCatch));
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Try));
            }

            var afterTryCatchFinally = GetLabeledOrNewBlock(operation.ExitLabel);

            VisitStatement(operation.Body);
            LinkBlocks(CurrentBasicBlock, afterTryCatchFinally);

            if (haveCatches)
            {
                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.Try);
                LeaveRegion();

                foreach (ICatchClauseOperation catchClause in operation.Catches)
                {
                    RegionBuilder filterAndHandlerRegion = null;

                    IOperation exceptionDeclarationOrExpression = catchClause.ExceptionDeclarationOrExpression;
                    IOperation filter = catchClause.Filter;
                    bool haveFilter = filter != null;
                    var catchBlock = new BasicBlock(BasicBlockKind.Block);

                    if (haveFilter)
                    {
                        filterAndHandlerRegion = new RegionBuilder(ControlFlowGraph.RegionKind.FilterAndHandler, catchClause.ExceptionType, catchClause.Locals);
                        EnterRegion(filterAndHandlerRegion);

                        var filterRegion = new RegionBuilder(ControlFlowGraph.RegionKind.Filter, catchClause.ExceptionType);
                        EnterRegion(filterRegion);

                        AddExceptionStore(catchClause.ExceptionType, exceptionDeclarationOrExpression);

                        VisitConditionalBranch(filter, ref catchBlock, sense: true);
                        var continueDispatchBlock = new BasicBlock(BasicBlockKind.Block);
                        AppendNewBlock(continueDispatchBlock);
                        continueDispatchBlock.InternalNext.Branch.Kind = BasicBlock.BranchKind.StructuredExceptionHandling;
                        LeaveRegion();

                        Debug.Assert(filterRegion.LastBlock.InternalNext.Branch.Destination == null);
                        Debug.Assert(filterRegion.FirstBlock.Predecessors.IsEmpty);
                    }

                    var handlerRegion = new RegionBuilder(ControlFlowGraph.RegionKind.Catch, catchClause.ExceptionType,
                                                          haveFilter ? default : catchClause.Locals);
                    EnterRegion(handlerRegion);

                    AppendNewBlock(catchBlock, linkToPrevious: false);

                    if (!haveFilter)
                    {
                        AddExceptionStore(catchClause.ExceptionType, exceptionDeclarationOrExpression);
                    }

                    VisitStatement(catchClause.Handler);
                    LinkBlocks(CurrentBasicBlock, afterTryCatchFinally);

                    LeaveRegion();

                    if (haveFilter)
                    {
                        Debug.Assert(_currentRegion == filterAndHandlerRegion);
                        LeaveRegion();
                        Debug.Assert(filterAndHandlerRegion.Regions[0].LastBlock.InternalNext.Branch.Destination == null);
                        Debug.Assert(handlerRegion.FirstBlock.Predecessors.All(p => filterAndHandlerRegion.Regions[0].FirstBlock.Ordinal <= p.Ordinal &&
                                                                                    filterAndHandlerRegion.Regions[0].LastBlock.Ordinal >= p.Ordinal));
                    }
                    else
                    {
                        Debug.Assert(handlerRegion.FirstBlock.Predecessors.IsEmpty);
                    }
                }

                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.TryAndCatch);
                LeaveRegion();
            }

            if (haveFinally)
            {
                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.Try);
                LeaveRegion();

                var finallyRegion = new RegionBuilder(ControlFlowGraph.RegionKind.Finally);
                EnterRegion(finallyRegion);
                AppendNewBlock(new BasicBlock(BasicBlockKind.Block));
                VisitStatement(operation.Finally);
                var continueDispatchBlock = new BasicBlock(BasicBlockKind.Block);
                AppendNewBlock(continueDispatchBlock);
                continueDispatchBlock.InternalNext.Branch.Kind = BasicBlock.BranchKind.StructuredExceptionHandling;
                LeaveRegion();
                Debug.Assert(_currentRegion == tryAndFinallyRegion);
                LeaveRegion();
                Debug.Assert(finallyRegion.LastBlock.InternalNext.Branch.Destination == null);
                Debug.Assert(finallyRegion.FirstBlock.Predecessors.IsEmpty);
            }

            AppendNewBlock(afterTryCatchFinally, linkToPrevious: false);
            Debug.Assert(tryAndFinallyRegion?.Regions[1].LastBlock.InternalNext.Branch.Destination == null);

            return null;
        }

        private void AddExceptionStore(ITypeSymbol exceptionType, IOperation exceptionDeclarationOrExpression)
        {
            if (exceptionDeclarationOrExpression != null)
            {
                IOperation exceptionTarget;
                SyntaxNode syntax = exceptionDeclarationOrExpression.Syntax;
                if (exceptionDeclarationOrExpression.Kind == OperationKind.VariableDeclarator)
                {
                    ILocalSymbol local = ((IVariableDeclaratorOperation)exceptionDeclarationOrExpression).Symbol;
                    exceptionTarget = new LocalReferenceExpression(local,
                                                                  isDeclaration: true,
                                                                  semanticModel: null,
                                                                  syntax,
                                                                  local.Type,
                                                                  constantValue: default,
                                                                  isImplicit: true);
                }
                else
                {
                    exceptionTarget = Visit(exceptionDeclarationOrExpression);
                }

                if (exceptionTarget != null)
                {
                    AddStatement(new SimpleAssignmentExpression(
                                         exceptionTarget,
                                         isRef: false,
                                         new CaughtExceptionOperation(syntax, exceptionType),
                                         semanticModel: null,
                                         syntax,
                                         type: null,
                                         constantValue: default,
                                         isImplicit: true));
                }
            }
        }

        public override IOperation VisitCatchClause(ICatchClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitReturn(IReturnOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            IOperation returnedValue = Visit(operation.ReturnedValue);

            switch (operation.Kind)
            {
                case OperationKind.YieldReturn:
                    AddStatement(new ReturnStatement(OperationKind.YieldReturn, returnedValue, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit));
                    break;

                case OperationKind.YieldBreak:
                case OperationKind.Return:
                    BasicBlock current = CurrentBasicBlock;
                    LinkBlocks(CurrentBasicBlock, _exit, returnedValue is null ? BasicBlock.BranchKind.Regular : BasicBlock.BranchKind.Return);
                    current.InternalNext.Value = Operation.SetParentOperation(returnedValue, null);
                    _currentBasicBlock = null;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(operation.Kind);
            }

            return null;
        }

        public override IOperation VisitLabeled(ILabeledOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            AppendNewBlock(GetLabeledOrNewBlock(operation.Label));
            VisitStatement(operation.Operation);
            return null;
        }

        private BasicBlock GetLabeledOrNewBlock(ILabelSymbol labelOpt)
        {
            if (labelOpt == null)
            {
                return new BasicBlock(BasicBlockKind.Block);
            }

            BasicBlock labeledBlock;

            if (_labeledBlocks == null)
            {
                _labeledBlocks = PooledDictionary<ILabelSymbol, BasicBlock>.GetInstance();
            }
            else if (_labeledBlocks.TryGetValue(labelOpt, out labeledBlock))
            {
                return labeledBlock;
            }

            labeledBlock = new BasicBlock(BasicBlockKind.Block);
            _labeledBlocks.Add(labelOpt, labeledBlock);
            return labeledBlock;
        }

        public override IOperation VisitBranch(IBranchOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            LinkBlocks(CurrentBasicBlock, GetLabeledOrNewBlock(operation.Target));
            _currentBasicBlock = null;
            return null;
        }

        public override IOperation VisitEmpty(IEmptyOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            return null;
        }

        public override IOperation VisitThrow(IThrowOperation operation, int? captureIdForResult)
        {
            bool isStatement = (_currentStatement == operation);

            if (!isStatement)
            {
                SpillEvalStack();
            }

            IOperation exception = Operation.SetParentOperation(Visit(operation.Exception), null);

            BasicBlock current = CurrentBasicBlock;
            AppendNewBlock(new BasicBlock(BasicBlockKind.Block), linkToPrevious: false);
            Debug.Assert(current.InternalNext.Value == null);
            Debug.Assert(current.InternalNext.Branch.Destination == null);
            Debug.Assert(current.InternalNext.Branch.Kind == BasicBlock.BranchKind.None);
            current.InternalNext.Value = exception;
            current.InternalNext.Branch.Kind = operation.Exception == null ? BasicBlock.BranchKind.ReThrow : BasicBlock.BranchKind.Throw;

            if (isStatement)
            {
                return null;
            }
            else
            {
                return Operation.CreateOperationNone(semanticModel: null, operation.Syntax, constantValue: default, children: ImmutableArray<IOperation>.Empty, isImplicit: true);
            }
        }

        public override IOperation VisitUsing(IUsingOperation operation, int? captureIdForResult)
        {
            Debug.Assert(operation == _currentStatement);

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            ITypeSymbol iDisposable = compilation.GetSpecialType(SpecialType.System_IDisposable);

            if (operation.Resources.Kind == OperationKind.VariableDeclarationGroup)
            {
                var declarationGroup = (IVariableDeclarationGroupOperation)operation.Resources;
                var resourceQueue = ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)>.GetInstance(declarationGroup.Declarations.Length);

                // PROTOTYPE(dataflow): Once https://github.com/dotnet/roslyn/issues/25825 is fixed
                //                      we should switch to IUsingOperation.Locals property
                //                      because the current approach doesn't handle 'out vars' and the like.
                var locals = ArrayBuilder<ILocalSymbol>.GetInstance(declarationGroup.Declarations.Length);

                foreach (IVariableDeclarationOperation declaration in declarationGroup.Declarations)
                {
                    foreach (IVariableDeclaratorOperation declarator in declaration.Declarators)
                    {
                        locals.Add(declarator.Symbol);
                        resourceQueue.Add((declaration, declarator));
                    }
                }

                resourceQueue.ReverseContents();
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: locals.ToImmutableAndFree()));

                processQueue(resourceQueue);

                LeaveRegion();
            }
            else
            {
                Debug.Assert(operation.Resources.Kind != OperationKind.VariableDeclaration);
                Debug.Assert(operation.Resources.Kind != OperationKind.VariableDeclarator);

                // PROTOTYPE(dataflow): Once https://github.com/dotnet/roslyn/issues/25825 is fixed
                //                      we should handle locals in IUsingOperation.Locals property:
                //                      'out vars' and the like.
                IOperation resource = Visit(operation.Resources);
                int captureId = _availableCaptureId++;

                if (shouldConvertToIDisposableBeforeTry(resource))
                {
                    resource = ConvertToIDisposable(resource, iDisposable);
                }

                AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                processResource(new FlowCaptureReference(captureId, resource.Syntax, resource.Type, constantValue: default), resourceQueueOpt: null);
            }

            return null;

            void processQueue(ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)> resourceQueueOpt)
            {
                if (resourceQueueOpt == null || resourceQueueOpt.Count == 0)
                {
                    VisitStatement(operation.Body);
                }
                else
                {
                    (IVariableDeclarationOperation declaration, IVariableDeclaratorOperation declarator) = resourceQueueOpt.Pop();
                    HandleVariableDeclarator(declaration, declarator);
                    ILocalSymbol localSymbol = declarator.Symbol;
                    processResource(new LocalReferenceExpression(localSymbol, isDeclaration: false, semanticModel: null, declarator.Syntax, localSymbol.Type,
                                                                 constantValue: default, isImplicit: true),
                                    resourceQueueOpt);
                }
            }

            bool shouldConvertToIDisposableBeforeTry(IOperation resource)
            {
                return resource.Type == null || resource.Type.Kind == SymbolKind.DynamicType;
            }

            void processResource(IOperation resource, ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)> resourceQueueOpt)
            {
                // When ResourceType is a non-nullable value type, the expansion is:
                // 
                // { 
                //   ResourceType resource = expr; 
                //   try { statement; } 
                //   finally { ((IDisposable)resource).Dispose(); }
                // }
                // 
                // Otherwise, when Resource type is a nullable value type or
                // a reference type other than dynamic, the expansion is:
                // 
                // { 
                //   ResourceType resource = expr; 
                //   try { statement; } 
                //   finally { if (resource != null) ((IDisposable)resource).Dispose(); }
                // }
                // 
                // Otherwise, when ResourceType is dynamic, the expansion is:
                // { 
                //   dynamic resource = expr; 
                //   IDisposable d = (IDisposable)resource;
                //   try { statement; } 
                //   finally { if (d != null) d.Dispose(); }
                // }

                if (shouldConvertToIDisposableBeforeTry(resource))
                {
                    resource = ConvertToIDisposable(resource, iDisposable);
                    int captureId = _availableCaptureId++;
                    AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                    resource = new FlowCaptureReference(captureId, resource.Syntax, resource.Type, constantValue: default);
                }

                var afterTryFinally = new BasicBlock(BasicBlockKind.Block);

                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Try));

                processQueue(resourceQueueOpt);

                LinkBlocks(CurrentBasicBlock, afterTryFinally);

                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.Try);
                LeaveRegion();

                AddDisposingFinally(resource, knownToImplementIDisposable: true, iDisposable, compilation);

                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.TryAndFinally);
                LeaveRegion();

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }
        }

        private void AddDisposingFinally(IOperation resource, bool knownToImplementIDisposable, ITypeSymbol iDisposable, Compilation compilation)
        {
            Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.TryAndFinally);

            var endOfFinally = new BasicBlock(BasicBlockKind.Block);
            endOfFinally.InternalNext.Branch.Kind = BasicBlock.BranchKind.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Finally));
            AppendNewBlock(new BasicBlock(BasicBlockKind.Block));

            if (!knownToImplementIDisposable)
            {
                Debug.Assert(!isNotNullableValueType(resource.Type));
                resource = ConvertToIDisposable(resource, iDisposable, isTryCast: true);
                int captureId = _availableCaptureId++;
                AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                resource = new FlowCaptureReference(captureId, resource.Syntax, iDisposable, constantValue: default);
            }

            if (!knownToImplementIDisposable || !isNotNullableValueType(resource.Type))
            {
                IOperation condition = MakeIsNullOperation(OperationCloner.CloneOperation(resource), compilation);
                condition = Operation.SetParentOperation(condition, null);
                LinkBlocks(CurrentBasicBlock, (condition, JumpIfTrue: true, RegularBranch(endOfFinally)));
                _currentBasicBlock = null;
            }

            if (!resource.Type.Equals(iDisposable))
            {
                resource = ConvertToIDisposable(resource, iDisposable);
            }

            AddStatement(tryDispose(resource) ??
                         // PROTOTYPE(dataflow): The scenario with missing Dispose is not covered by unit-tests.
                         MakeInvalidOperation(type: null, resource));

            AppendNewBlock(endOfFinally);

            LeaveRegion();
            return;

            IOperation tryDispose(IOperation value)
            {
                Debug.Assert(value.Type == iDisposable);

                var method = (IMethodSymbol)compilation.CommonGetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose);
                if (method != null)
                {
                    return new InvocationExpression(method, value, isVirtual: true,
                                                    ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, value.Syntax,
                                                    method.ReturnType, constantValue: default, isImplicit: true);
                }

                return null;
            }

            bool isNotNullableValueType(ITypeSymbol type)
            {
                return type?.IsValueType == true && !ITypeSymbolHelpers.IsNullableType(type);
            }
        }

        private static IOperation ConvertToIDisposable(IOperation operand, ITypeSymbol iDisposable, bool isTryCast = false)
        {
            Debug.Assert(iDisposable.SpecialType == SpecialType.System_IDisposable);
            return new ConversionOperation(operand, ConvertibleConversion.Instance, isTryCast, isChecked: false,
                                           semanticModel: null, operand.Syntax, iDisposable, constantValue: default, isImplicit: true);
        }

        public override IOperation VisitLock(ILockOperation operation, int? captureIdForResult)
        {
            Debug.Assert(operation == _currentStatement);

            SemanticModel semanticModel = ((Operation)operation).SemanticModel;
            ITypeSymbol objectType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);

            // If Monitor.Enter(object, ref bool) is available:
            //
            // L $lock = `LockedValue`;  
            // bool $lockTaken = false;                   
            // try
            // {
            //     Monitor.Enter($lock, ref $lockTaken);
            //     `body`                               
            // }
            // finally
            // {                                        
            //     if ($lockTaken) Monitor.Exit($lock);   
            // }

            // If Monitor.Enter(object, ref bool) is not available:
            //
            // L $lock = `LockedValue`;
            // Monitor.Enter($lock);           // NB: before try-finally so we don't Exit if an exception prevents us from acquiring the lock.
            // try 
            // {
            //     `body`
            // } 
            // finally 
            // {
            //     Monitor.Exit($lock); 
            // }

            // If original type of the LockedValue object is System.Object, VB calls runtime helper (if one is available)
            // Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType to ensure no value type is 
            // used. 
            // For simplicity, we will not synthesize this call because its presence is unlikely to affect graph analysis.

            IOperation lockedValue = Visit(operation.LockedValue);

            if (!objectType.Equals(lockedValue.Type))
            {
                lockedValue = new ConversionOperation(lockedValue, ConvertibleConversion.Instance, isTryCast: false, isChecked: false,
                                                      semanticModel: null, lockedValue.Syntax, objectType, constantValue: default, isImplicit: true);
            }

            int captureId = _availableCaptureId++;
            AddStatement(new FlowCapture(captureId, lockedValue.Syntax, lockedValue));
            lockedValue = new FlowCaptureReference(captureId, lockedValue.Syntax, lockedValue.Type, constantValue: default);

            var enterMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2);
            bool legacyMode = (enterMethod == null);

            if (legacyMode)
            {
                enterMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter);

                // Monitor.Enter($lock);
                if (enterMethod == null)
                {
                    // PROTOTYPE(dataflow): The scenario with missing Enter is not covered by unit-tests.
                    AddStatement(MakeInvalidOperation(type: null, lockedValue));
                }
                else
                {
                    AddStatement(new InvocationExpression(enterMethod, instance: null, isVirtual: false,
                                                          ImmutableArray.Create<IArgumentOperation>(
                                                                    new ArgumentOperation(lockedValue,
                                                                                          ArgumentKind.Explicit,
                                                                                          enterMethod.Parameters[0],
                                                                                          inConversionOpt: null,
                                                                                          outConversionOpt: null,
                                                                                          semanticModel: null,
                                                                                          lockedValue.Syntax,
                                                                                          isImplicit: true)),
                                                          semanticModel: null, lockedValue.Syntax,
                                                          enterMethod.ReturnType, constantValue: default, isImplicit: true));
                }
            }

            var afterTryFinally = new BasicBlock(BasicBlockKind.Block);

            EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.TryAndFinally));
            EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Try));

            IOperation lockTaken = null;
            if (!legacyMode)
            {
                // Monitor.Enter($lock, ref $lockTaken);
                lockTaken = new FlowCaptureReference(_availableCaptureId++, lockedValue.Syntax, semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean), constantValue: default);
                AddStatement(new InvocationExpression(enterMethod, instance: null, isVirtual: false,
                                                      ImmutableArray.Create<IArgumentOperation>(
                                                                new ArgumentOperation(lockedValue,
                                                                                      ArgumentKind.Explicit,
                                                                                      enterMethod.Parameters[0],
                                                                                      inConversionOpt: null,
                                                                                      outConversionOpt: null,
                                                                                      semanticModel: null,
                                                                                      lockedValue.Syntax,
                                                                                      isImplicit: true),
                                                                new ArgumentOperation(lockTaken,
                                                                                      ArgumentKind.Explicit,
                                                                                      enterMethod.Parameters[1],
                                                                                      inConversionOpt: null,
                                                                                      outConversionOpt: null,
                                                                                      semanticModel: null,
                                                                                      lockedValue.Syntax,
                                                                                      isImplicit: true)),
                                                      semanticModel: null, lockedValue.Syntax,
                                                      enterMethod.ReturnType, constantValue: default, isImplicit: true));
            }

            VisitStatement(operation.Body);

            LinkBlocks(CurrentBasicBlock, afterTryFinally);

            Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.Try);
            LeaveRegion();

            var endOfFinally = new BasicBlock(BasicBlockKind.Block);
            endOfFinally.InternalNext.Branch.Kind = BasicBlock.BranchKind.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Finally));
            AppendNewBlock(new BasicBlock(BasicBlockKind.Block));

            if (!legacyMode)
            {
                // if ($lockTaken)
                IOperation condition = OperationCloner.CloneOperation(lockTaken);
                condition = Operation.SetParentOperation(condition, null);
                LinkBlocks(CurrentBasicBlock, (condition, JumpIfTrue: false, RegularBranch(endOfFinally)));
                _currentBasicBlock = null;
            }

            // Monitor.Exit($lock);
            var exitMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Exit);
            lockedValue = OperationCloner.CloneOperation(lockedValue);

            if (exitMethod == null)
            {
                // PROTOTYPE(dataflow): The scenario with missing Exit is not covered by unit-tests.
                AddStatement(MakeInvalidOperation(type: null, lockedValue));
            }
            else
            {
                AddStatement(new InvocationExpression(exitMethod, instance: null, isVirtual: false,
                                                      ImmutableArray.Create<IArgumentOperation>(
                                                                new ArgumentOperation(lockedValue,
                                                                                      ArgumentKind.Explicit,
                                                                                      exitMethod.Parameters[0],
                                                                                      inConversionOpt: null,
                                                                                      outConversionOpt: null,
                                                                                      semanticModel: null,
                                                                                      lockedValue.Syntax,
                                                                                      isImplicit: true)),
                                                      semanticModel: null, lockedValue.Syntax,
                                                      exitMethod.ReturnType, constantValue: default, isImplicit: true));
            }

            AppendNewBlock(endOfFinally);

            LeaveRegion();
            Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.TryAndFinally);
            LeaveRegion();

            AppendNewBlock(afterTryFinally, linkToPrevious: false);

            return null;
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            ForEachLoopOperationInfo info = ((BaseForEachLoopStatement)operation).Info;

            bool haveLocals = !operation.Locals.IsEmpty;
            bool createdRegionForCollection = false;

            if (haveLocals && operation.LoopControlVariable.Kind == OperationKind.VariableDeclarator)
            {
                // VB has rather interesting scoping rules for control variable.
                // It is in scope in the collection expression. However, it is considered to be 
                // "a different" version of that local. Effectively when the code is emitted,
                // there are two distinct locals, one is used in the collection expression and the 
                // other is used as a loop control variable. This is done to have proper hoisting 
                // and lifetime in presence of lambdas.
                // Rather than introducing a separate local symbol, we will simply add another 
                // lifetime region for that local around the collection expression. 

                var declarator = (IVariableDeclaratorOperation)operation.LoopControlVariable;
                ILocalSymbol local = declarator.Symbol;

                foreach (IOperation op in operation.Collection.DescendantsAndSelf())
                {
                    if (op is ILocalReferenceOperation l && l.Local.Equals(local))
                    {
                        EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: ImmutableArray.Create(local)));
                        createdRegionForCollection = true;
                        break;
                    }
                }
            }

            IOperation enumerator = getEnumerator();

            if (createdRegionForCollection)
            {
                LeaveRegion();
            }

            if (info.NeedsDispose)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Try));
            }

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            var @break = GetLabeledOrNewBlock(operation.ExitLabel);

            AppendNewBlock(@continue);

            IOperation condition = Operation.SetParentOperation(getCondition(enumerator), null);
            LinkBlocks(CurrentBasicBlock, (condition, JumpIfTrue: false, RegularBranch(@break)));
            _currentBasicBlock = null;

            if (haveLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.Locals));
            }

            AddStatement(getLoopControlVariableAssignment(applyConversion(info.CurrentConversion, getCurrent(OperationCloner.CloneOperation(enumerator)), info.ElementType)));
            VisitStatement(operation.Body);
            LinkBlocks(CurrentBasicBlock, @continue);

            if (haveLocals)
            {
                LeaveRegion();
            }

            AppendNewBlock(@break);

            if (info.NeedsDispose)
            {
                var afterTryFinally = new BasicBlock(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterTryFinally);

                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.Try);
                LeaveRegion();

                Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
                AddDisposingFinally(OperationCloner.CloneOperation(enumerator),
                                    info.KnownToImplementIDisposable,
                                    compilation.GetSpecialType(SpecialType.System_IDisposable),
                                    compilation);

                Debug.Assert(_currentRegion.Kind == ControlFlowGraph.RegionKind.TryAndFinally);
                LeaveRegion();

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }

            return null;

            IOperation applyConversion(IConvertibleConversion conversionOpt, IOperation operand, ITypeSymbol targetType)
            {
                if (conversionOpt?.ToCommonConversion().IsIdentity == false)
                {
                    operand = new ConversionOperation(operand, conversionOpt, isTryCast: false, isChecked: false, semanticModel: null,
                                                      operand.Syntax, targetType, constantValue: default, isImplicit: true);
                }

                return operand;
            }

            IOperation getEnumerator()
            {
                if (info.GetEnumeratorMethod != null)
                {
                    IOperation invocation = makeInvocation(operation.Collection.Syntax,
                                                           info.GetEnumeratorMethod,
                                                           info.GetEnumeratorMethod.IsStatic ? null : Visit(operation.Collection),
                                                           info.GetEnumeratorArguments);

                    int enumeratorCaptureId = _availableCaptureId++;
                    AddStatement(new FlowCapture(enumeratorCaptureId, operation.Collection.Syntax, invocation));

                    return new FlowCaptureReference(enumeratorCaptureId, operation.Collection.Syntax, info.GetEnumeratorMethod.ReturnType, constantValue: default);
                }
                else
                {
                    // This must be an error case
                    AddStatement(MakeInvalidOperation(type: null, Visit(operation.Collection)));
                    return new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Collection.Syntax, 
                                                type: null,constantValue: default, isImplicit: true);
                }
            }

            IOperation getCondition(IOperation enumeratorRef)
            {
                if (info.MoveNextMethod != null)
                {
                    return makeInvocationDroppingInstanceForStaticMethods(info.MoveNextMethod, enumeratorRef, info.MoveNextArguments);
                }
                else
                {
                    // This must be an error case
                    return MakeInvalidOperation(((Operation)operation).SemanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean), enumeratorRef);
                }
            }

            IOperation getCurrent(IOperation enumeratorRef)
            {
                if (info.CurrentProperty != null)
                {
                    return new PropertyReferenceExpression(info.CurrentProperty,
                                                           info.CurrentProperty.IsStatic ? null : enumeratorRef,
                                                           makeArguments(info.CurrentArguments), semanticModel: null,
                                                           operation.LoopControlVariable.Syntax,
                                                           info.CurrentProperty.Type, constantValue: default, isImplicit: true);
                }
                else
                {
                    // This must be an error case
                    return MakeInvalidOperation(type: null, enumeratorRef);
                }
            }

            IOperation getLoopControlVariableAssignment(IOperation current)
            {
                switch (operation.LoopControlVariable.Kind)
                {
                    case OperationKind.VariableDeclarator:
                        var declarator = (IVariableDeclaratorOperation)operation.LoopControlVariable;
                        ILocalSymbol local = declarator.Symbol;
                        current = applyConversion(info.ElementConversion, current, local.Type);

                        return new SimpleAssignmentExpression(new LocalReferenceExpression(local, isDeclaration: true, semanticModel: null,
                                                                                           declarator.Syntax, local.Type, constantValue: default, isImplicit: true),
                                                              isRef: local.RefKind != RefKind.None, current, semanticModel: null, declarator.Syntax, type: null,
                                                              constantValue: default, isImplicit: true);

                    case OperationKind.Tuple:
                    case OperationKind.DeclarationExpression:
                        Debug.Assert(info.ElementConversion?.ToCommonConversion().IsIdentity != false);

                        return new DeconstructionAssignmentExpression(VisitPreservingTupleOperations(operation.LoopControlVariable), 
                                                                      current, semanticModel: null,
                                                                      operation.LoopControlVariable.Syntax, operation.LoopControlVariable.Type,
                                                                      constantValue: default, isImplicit: true);
                    default:
                        return new SimpleAssignmentExpression(Visit(operation.LoopControlVariable), 
                                                              isRef: false, // In C# this is an error case and VB doesn't support ref locals
                                                              current, semanticModel: null, operation.LoopControlVariable.Syntax,
                                                              operation.LoopControlVariable.Type,
                                                              constantValue: default, isImplicit: true);
                }
            }

            InvocationExpression makeInvocationDroppingInstanceForStaticMethods(IMethodSymbol method, IOperation instance, Lazy<ImmutableArray<IArgumentOperation>> arguments)
            {
                return makeInvocation(instance.Syntax, method, method.IsStatic ? null : instance, arguments);
            }

            InvocationExpression makeInvocation(SyntaxNode syntax, IMethodSymbol method, IOperation instanceOpt, Lazy<ImmutableArray<IArgumentOperation>> arguments)
            {
                Debug.Assert(method.IsStatic == (instanceOpt == null));
                return new InvocationExpression(method, instanceOpt,
                                                isVirtual: method.IsVirtual || method.IsAbstract || method.IsOverride,
                                                makeArguments(arguments), semanticModel: null, syntax,
                                                method.ReturnType, constantValue: default, isImplicit: true);
            }

            ImmutableArray<IArgumentOperation> makeArguments(Lazy<ImmutableArray<IArgumentOperation>> arguments)
            {
                if (arguments != null)
                {
                    return VisitArguments(arguments.Value);
                }

                return ImmutableArray<IArgumentOperation>.Empty;
            }
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            INamedTypeSymbol booleanType = ((Operation)operation).SemanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);
            int expressionCaptureId = VisitAndCapture(operation.Value);

            ImmutableArray<ILocalSymbol> locals = getLocals();
            bool haveLocals = !locals.IsEmpty;
            if (haveLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: locals));
            }

            BasicBlock defaultBody = null; // Adjusted in handleSection
            BasicBlock @break = GetLabeledOrNewBlock(operation.ExitLabel);

            foreach (ISwitchCaseOperation section in operation.Cases)
            {
                handleSection(section);
            }

            if (defaultBody != null)
            {
                LinkBlocks(CurrentBasicBlock, defaultBody); 
            }

            if (haveLocals)
            {
                LeaveRegion();
            }

            AppendNewBlock(@break);

            return null;

            ImmutableArray<ILocalSymbol> getLocals()
            {
                ImmutableArray<ILocalSymbol> l = operation.Locals;
                foreach (ISwitchCaseOperation section in operation.Cases)
                {
                    l = l.Concat(section.Locals);
                }

                return l;
            }

            void handleSection(ISwitchCaseOperation section)
            {
                var body = new BasicBlock(BasicBlockKind.Block);
                var nextSection = new BasicBlock(BasicBlockKind.Block);

                IOperation condition = ((BaseSwitchCase)section).Condition;
                if (condition != null)
                {
                    Debug.Assert(section.Clauses.All(c => c.Label == null));
                    _currentSwitchOperationExpression = getSwitchValue();
                    VisitConditionalBranch(condition, ref nextSection, sense: false);
                    _currentSwitchOperationExpression = null;
                }
                else
                {
                    foreach (ICaseClauseOperation caseClause in section.Clauses)
                    {
                        var nextCase = new BasicBlock(BasicBlockKind.Block);
                        handleCase(caseClause, body, nextCase);
                        AppendNewBlock(nextCase);
                    }

                    LinkBlocks(CurrentBasicBlock, nextSection);
                }

                AppendNewBlock(body);

                VisitStatements(section.Body);

                LinkBlocks(CurrentBasicBlock, @break);

                AppendNewBlock(nextSection);
            }

            void handleCase(ICaseClauseOperation caseClause, BasicBlock body, BasicBlock nextCase)
            {
                IOperation condition;
                BasicBlock labeled = GetLabeledOrNewBlock(caseClause.Label);
                LinkBlocks(labeled, body);

                switch (caseClause.CaseKind)
                {
                    case CaseKind.SingleValue:
                        handleEqualityCheck(((ISingleValueCaseClauseOperation)caseClause).Value);
                        break;

                        void handleEqualityCheck(IOperation compareWith)
                        {
                            bool leftIsNullable = ITypeSymbolHelpers.IsNullableType(operation.Value.Type);
                            bool rightIsNullable = ITypeSymbolHelpers.IsNullableType(compareWith.Type);
                            bool isLifted = leftIsNullable || rightIsNullable;
                            IOperation leftOperand = getSwitchValue();
                            IOperation rightOperand = Visit(compareWith);

                            if (isLifted)
                            {
                                if (!leftIsNullable)
                                {
                                    if (leftOperand.Type != null)
                                    {
                                        // PROTOTYPE(dataflow): Consider using similar conversion for TryMakeNullable value and using the helper here
                                        leftOperand = new ConversionOperation(leftOperand, ConvertibleConversion.Instance, isTryCast: false, isChecked: false,
                                                                              semanticModel: null, leftOperand.Syntax, compareWith.Type,
                                                                              constantValue: default, isImplicit: true);
                                    }
                                }
                                else if (!rightIsNullable && rightOperand.Type != null)
                                {
                                    // PROTOTYPE(dataflow): Consider using similar conversion for TryMakeNullable value and using the helper here
                                    rightOperand = new ConversionOperation(rightOperand, ConvertibleConversion.Instance, isTryCast: false, isChecked: false,
                                                                           semanticModel: null, rightOperand.Syntax, operation.Value.Type,
                                                                           constantValue: default, isImplicit: true);
                                }
                            }

                            condition = new BinaryOperatorExpression(BinaryOperatorKind.Equals,
                                                                     leftOperand,
                                                                     rightOperand,
                                                                     isLifted,
                                                                     isChecked: false,
                                                                     isCompareText: false,
                                                                     operatorMethod: null,
                                                                     semanticModel: null,
                                                                     compareWith.Syntax,
                                                                     booleanType,
                                                                     constantValue: default,
                                                                     isImplicit: true);

                            condition = Operation.SetParentOperation(condition, null);
                            LinkBlocks(CurrentBasicBlock, (condition, JumpIfTrue: false, RegularBranch(nextCase)));
                            AppendNewBlock(labeled);
                            _currentBasicBlock = null;
                        }

                    case CaseKind.Pattern:
                        var patternClause = (IPatternCaseClauseOperation)caseClause;

                        condition = new IsPatternExpression(getSwitchValue(), Visit(patternClause.Pattern), semanticModel: null, patternClause.Pattern.Syntax, booleanType, constantValue: default, isImplicit: true);
                        condition = Operation.SetParentOperation(condition, null);
                        LinkBlocks(CurrentBasicBlock, (condition, JumpIfTrue: false, RegularBranch(nextCase)));

                        if (patternClause.Guard != null)
                        {
                            AppendNewBlock(new BasicBlock(BasicBlockKind.Block));
                            VisitConditionalBranch(patternClause.Guard, ref nextCase, sense: false);
                        }

                        AppendNewBlock(labeled);
                        _currentBasicBlock = null;
                        break;

                    case CaseKind.Relational:
                        var relationalValueClause = (IRelationalCaseClauseOperation)caseClause;

                        if (relationalValueClause.Relation == BinaryOperatorKind.Equals)
                        {
                            handleEqualityCheck(relationalValueClause.Value);
                            break;
                        }

                        // A switch section with a relational case other than equality must have 
                        // condition assosiated with it. This point should not be reachable.
                        throw ExceptionUtilities.UnexpectedValue(relationalValueClause.Relation);

                    case CaseKind.Default:
                        var defaultClause = (IDefaultCaseClauseOperation)caseClause;
                        if (defaultBody == null)
                        {
                            defaultBody = labeled;
                        }

                        // 'default' clause is never entered from the top, we'll jump back to it after all 
                        // sections are processed.
                        LinkBlocks(CurrentBasicBlock, nextCase);
                        AppendNewBlock(labeled);
                        _currentBasicBlock = null;
                        break;

                    default:
                        // PROTOTYPE(dataflow): TODO. See VisitRangeCaseClause
                        throw new NotImplementedException();
                }
            }

            FlowCaptureReference getSwitchValue()
            {
                return new FlowCaptureReference(expressionCaptureId, operation.Value.Syntax, operation.Value.Type, operation.Value.ConstantValue);
            }
        }

        public override IOperation VisitSwitchCase(ISwitchCaseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable; 
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
            //return new RangeCaseClause(Visit(operation.MinimumValue), Visit(operation.MaximumValue), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitEnd(IEndOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            BasicBlock current = CurrentBasicBlock;
            AppendNewBlock(new BasicBlock(BasicBlockKind.Block), linkToPrevious: false);
            Debug.Assert(current.InternalNext.Value == null);
            Debug.Assert(current.InternalNext.Branch.Destination == null);
            Debug.Assert(current.InternalNext.Branch.Kind == BasicBlock.BranchKind.None);
            current.InternalNext.Branch.Kind = BasicBlock.BranchKind.ProgramTermination;
            return null;
        }

        public override IOperation VisitForLoop(IForLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            // for (initializer; condition; increment)
            //   body;
            //
            // becomes the following (with block added for locals)
            //
            // {
            //   initializer;
            // start:
            //   {
            //     GotoIfFalse condition break;
            //     body;
            // continue:
            //     increment;
            //     goto start;
            //   }
            // }
            // break:

            bool haveLocals = !operation.Locals.IsEmpty;

            if (haveLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.Locals));
            }

            ImmutableArray<IOperation> initialization = operation.Before;
            
            if (initialization.Length == 1 && initialization[0].Kind == OperationKind.VariableDeclarationGroup)
            {
                HandleVariableDeclarations((VariableDeclarationGroupOperation)initialization.Single());
            }
            else
            {
                VisitStatements(initialization);
            }

            var start = new BasicBlock(BasicBlockKind.Block); 
            AppendNewBlock(start);

            bool haveConditionLocals = !operation.ConditionLocals.IsEmpty;
            if (haveConditionLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.ConditionLocals));
            }

            var @break = GetLabeledOrNewBlock(operation.ExitLabel);
            if (operation.Condition != null)
            {
                VisitConditionalBranch(operation.Condition, ref @break, sense: false);
            }

            VisitStatement(operation.Body);

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            AppendNewBlock(@continue);

            VisitStatements(operation.AtLoopBottom);

            LinkBlocks(CurrentBasicBlock, start);

            if (haveConditionLocals)
            {
                LeaveRegion();
            }

            if (haveLocals)
            {
                LeaveRegion();
            }

            AppendNewBlock(@break);

            return null;
        }

        internal override IOperation VisitFixed(IFixedOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            bool haveLocals = !operation.Locals.IsEmpty;
            if (haveLocals)
            {
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.Locals, locals: operation.Locals));
            }

            HandleVariableDeclarations(operation.Variables);

            VisitStatement(operation.Body);

            if (haveLocals)
            {
                LeaveRegion();
            }

            return null;
        }

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, int? captureIdForResult)
        {
            // Anything that has a declaration group (such as for loops) needs to handle them directly itself,
            // this should only be encountered by the visitor for declaration statements.
            Debug.Assert(_currentStatement == operation);

            HandleVariableDeclarations(operation);
            return null;
        }

        private void HandleVariableDeclarations(IVariableDeclarationGroupOperation operation)
        {
            // We erase variable declarations from the control flow graph, as variable lifetime information is
            // contained in a parallel data structure.
            foreach (var declaration in operation.Declarations)
            {
                HandleVariableDeclaration(declaration);
            }
        }

        private void HandleVariableDeclaration(IVariableDeclarationOperation operation)
        {
            foreach (IVariableDeclaratorOperation declarator in operation.Declarators)
            {
                HandleVariableDeclarator(operation, declarator);
            }
        }

        private void HandleVariableDeclarator(IVariableDeclarationOperation declaration, IVariableDeclaratorOperation declarator)
        {
            ILocalSymbol localSymbol = declarator.Symbol;

            // We skip constants in the control flow graph, as they're not actually involved in any control flow.
            if (localSymbol.IsConst)
            {
                return;
            }

            // If the local is a static (possible in VB), then we create a semaphore for conditional execution of the initializer.
            BasicBlock afterInitialization = null;
            if (localSymbol.IsStatic && (declarator.Initializer != null || declaration.Initializer != null))
            {
                afterInitialization = new BasicBlock(BasicBlockKind.Block);

                ITypeSymbol booleanType = ((Operation)declaration).SemanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);
                var initializationSemaphore = new StaticLocalInitializationSemaphoreOperation(localSymbol, declarator.Syntax, booleanType);
                Operation.SetParentOperation(initializationSemaphore, null);

                LinkBlocks(CurrentBasicBlock, (initializationSemaphore, JumpIfTrue: false, RegularBranch(afterInitialization)));

                _currentBasicBlock = null;
                EnterRegion(new RegionBuilder(ControlFlowGraph.RegionKind.StaticLocalInitializer));
            }

            IOperation initializer = null;
            SyntaxNode assignmentSyntax = null;
            if (declarator.Initializer != null)
            {
                initializer = Visit(declarator.Initializer.Value);
                assignmentSyntax = declarator.Syntax;
            }

            if (declaration.Initializer != null)
            {
                IOperation operationInitializer = Visit(declaration.Initializer.Value);
                assignmentSyntax = declaration.Syntax;
                if (initializer != null)
                {
                    // PROTOTYPE(dataflow): Add a test with control flow in a shared initializer after
                    // object creation support has been added
                    initializer = new InvalidOperation(ImmutableArray.Create(initializer, operationInitializer),
                                                        semanticModel: null,
                                                        declaration.Syntax,
                                                        type: localSymbol.Type,
                                                        constantValue: default,
                                                        isImplicit: true);
                }
                else
                {
                    initializer = operationInitializer;
                }
            }

            // If we have an afterInitialization, then we must have static local and an initializer to ensure we don't create empty regions that can't be cleaned up.
            Debug.Assert(afterInitialization == null || (localSymbol.IsStatic && initializer != null));

            if (initializer != null)
            {
                // We can't use the IdentifierToken as the syntax for the local reference, so we use the
                // entire declarator as the node
                var localRef = new LocalReferenceExpression(localSymbol, isDeclaration: true, semanticModel: null, declarator.Syntax, localSymbol.Type, constantValue: default, isImplicit: true);
                var assignment = new SimpleAssignmentExpression(localRef, isRef: localSymbol.IsRef, initializer, semanticModel: null, assignmentSyntax, localRef.Type, constantValue: default, isImplicit: true);
                AddStatement(assignment);

                if (localSymbol.IsStatic)
                {
                    LeaveRegion();
                    AppendNewBlock(afterInitialization);
                }
            }
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, int? captureIdForResult)
        {
            // All variable declarators should be handled by VisitVariableDeclarationGroup.
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, int? captureIdForResult)
        {
            // All variable declarators should be handled by VisitVariableDeclaration.
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitVariableInitializer(IVariableInitializerOperation operation, int? captureIdForResult)
        {
            // All variable initializers should be removed from the tree by VisitVariableDeclaration.
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitFlowCapture(IFlowCaptureOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitIsNull(IIsNullOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitCaughtException(ICaughtExceptionOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitInvocation(IInvocationOperation operation, int? captureIdForResult)
        {
            if (operation.Instance != null)
            {
                // PROTOTYPE(dataflow): drop instance for a static method
                _evalStack.Push(Visit(operation.Instance));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(operation.Arguments);
            IOperation visitedInstance = operation.Instance == null ? null : _evalStack.Pop();

            return new InvocationExpression(operation.TargetMethod, visitedInstance, operation.IsVirtual, visitedArguments, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, int? captureIdForResult)
        {
            ImmutableArray<IArgumentOperation> visitedArgs = VisitArguments(operation.Arguments);

            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            var objectCreation = new ObjectCreationExpression(operation.Constructor, initializer: null, visitedArgs, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);

            // PROTOTYPE(dataflow): For a non-null initializer, we'll need to assign the created object to a flow reference and rewrite all initializers to assign to/call functions on it, then return return the flow reference

            return objectCreation;
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, int? captureIdForResult)
        {
            // If the assignment target is a tuple, we want to decompose the tuple and push each element back onto the stack, so that if the value
            // has control flow the individual elements are captured. Then we can recompose the tuple after operation.Value has been visited.
            // We do this to keep the graph sane, so that users don't have to track a tuple captured via flow control when it's not really
            // the tuple that's been captured, it's the operands to the tuple.
            PushTargetAndUnwrapTupleIfNecessary(operation.Target);
            IOperation visitedValue = Visit(operation.Value);
            IOperation visitedTarget = PopTargetAndWrapTupleIfNecessary(operation.Target);

            return new DeconstructionAssignmentExpression(visitedTarget, visitedValue, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        /// <summary>
        /// Recursively push nexted values onto the stack for visiting
        /// </summary>
        private void PushTargetAndUnwrapTupleIfNecessary(IOperation value)
        {
            if (value.Kind == OperationKind.Tuple)
            {
                var tuple = (ITupleOperation)value;

                foreach (IOperation element in tuple.Elements)
                {
                    PushTargetAndUnwrapTupleIfNecessary(element);
                }
            }
            else
            {
                _evalStack.Push(Visit(value));
            }
        }

        /// <summary>
        /// Recursively pop nested tuple values off the stack after visiting
        /// </summary>
        private IOperation PopTargetAndWrapTupleIfNecessary(IOperation value)
        {
            if (value.Kind == OperationKind.Tuple)
            {
                var tuple = (ITupleOperation)value;
                var numElements = tuple.Elements.Length;
                var elementBuilder = ArrayBuilder<IOperation>.GetInstance(numElements);
                for (int i = numElements - 1; i >= 0; i--)
                {
                    elementBuilder.Add(PopTargetAndWrapTupleIfNecessary(tuple.Elements[i]));
                }
                elementBuilder.ReverseContents();
                return new TupleExpression(elementBuilder.ToImmutableAndFree(), semanticModel: null, tuple.Syntax, tuple.Type, tuple.NaturalType, tuple.ConstantValue, tuple.IsImplicit);
            }
            else
            {
                return _evalStack.Pop();
            }
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, int? captureIdForResult)
        {
            return new DeclarationExpression(VisitPreservingTupleOperations(operation.Expression), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        private IOperation VisitPreservingTupleOperations(IOperation operation)
        {
            PushTargetAndUnwrapTupleIfNecessary(operation);
            return PopTargetAndWrapTupleIfNecessary(operation);
        }

        public override IOperation VisitTuple(ITupleOperation operation, int? captureIdForResult)
        {
            PushArray(operation.Elements);
            return new TupleExpression(PopArray(operation.Elements), semanticModel: null, operation.Syntax, operation.Type, operation.NaturalType, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitNoneOperation(IOperation operation, int? captureIdForResult)
        {
            if (_currentStatement == operation)
            {
                VisitNoneOperationStatement(operation);
                return null;
            }
            else
            {
                return VisitNoneOperationExpression(operation);
            }
        }

        private void VisitNoneOperationStatement(IOperation operation)
        {
            Debug.Assert(_currentStatement == operation);
            VisitStatements(operation.Children);
        }

        private IOperation VisitNoneOperationExpression(IOperation operation)
        {
            int startingStackSize = _evalStack.Count;
            foreach (IOperation child in operation.Children)
            {
                _evalStack.Push(Visit(child));
            }

            int numChildren = _evalStack.Count - startingStackSize;
            Debug.Assert(numChildren == operation.Children.Count());

            if (numChildren == 0)
            {
                return Operation.CreateOperationNone(semanticModel: null, operation.Syntax, operation.ConstantValue, ImmutableArray<IOperation>.Empty, operation.IsImplicit);
            }

            var childrenBuilder = ArrayBuilder<IOperation>.GetInstance(numChildren);
            for (int i = 0; i < numChildren; i++)
            {
                childrenBuilder.Add(_evalStack.Pop());
            }

            childrenBuilder.ReverseContents();

            return Operation.CreateOperationNone(semanticModel: null, operation.Syntax, operation.ConstantValue, childrenBuilder.ToImmutableAndFree(), operation.IsImplicit);
        }

        public override IOperation VisitNameOf(INameOfOperation operation, int? captureIdForResult)
        {
            Debug.Assert(operation.ConstantValue.HasValue);
            return new LiteralExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        private T Visit<T>(T node) where T : IOperation
        {
            return (T)Visit(node, argument: null);
        }

        public IOperation Visit(IOperation operation)
        {
            // We should never be revisiting nodes we've already visited, and we don't set SemanticModel in this builder.
            Debug.Assert(operation == null || ((Operation)operation).SemanticModel != null);
            return Visit(operation, argument: null);
        }

        public override IOperation DefaultVisit(IOperation operation, int? captureIdForResult)
        {
            // this should never reach, otherwise, there is missing override for IOperation type
            throw ExceptionUtilities.Unreachable;
        }

        #region PROTOTYPE(dataflow): Naive implementation that simply clones nodes and erases SemanticModel, likely to change
        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> nodes) where T : IOperation
        {
            // clone the array
            return nodes.SelectAsArray(n => Visit(n));
        }

        public override IOperation VisitArgument(IArgumentOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(DATAFLOW): All usages of this should be removed the following line uncommented when support is added for object creation, property reference, and raise events.
            // throw ExceptionUtilities.Unreachable;
            var baseArgument = (BaseArgument)operation;
            return new ArgumentOperation(Visit(operation.Value), operation.ArgumentKind, operation.Parameter, baseArgument.InConversionConvertibleOpt, baseArgument.OutConversionConvertibleOpt, semanticModel: null, operation.Syntax, operation.IsImplicit);
        }

        public override IOperation VisitConversion(IConversionOperation operation, int? captureIdForResult)
        {
            return new ConversionOperation(Visit(operation.Operand), ((BaseConversionExpression)operation).ConvertibleConversion, operation.IsTryCast, operation.IsChecked, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForToLoop(IForToLoopOperation operation, int? captureIdForResult)
        {
            return new ForToLoopStatement(operation.Locals, operation.ContinueLabel, operation.ExitLabel, Visit(operation.LoopControlVariable), Visit(operation.InitialValue), Visit(operation.LimitValue), Visit(operation.StepValue), Visit(operation.Body), VisitArray(operation.NextVariables), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitWith(IWithOperation operation, int? captureIdForResult)
        {
            return new WithStatement(Visit(operation.Body), Visit(operation.Value), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitStop(IStopOperation operation, int? captureIdForResult)
        {
            return new StopStatement(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, int? captureIdForResult)
        {
            return new OmittedArgumentExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, int? captureIdForResult)
        {
            return new ArrayElementReferenceExpression(Visit(operation.ArrayReference), VisitArray(operation.Indices), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, int? captureIdForResult)
        {
            return new PointerIndirectionReferenceExpression(Visit(operation.Pointer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, int? captureIdForResult)
        {
            return new LocalReferenceExpression(operation.Local, operation.IsDeclaration, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, int? captureIdForResult)
        {
            return new ParameterReferenceExpression(operation.Parameter, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, int? captureIdForResult)
        {
            return new InstanceReferenceExpression(operation.ReferenceKind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): drop instance for a static field
            return new FieldReferenceExpression(operation.Field, operation.IsDeclaration, Visit(operation.Instance), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): drop instance for a static method
            return new MethodReferenceExpression(operation.Method, operation.IsVirtual, Visit(operation.Instance), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): drop instance for a static property
            return new PropertyReferenceExpression(operation.Property, Visit(operation.Instance), VisitArray(operation.Arguments), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, int? captureIdForResult)
        {
            return new EventReferenceExpression(operation.Event, Visit(operation.Instance), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, int? captureIdForResult)
        {
            return new EventAssignmentOperation(Visit(operation.EventReference), Visit(operation.HandlerValue), operation.Adds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitPlaceholder(IPlaceholderOperation operation, int? captureIdForResult)
        {
            if (operation.PlaceholderKind == PlaceholderKind.SwitchOperationExpression && _currentSwitchOperationExpression != null)
            {
                return OperationCloner.CloneOperation(_currentSwitchOperationExpression);
            }

            return new PlaceholderExpression(operation.PlaceholderKind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, int? captureIdForResult)
        {
            return new IsTypeExpression(Visit(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, int? captureIdForResult)
        {
            return new SizeOfExpression(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, int? captureIdForResult)
        {
            return new TypeOfExpression(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, int? captureIdForResult)
        {
            return new AnonymousFunctionExpression(operation.Symbol, 
                                                   // PROTOTYPE(dataflow): Drop lambda's body for now to enable some test scenarios
                                                   new BlockStatement(ImmutableArray<IOperation>.Empty,
                                                                      ImmutableArray<ILocalSymbol>.Empty,
                                                                      semanticModel: null,
                                                                      operation.Body.Syntax, 
                                                                      operation.Body.Type, 
                                                                      operation.Body.ConstantValue, 
                                                                      operation.Body.IsImplicit),
                                                   semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, int? captureIdForResult)
        {
            return new DelegateCreationExpression(Visit(operation.Target), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, int? captureIdForResult)
        {
            return new LiteralExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAwait(IAwaitOperation operation, int? captureIdForResult)
        {
            return new AwaitExpression(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, int? captureIdForResult)
        {
            return new AddressOfExpression(Visit(operation.Reference), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, int? captureIdForResult)
        {
            return new AnonymousObjectCreationExpression(VisitArray(operation.Initializers), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, int? captureIdForResult)
        {
            return new ObjectOrCollectionInitializerExpression(VisitArray(operation.Initializers), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, int? captureIdForResult)
        {
            return new MemberInitializerExpression(Visit(operation.InitializedMember), Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, int? captureIdForResult)
        {
            return new CollectionElementInitializerExpression(operation.AddMethod, operation.IsDynamic, VisitArray(operation.Arguments), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, int? captureIdForResult)
        {
            return new FieldInitializer(operation.Locals, operation.InitializedFields, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, int? captureIdForResult)
        {
            return new PropertyInitializer(operation.Locals, operation.InitializedProperties, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, int? captureIdForResult)
        {
            return new ParameterInitializer(operation.Locals, operation.Parameter, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, int? captureIdForResult)
        {
            return new ArrayCreationExpression(VisitArray(operation.DimensionSizes), Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, int? captureIdForResult)
        {
            return new ArrayInitializer(VisitArray(operation.ElementValues), semanticModel: null, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, int? captureIdForResult)
        {
            bool isDecrement = operation.Kind == OperationKind.Decrement;
            return new IncrementExpression(isDecrement, operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.OperatorMethod, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, int? captureIdForResult)
        {
            return new ParenthesizedExpression(Visit(operation.Operand), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, int? captureIdForResult)
        {
            return new DynamicMemberReferenceExpression(Visit(operation.Instance), operation.MemberName, operation.TypeArguments, operation.ContainingType, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, int? captureIdForResult)
        {
            return new DynamicObjectCreationExpression(VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, int? captureIdForResult)
        {
            return new DynamicInvocationExpression(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, int? captureIdForResult)
        {
            return new DynamicIndexerAccessExpression(Visit(operation.Operation), VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, int? captureIdForResult)
        {
            return new DefaultValueExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, int? captureIdForResult)
        {
            return new TypeParameterObjectCreationExpression(Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, int? captureIdForResult)
        {
            return new InvalidOperation(VisitArray(operation.Children.ToImmutableArray()), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, int? captureIdForResult)
        {
            return new LocalFunctionStatement(operation.Symbol, Visit(operation.Body), Visit(operation.IgnoredBody), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, int? captureIdForResult)
        {
            return new InterpolatedStringExpression(VisitArray(operation.Parts), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, int? captureIdForResult)
        {
            return new InterpolatedStringText(Visit(operation.Text), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, int? captureIdForResult)
        {
            return new Interpolation(Visit(operation.Expression), Visit(operation.Alignment), Visit(operation.FormatString), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, int? captureIdForResult)
        {
            return new IsPatternExpression(Visit(operation.Value), Visit(operation.Pattern), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, int? captureIdForResult)
        {
            return new ConstantPattern(Visit(operation.Value), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, int? captureIdForResult)
        {
            return new DeclarationPattern(operation.DeclaredSymbol, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, int? captureIdForResult)
        {
            return new TranslatedQueryExpression(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, int? captureIdForResult)
        {
            return new RaiseEventStatement(Visit(operation.EventReference), VisitArray(operation.Arguments), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, int? captureIdForResult)
        {
            return new TupleBinaryOperatorExpression(operation.OperatorKind, Visit(operation.LeftOperand), Visit(operation.RightOperand), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, int? captureIdForResult)
        {
            return new ConstructorBodyOperation(operation.Locals, semanticModel: null, operation.Syntax, Visit(operation.Initializer), Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, int? captureIdForResult)
        {
            return new MethodBodyOperation(semanticModel: null, operation.Syntax, Visit(operation.BlockBody), Visit(operation.ExpressionBody));
        }

        public override IOperation VisitDiscardOperation(IDiscardOperation operation, int? captureIdForResult)
        {
            return new DiscardOperation(operation.DiscardSymbol, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
        #endregion
    }
}
