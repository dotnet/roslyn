﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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

        // PROTOTYPE(dataflow): does the public API IFlowCaptureOperation.Id specify how identifiers are created or assigned?
        // Should we use uint to exclude negative integers? Should we randomize them in any way to avoid dependencies 
        // being taken?
        private int _availableCaptureId = 0;

        private ControlFlowGraphBuilder()
        { }

        public static ControlFlowGraph Create(IBlockOperation body)
        {
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

            Debug.Assert(builder._evalStack.Count == 0);
            builder._evalStack.Free();
            builder._regionMap.Free();
            builder._labeledBlocks?.Free();

            return new ControlFlowGraph(blocks.ToImmutableAndFree(), region);
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

                while (source.FirstBlockOrdinal > destinationOrdinal || source.LastBlockOrdinal < destinationOrdinal)
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

                                    if (block.Statements.IsEmpty && block.InternalConditional.Condition == null && block.InternalNext.ReturnValue == null)
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

            int count = blocks.Count - 1;
            for (int i = 1; i < count; i++)
            {
                BasicBlock block = blocks[i];
                block.Ordinal = i;

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
                        Debug.Assert(predecessor.InternalNext.ReturnValue == null);
                        Debug.Assert((predecessor.InternalNext.Branch.Flags & BasicBlock.BranchFlags.Regular) != 0);

                        predecessor.AddStatements(block.Statements);
                        block.RemoveStatements();
                    }
                    else
                    {
                        continue;
                    }
                }

                ref BasicBlock.Branch next = ref block.InternalNext.Branch;

                Debug.Assert((block.InternalNext.ReturnValue == null) == ((next.Flags & BasicBlock.BranchFlags.Return) == 0));
                Debug.Assert(next.Destination != null ||
                             (next.Flags &
                              (BasicBlock.BranchFlags.Error | BasicBlock.BranchFlags.ProgramTermination |
                              BasicBlock.BranchFlags.Throw | BasicBlock.BranchFlags.ReThrow |
                              BasicBlock.BranchFlags.StructuredExceptionHandling)) != 0);

#if DEBUG
                if ((next.Flags & BasicBlock.BranchFlags.StructuredExceptionHandling) != 0)
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
                            next.Destination == null && next.Flags == BasicBlock.BranchFlags.StructuredExceptionHandling &&
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
                                // PROTOTYPE(dataflow): this code path is unreachable at the moment because we get here before we add locals
                                //                      to the .try region. But I think we can hit this code path when there is an explicit 
                                //                      goto at the end of .try once we have handling for gotos. 
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
                        }

                        continue;
                    }

                    if (next.Destination == null)
                    {
                        Debug.Assert(block.InternalNext.ReturnValue == null);

                        if ((next.Flags & BasicBlock.BranchFlags.StructuredExceptionHandling) == 0)
                        {
                            continue;
                        }

                        ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;

                        // It is safe to drop an unreachable empty basic block
                        if (predecessors.Count > 0)
                        {
                            // PROTOTYPE(dataflow): It should be safe to merge other branches with null destination, even when there are more than one predecessor 
                            //                      and more than one incoming branch

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
                                continue;
                            }

                            predecessor.InternalNext = block.InternalNext;
                        }
                    }
                    else
                    {
                        Debug.Assert((next.Flags & ~(BasicBlock.BranchFlags.Regular | BasicBlock.BranchFlags.Error | BasicBlock.BranchFlags.Return)) == 0);

                        ImmutableHashSet<BasicBlock> predecessors = block.Predecessors;
                        IOperation returnValue = block.InternalNext.ReturnValue;

                        RegionBuilder implicitEntryRegion = tryGetImplicitEntryRegion(block, currentRegion);

                        if (implicitEntryRegion != null)
                        {
                            // First blocks in filter/catch/finally do not capture all possible predecessors
                            // Do not try to merge them, unless they are simply linked to the next block
                            if (returnValue != null ||
                                next.Destination != blocks[i + 1])
                            {
                                continue;
                            }

                            Debug.Assert(implicitEntryRegion.LastBlock.Ordinal >= next.Destination.Ordinal);
                        }

                        if (returnValue != null)
                        {
                            BasicBlock predecessor;
                            int predecessorsCount = predecessors.Count;

                            if (predecessorsCount == 0)
                            {
                                // Let's drop an unreachable compiler generated return that VB optimistically adds at the end of a method body
                                if (next.Destination.Kind != BasicBlockKind.Exit ||
                                    !returnValue.IsImplicit ||
                                    returnValue.Kind != OperationKind.LocalReference ||
                                    !((ILocalReferenceOperation)returnValue).Local.IsFunctionValue)
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
                                    // Do not merge return with expression with more than one predecessor
                                    // Do not merge return with expression with conditional branch
                                    // Do not merge return with expression with an entry block
                                    // Do not merge return with expression into a different region
                                    continue;
                                }

                                Debug.Assert(predecessor.InternalNext.Branch.Destination == block);
                            }
                        }

                        RegionBuilder destinationRegion = regionMap[next.Destination];

                        // If source and destination are in different regions, it might
                        // be unsafe to merge branches.
                        if (currentRegion != destinationRegion)
                        {
                            fromCurrent?.Clear();
                            fromDestination?.Clear();

                            if (!checkBranchesFromPredecessors(block, currentRegion, destinationRegion))
                            {
                                continue;
                            }
                        }

                        foreach (BasicBlock predecessor in predecessors)
                        {
                            tryMergeBranch(predecessor, ref predecessor.InternalNext.Branch, block);
                            Debug.Assert(predecessor.InternalNext.ReturnValue == null);
                            predecessor.InternalNext.ReturnValue = returnValue;
                            tryMergeBranch(predecessor, ref predecessor.InternalConditional.Branch, block);
                        }

                        next.Destination.RemovePredecessor(block);
                    }

                    i--;
                    count--;
                    removeBlock(block, currentRegion);
                    anyRemoved = true;
                }
                else
                {
                    if (next.Destination == null)
                    {
                        continue;
                    }

                    Debug.Assert((next.Flags & ~(BasicBlock.BranchFlags.Regular | BasicBlock.BranchFlags.Error | BasicBlock.BranchFlags.Return)) == 0);

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
                        Debug.Assert(predecessor.InternalNext.ReturnValue == null);

                        mergeBranch(predecessor, ref predecessor.InternalNext.Branch, ref next);

                        // PROTOTYPE(dataflow): So far we dont have tests that pass through here with block.InternalNext.ReturnValue
                        //                      Should be able to hit this with backward branches once handling is added
                        predecessor.InternalNext.ReturnValue = block.InternalNext.ReturnValue;
                        next.Destination.RemovePredecessor(block);

                        predecessor.InternalConditional = block.InternalConditional;
                        BasicBlock destination = block.InternalConditional.Branch.Destination;
                        destination.AddPredecessor(predecessor);
                        destination.RemovePredecessor(block);

                        i--;
                        count--;
                        removeBlock(block, currentRegion);
                        anyRemoved = true;
                    }
                }
            }

            blocks[0].Ordinal = 0;
            blocks[count].Ordinal = count;

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

            void tryMergeBranch(BasicBlock predecessor, ref BasicBlock.Branch predecessorBranch, BasicBlock successor)
            {
                if (predecessorBranch.Destination == successor)
                {
                    mergeBranch(predecessor, ref predecessorBranch, ref successor.InternalNext.Branch);
                }
            }

            void mergeBranch(BasicBlock predecessor, ref BasicBlock.Branch predecessorBranch, ref BasicBlock.Branch successorBranch)
            {
                predecessorBranch.Destination = successorBranch.Destination;
                successorBranch.Destination.AddPredecessor(predecessor);
                Debug.Assert((predecessorBranch.Flags & ~(BasicBlock.BranchFlags.Regular | BasicBlock.BranchFlags.Error)) == 0);
                predecessorBranch.Flags = successorBranch.Flags | (predecessorBranch.Flags & BasicBlock.BranchFlags.Error);
            }

            bool checkBranchesFromPredecessors(BasicBlock block, RegionBuilder currentRegion, RegionBuilder destinationRegion)
            {
                foreach (BasicBlock predecessor in block.Predecessors)
                {
                    RegionBuilder predecessorRegion = regionMap[predecessor];

                    // If source and destination are in different regions, it might
                    // be unsafe to merge branches.
                    if (predecessorRegion != currentRegion)
                    {
                        fromPredecessor?.Clear();
                        collectAncestorsAndSelf(currentRegion, ref fromCurrent);
                        collectAncestorsAndSelf(destinationRegion, ref fromDestination);
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
            Debug.Assert(spillingTheStack || !_evalStack.Any(o => o.Kind != OperationKind.FlowCaptureReference));
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

        private static void LinkBlocks(BasicBlock prevBlock, BasicBlock nextBlock, BasicBlock.BranchFlags branchFlags = BasicBlock.BranchFlags.Regular)
        {
            Debug.Assert(prevBlock.InternalNext.ReturnValue == null);
            Debug.Assert(prevBlock.InternalNext.Branch.Destination == null);
            prevBlock.InternalNext.Branch.Destination = nextBlock;
            prevBlock.InternalNext.Branch.Flags = branchFlags;
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

            foreach (var statement in operation.Operations)
            {
                VisitStatement(statement);
            }

            if (haveLocals)
            {
                LeaveRegion();
            }

            return null;
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
                if (operation.Kind != OperationKind.FlowCaptureReference)
                {
                    int captureId = _availableCaptureId++;

                    AddStatement(new FlowCapture(captureId, operation.Syntax, Visit(operation))
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
                                             semanticModel: null, originalArgument.Syntax, originalArgument.ConstantValue, originalArgument.IsImplicit);
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
            if (IsConditional(operation))
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

                                if (fallThrough != null)
                                {
                                    AppendNewBlock(fallThrough);
                                }
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

        public override IOperation VisitCoalesce(ICoalesceOperation operation, int? captureIdForResult)
        {
            SyntaxNode valueSyntax = operation.Value.Syntax;
            ITypeSymbol valueTypeOpt = operation.Value.Type;

            SpillEvalStack();
            int testExpressionCaptureId = VisitAndCapture(operation.Value);

            var whenNull = new BasicBlock(BasicBlockKind.Block);
            Optional<object> constantValue = operation.Value.ConstantValue;

            LinkBlocks(CurrentBasicBlock,
                       (Operation.SetParentOperation(MakeIsNullOperation(((Operation)operation).SemanticModel,
                                                                         new FlowCaptureReference(testExpressionCaptureId, valueSyntax, valueTypeOpt, constantValue)),
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
                    possiblyUnwrappedValue = TryUnwrapNullableValue(capturedValue);
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

            int resultCaptureId = captureIdForResult ?? _availableCaptureId++;

            AddStatement(new FlowCapture(resultCaptureId, valueSyntax, convertedTestExpression));

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
            return new BasicBlock.Branch() { Destination = destination, Flags = BasicBlock.BranchFlags.Regular };
        }

        private static IOperation MakeInvalidOperation(ITypeSymbol type, IOperation child)
        {
            return new InvalidOperation(ImmutableArray.Create<IOperation>(child),
                                        semanticModel: null, child.Syntax, type,
                                        constantValue: default, isImplicit: true);
        }

        private static IsNullOperation MakeIsNullOperation(SemanticModel semanticModel, IOperation operand)
        {
            Optional<object> constantValue = operand.ConstantValue;
            return new IsNullOperation(operand.Syntax, operand,
                                       semanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean),
                                       constantValue.HasValue ? new Optional<object>(constantValue.Value == null) : default);
        }

        private static IOperation TryUnwrapNullableValue(IOperation value)
        {
            ITypeSymbol valueType = value.Type;

            Debug.Assert(ITypeSymbolHelpers.IsNullableType(valueType));

            foreach (ISymbol candidate in valueType.GetMembers("GetValueOrDefault"))
            {
                if (candidate.Kind == SymbolKind.Method && !candidate.IsStatic && candidate.DeclaredAccessibility == Accessibility.Public)
                {
                    var method = (IMethodSymbol)candidate;
                    if (method.Parameters.Length == 0 && !method.ReturnsByRef && !method.ReturnsByRefReadonly &&
                        method.OriginalDefinition.ReturnType.Equals(((INamedTypeSymbol)valueType).OriginalDefinition.TypeParameters[0]))
                    {
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
                           (Operation.SetParentOperation(MakeIsNullOperation(((Operation)operation).SemanticModel,
                                                                             new FlowCaptureReference(testExpressionCaptureId, testExpressionSyntax, testExpressionType, constantValue)),
                                                         null),
                            true,
                            RegularBranch(whenNull)));
                _currentBasicBlock = null;

                IOperation receiver = new FlowCaptureReference(testExpressionCaptureId, testExpressionSyntax, testExpressionType, constantValue);

                if (ITypeSymbolHelpers.IsNullableType(testExpressionType))
                {
                    receiver = TryUnwrapNullableValue(receiver) ??
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

            int resultCaptureId = captureIdForResult ?? _availableCaptureId++;

            if (ITypeSymbolHelpers.IsNullableType(operation.Type) && !ITypeSymbolHelpers.IsNullableType(currentConditionalAccess.WhenNotNull.Type))
            {
                IOperation access = Visit(currentConditionalAccess.WhenNotNull);
                AddStatement(new FlowCapture(resultCaptureId, currentConditionalAccess.WhenNotNull.Syntax,
                                             TryMakeNullableValue((INamedTypeSymbol)operation.Type, access) ??
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

        private static IOperation TryMakeNullableValue(INamedTypeSymbol type, IOperation underlyingValue)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(type));

            foreach (IMethodSymbol method in type.InstanceConstructors)
            {
                if (method.DeclaredAccessibility == Accessibility.Public && method.Parameters.Length == 1 &&
                    method.OriginalDefinition.Parameters[0].Type.Equals(type.OriginalDefinition.TypeParameters[0]))
                {
                    return new ObjectCreationExpression(method, initializer: null,
                                                        ImmutableArray.Create<IArgumentOperation>(
                                                                    new ArgumentOperation(underlyingValue,
                                                                                        ArgumentKind.Explicit,
                                                                                        method.Parameters[0],
                                                                                        inConversionOpt: null,
                                                                                        outConversionOpt: null,
                                                                                        semanticModel: null,
                                                                                        underlyingValue.Syntax,
                                                                                        constantValue: null,
                                                                                        isImplicit: true)),
                                                        semanticModel: null,
                                                        underlyingValue.Syntax,
                                                        type,
                                                        constantValue: null,
                                                        isImplicit: true);
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
                        continueDispatchBlock.InternalNext.Branch.Flags = BasicBlock.BranchFlags.StructuredExceptionHandling;
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
                continueDispatchBlock.InternalNext.Branch.Flags = BasicBlock.BranchFlags.StructuredExceptionHandling;
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
                    LinkBlocks(CurrentBasicBlock, _exit, returnedValue is null ? BasicBlock.BranchFlags.Regular : BasicBlock.BranchFlags.Return);
                    current.InternalNext.ReturnValue = Operation.SetParentOperation(returnedValue, null);
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

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, int? captureIdForResult)
        {
            // Anything that has a declaration group (such as for loops) needs to handle them directly itself,
            // this should only be encountered by the visitor for declaration statements.
            Debug.Assert(_currentStatement == operation);

            // We erase variable declarations from the control flow graph, as variable lifetime information is
            // contained in a parallel data structure.
            foreach (var declaration in operation.Declarations)
            {
                HandleVariableDeclaration(declaration);
            }

            return null;
        }

        private void HandleVariableDeclaration(IVariableDeclarationOperation operation)
        {
            foreach (IVariableDeclaratorOperation declarator in operation.Declarators)
            {
                ILocalSymbol localSymbol = declarator.Symbol;

                IOperation initializer = null;
                SyntaxNode assignmentSyntax = null;
                if (declarator.Initializer != null)
                {
                    initializer = Visit(declarator.Initializer.Value);
                    assignmentSyntax = declarator.Syntax;
                }

                if (operation.Initializer != null)
                {
                    IOperation operationInitializer = Visit(operation.Initializer.Value);
                    assignmentSyntax = operation.Syntax;
                    if (initializer != null)
                    {
                        // PROTOTYPE(dataflow): Add a test with control flow in a shared initializer after
                        // object creation support has been added
                        initializer = new InvalidOperation(ImmutableArray.Create(initializer, operationInitializer),
                                                           semanticModel: null,
                                                           operation.Syntax,
                                                           type: localSymbol.Type,
                                                           constantValue: default,
                                                           isImplicit: true);
                    }
                    else
                    {
                        initializer = operationInitializer;
                    }
                }

                if (initializer != null)
                {
                    // We can't use the IdentifierToken as the syntax for the local reference, so we use the
                    // entire declarator as the node
                    var localRef = new LocalReferenceExpression(localSymbol, isDeclaration: true, semanticModel: null, declarator.Syntax, localSymbol.Type, constantValue: default, isImplicit: true);
                    var assignment = new SimpleAssignmentExpression(localRef, isRef: localSymbol.IsRef, initializer, semanticModel: null, assignmentSyntax, localRef.Type, constantValue: default, isImplicit: true);
                    AddStatement(assignment);
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
            foreach (var child in operation.Children)
            {
                VisitStatement(child);
            }
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

        private T Visit<T>(T node) where T : IOperation
        {
            return (T)Visit(node, argument: null);
        }

        public IOperation Visit(IOperation operation)
        {
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
            return new ArgumentOperation(Visit(operation.Value), operation.ArgumentKind, operation.Parameter, baseArgument.InConversionConvertibleOpt, baseArgument.OutConversionConvertibleOpt, semanticModel: null, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitConversion(IConversionOperation operation, int? captureIdForResult)
        {
            return new ConversionOperation(Visit(operation.Operand), ((BaseConversionExpression)operation).ConvertibleConversion, operation.IsTryCast, operation.IsChecked, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, int? captureIdForResult)
        {
            return new SwitchStatement(Visit(operation.Value), VisitArray(operation.Cases), operation.ExitLabel, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSwitchCase(ISwitchCaseOperation operation, int? captureIdForResult)
        {
            return new SwitchCase(VisitArray(operation.Clauses), VisitArray(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, int? captureIdForResult)
        {
            return new SingleValueCaseClause(Visit(operation.Value), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, int? captureIdForResult)
        {
            return new RelationalCaseClause(Visit(operation.Value), operation.Relation, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, int? captureIdForResult)
        {
            return new RangeCaseClause(Visit(operation.MinimumValue), Visit(operation.MaximumValue), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, int? captureIdForResult)
        {
            return new DefaultCaseClause(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForLoop(IForLoopOperation operation, int? captureIdForResult)
        {
            return new ForLoopStatement(VisitArray(operation.Before), Visit(operation.Condition), VisitArray(operation.AtLoopBottom), operation.Locals, operation.ContinueLabel, operation.ExitLabel, Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForToLoop(IForToLoopOperation operation, int? captureIdForResult)
        {
            return new ForToLoopStatement(operation.Locals, operation.ContinueLabel, operation.ExitLabel, Visit(operation.LoopControlVariable), Visit(operation.InitialValue), Visit(operation.LimitValue), Visit(operation.StepValue), Visit(operation.Body), VisitArray(operation.NextVariables), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): note that the loop control variable can be an IVariableDeclarator directly, and this function is expected to handle it without calling Visit(IVariableDeclarator)
            return new ForEachLoopStatement(operation.Locals, operation.ContinueLabel, operation.ExitLabel, Visit(operation.LoopControlVariable), Visit(operation.Collection), VisitArray(operation.NextVariables), Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitLock(ILockOperation operation, int? captureIdForResult)
        {
            return new LockStatement(Visit(operation.LockedValue), Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitUsing(IUsingOperation operation, int? captureIdForResult)
        {
            return new UsingStatement(Visit(operation.Resources), Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitFixed(IFixedOperation operation, int? captureIdForResult)
        {
            return new FixedStatement(Visit(operation.Variables), Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitExpressionStatement(IExpressionStatementOperation operation, int? captureIdForResult)
        {
            return new ExpressionStatement(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        internal override IOperation VisitWith(IWithOperation operation, int? captureIdForResult)
        {
            return new WithStatement(Visit(operation.Body), Visit(operation.Value), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitStop(IStopOperation operation, int? captureIdForResult)
        {
            return new StopStatement(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitEnd(IEndOperation operation, int? captureIdForResult)
        {
            return new EndStatement(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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
            return new InstanceReferenceExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, int? captureIdForResult)
        {
            return new FieldReferenceExpression(operation.Field, operation.IsDeclaration, Visit(operation.Instance), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, int? captureIdForResult)
        {
            return new MethodReferenceExpression(operation.Method, operation.IsVirtual, Visit(operation.Instance), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, int? captureIdForResult)
        {
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
            return new PlaceholderExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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
            return new AnonymousFunctionExpression(operation.Symbol, Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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

        public override IOperation VisitNameOf(INameOfOperation operation, int? captureIdForResult)
        {
            return new NameOfExpression(Visit(operation.Argument), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitThrow(IThrowOperation operation, int? captureIdForResult)
        {
            return new ThrowExpression(Visit(operation.Exception), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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
            return new FieldInitializer(operation.InitializedFields, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, int? captureIdForResult)
        {
            return new PropertyInitializer(operation.InitializedProperties, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, int? captureIdForResult)
        {
            return new ParameterInitializer(operation.Parameter, Visit(operation.Value), operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, int? captureIdForResult)
        {
            return new ArrayCreationExpression(VisitArray(operation.DimensionSizes), Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, int? captureIdForResult)
        {
            return new ArrayInitializer(VisitArray(operation.ElementValues), semanticModel: null, operation.Syntax, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, int? captureIdForResult)
        {
            return new DeconstructionAssignmentExpression(Visit(operation.Target), Visit(operation.Value), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, int? captureIdForResult)
        {
            return new DeclarationExpression(Visit(operation.Expression), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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
            return new LocalFunctionStatement(operation.Symbol, Visit(operation.Body), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
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

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, int? captureIdForResult)
        {
            return new PatternCaseClause(operation.Label, Visit(operation.Pattern), Visit(operation.Guard), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTuple(ITupleOperation operation, int? captureIdForResult)
        {
            return new TupleExpression(VisitArray(operation.Elements), semanticModel: null, operation.Syntax, operation.Type, operation.NaturalType, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, int? captureIdForResult)
        {
            return new TranslatedQueryExpression(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, int? captureIdForResult)
        {
            return new RaiseEventStatement(Visit(operation.EventReference), VisitArray(operation.Arguments), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }
        #endregion
    }
}
