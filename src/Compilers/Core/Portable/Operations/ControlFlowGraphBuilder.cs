// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder : OperationVisitor<int?, IOperation>
    {
        private readonly BasicBlockBuilder _entry = new BasicBlockBuilder(BasicBlockKind.Entry);
        private readonly BasicBlockBuilder _exit = new BasicBlockBuilder(BasicBlockKind.Exit);

        private ArrayBuilder<BasicBlockBuilder> _blocks;
        private PooledDictionary<BasicBlockBuilder, RegionBuilder> _regionMap;
        private BasicBlockBuilder _currentBasicBlock;
        private RegionBuilder _currentRegion;
        private PooledDictionary<ILabelSymbol, BasicBlockBuilder> _labeledBlocks;

        private IOperation _currentStatement;
        private ArrayBuilder<IOperation> _evalStack;
        private IOperation _currentConditionalAccessInstance;
        private IOperation _currentSwitchOperationExpression;
        private IOperation _forToLoopBinaryOperatorLeftOperand;
        private IOperation _forToLoopBinaryOperatorRightOperand;
        private IOperation _currentAggregationGroup;
        private bool _forceImplicit; // Force all rewritten nodes to be marked as implicit regardless of their original state.

        // PROTOTYPE(dataflow): does the public API IFlowCaptureOperation.Id specify how identifiers are created or assigned?
        // Should we use uint to exclude negative integers? Should we randomize them in any way to avoid dependencies 
        // being taken?
        private int _availableCaptureId = 0;

        /// <summary>
        /// Holds the current object being initialized if we're visiting an object initializer.
        /// Or the target of a VB With statement.
        /// </summary>
        private IOperation _currentImplicitInstance;

        private ControlFlowGraphBuilder()
        { }

        private bool IsImplicit(IOperation operation)
        {
            return _forceImplicit || operation.IsImplicit;
        }

        public static ControlFlowGraph Create(IOperation body, ControlFlowRegion enclosing = null)
        {
            // PROTOTYPE(dataflow): Consider getting the SemanticModel and Compilation from the root node, 
            //                      storing them in readonly fields in ControlFlowGraphBuilder, and reusing
            //                      throughout the process rather than getting them from individual nodes.

            Debug.Assert(body != null);

#if DEBUG
            if (enclosing == null)
            {
                Debug.Assert(body.Parent == null);
                Debug.Assert(body.Kind == OperationKind.Block ||
                    body.Kind == OperationKind.MethodBodyOperation ||
                    body.Kind == OperationKind.ConstructorBodyOperation ||
                    body.Kind == OperationKind.FieldInitializer ||
                    body.Kind == OperationKind.PropertyInitializer ||
                    body.Kind == OperationKind.ParameterInitializer,
                    $"Unexpected root operation kind: {body.Kind}");
            }
            else
            {
                Debug.Assert(body.Kind == OperationKind.LocalFunction || body.Kind == OperationKind.AnonymousFunction);
            }
#endif 

            var builder = new ControlFlowGraphBuilder();
            var blocks = ArrayBuilder<BasicBlockBuilder>.GetInstance();
            builder._blocks = blocks;
            builder._evalStack = ArrayBuilder<IOperation>.GetInstance();
            builder._regionMap = PooledDictionary<BasicBlockBuilder, RegionBuilder>.GetInstance();

            var root = new RegionBuilder(ControlFlowRegionKind.Root);
            builder.EnterRegion(root);
            builder.AppendNewBlock(builder._entry, linkToPrevious: false);
            builder._currentBasicBlock = null;

            builder.EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime));

            switch(body.Kind)
            {
                case OperationKind.LocalFunction:
                    builder.VisitLocalFunctionAsRoot((ILocalFunctionOperation)body);
                    break;
                case OperationKind.AnonymousFunction:
                    var anonymousFunction = (IAnonymousFunctionOperation)body;
                    builder.VisitStatement(anonymousFunction.Body);
                    break;
                default:
                    builder.VisitStatement(body);
                    break;
            }

            builder.LeaveRegion();

            builder.AppendNewBlock(builder._exit);
            builder.LeaveRegion();
            Debug.Assert(builder._currentRegion == null);

            CheckUnresolvedBranches(blocks, builder._labeledBlocks);
            Pack(blocks, root, builder._regionMap);
            var methods = ArrayBuilder<IMethodSymbol>.GetInstance();
            var methodsMap = ImmutableDictionary.CreateBuilder<IMethodSymbol, (ControlFlowRegion, IOperation, int)>();
            ControlFlowRegion region = root.ToImmutableRegionAndFree(blocks, methods, methodsMap, enclosing);
            root = null;
            MarkReachableBlocks(blocks);

            Debug.Assert(builder._evalStack.Count == 0);
            builder._evalStack.Free();
            builder._regionMap.Free();
            builder._labeledBlocks?.Free();

            return new ControlFlowGraph(body, ToImmutableBlocks(blocks), region, methods.ToImmutableAndFree(), methodsMap.ToImmutable());
        }

        private static ImmutableArray<BasicBlock> ToImmutableBlocks(ArrayBuilder<BasicBlockBuilder> blockBuilders)
        {
            var builder = ArrayBuilder<BasicBlock>.GetInstance(blockBuilders.Count);

            // Pass 1: Iterate through blocksBuilder to create basic blocks.
            foreach (BasicBlockBuilder blockBuilder in blockBuilders)
            {
                builder.Add(blockBuilder.ToImmutable());
            }

            // Pass 2: Create control flow branches with source and destination info and
            //         update the branch information for the created basic blocks.
            foreach (BasicBlockBuilder blockBuilder in blockBuilders)
            {
                ControlFlowBranch successor = getFallThroughSuccessor(blockBuilder);
                ControlFlowBranch conditionalSuccessor = getConditionalSuccessor(blockBuilder);
                builder[blockBuilder.Ordinal].SetSuccessors(successor, conditionalSuccessor);
            }

            // Pass 3: Set the predecessors for the created basic blocks.
            foreach (BasicBlockBuilder blockBuilder in blockBuilders)
            {
                builder[blockBuilder.Ordinal].SetPredecessors(blockBuilder.ConvertPredecessorsToBranches(builder));
            }

            return builder.ToImmutableAndFree();

            ControlFlowBranch getFallThroughSuccessor(BasicBlockBuilder blockBuilder)
            {
                return blockBuilder.Kind != BasicBlockKind.Exit ?
                           getBranch(in blockBuilder.FallThrough, blockBuilder, isConditionalSuccessor: false) :
                           null;
            }

            ControlFlowBranch getConditionalSuccessor(BasicBlockBuilder blockBuilder)
            {
                return blockBuilder.HasCondition ?
                           getBranch(in blockBuilder.Conditional, blockBuilder, isConditionalSuccessor: true) :
                           null;
            }

            ControlFlowBranch getBranch(in BasicBlockBuilder.Branch branch, BasicBlockBuilder source, bool isConditionalSuccessor)
            {
                return new ControlFlowBranch(
                        source: builder[source.Ordinal],
                        destination: branch.Destination != null ? builder[branch.Destination.Ordinal] : null,
                        branch.Kind,
                        isConditionalSuccessor);
            }
        }

        private static void MarkReachableBlocks(ArrayBuilder<BasicBlockBuilder> blocks)
        {
            var continueDispatchAfterFinally = PooledDictionary<ControlFlowRegion, bool>.GetInstance();
            var dispatchedExceptionsFromRegions = PooledHashSet<ControlFlowRegion>.GetInstance();
            MarkReachableBlocks(blocks, firstBlockOrdinal: 0, lastBlockOrdinal: blocks.Count - 1,
                                outOfRangeBlocksToVisit: null,
                                continueDispatchAfterFinally,
                                dispatchedExceptionsFromRegions,
                                out _);
            continueDispatchAfterFinally.Free();
            dispatchedExceptionsFromRegions.Free();
        }

        private static BitVector MarkReachableBlocks(
            ArrayBuilder<BasicBlockBuilder> blocks,
            int firstBlockOrdinal,
            int lastBlockOrdinal,
            ArrayBuilder<BasicBlockBuilder> outOfRangeBlocksToVisit,
            PooledDictionary<ControlFlowRegion, bool> continueDispatchAfterFinally,
            PooledHashSet<ControlFlowRegion> dispatchedExceptionsFromRegions,
            out bool fellThrough)
        {
            var visited = BitVector.Empty;
            var toVisit = ArrayBuilder<BasicBlockBuilder>.GetInstance();

            fellThrough = false;
            toVisit.Push(blocks[firstBlockOrdinal]);

            do
            {
                BasicBlockBuilder current = toVisit.Pop();

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

                if (current.HasCondition)
                {
                    if (current.BranchValue.ConstantValue.HasValue && current.BranchValue.ConstantValue.Value is bool constant)
                    {
                        if (constant == (current.ConditionKind == ControlFlowConditionKind.WhenTrue))
                        {
                            followBranch(current, in current.Conditional);
                            fallThrough = false;
                        }
                    }
                    else
                    {
                        followBranch(current, in current.Conditional);
                    }
                }

                if (fallThrough)
                {
                    BasicBlockBuilder.Branch branch = current.FallThrough;
                    followBranch(current, in branch);

                    if (current.Ordinal == lastBlockOrdinal && branch.Kind != ControlFlowBranchSemantics.Throw && branch.Kind != ControlFlowBranchSemantics.Rethrow)
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

            void followBranch(BasicBlockBuilder current, in BasicBlockBuilder.Branch branch)
            {
                switch (branch.Kind)
                {
                    case ControlFlowBranchSemantics.None:
                    case ControlFlowBranchSemantics.ProgramTermination:
                    case ControlFlowBranchSemantics.StructuredExceptionHandling:
                    case ControlFlowBranchSemantics.Throw:
                    case ControlFlowBranchSemantics.Rethrow:
                    case ControlFlowBranchSemantics.Error:
                        Debug.Assert(branch.Destination == null);
                        return;

                    case ControlFlowBranchSemantics.Regular:
                    case ControlFlowBranchSemantics.Return:
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
            bool stepThroughFinally(ControlFlowRegion region, BasicBlockBuilder destination)
            {
                int destinationOrdinal = destination.Ordinal;
                while (!region.ContainsBlock(destinationOrdinal))
                {
                    Debug.Assert(region.Kind != ControlFlowRegionKind.Root);
                    ControlFlowRegion enclosing = region.EnclosingRegion;
                    if (region.Kind == ControlFlowRegionKind.Try && enclosing.Kind == ControlFlowRegionKind.TryAndFinally)
                    {
                        Debug.Assert(enclosing.NestedRegions[0] == region);
                        Debug.Assert(enclosing.NestedRegions[1].Kind == ControlFlowRegionKind.Finally);
                        if (!stepThroughSingleFinally(enclosing.NestedRegions[1]))
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
            bool stepThroughSingleFinally(ControlFlowRegion @finally)
            {
                Debug.Assert(@finally.Kind == ControlFlowRegionKind.Finally);

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
                                       blocks[@finally.LastBlockOrdinal].FallThrough.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling;

                    continueDispatchAfterFinally.Add(@finally, continueDispatch);
                }

                return continueDispatch;
            }

            void dispatchException(ControlFlowRegion fromRegion)
            {
                do
                {
                    if (!dispatchedExceptionsFromRegions.Add(fromRegion))
                    {
                        return;
                    }

                    ControlFlowRegion enclosing = fromRegion.Kind == ControlFlowRegionKind.Root ? null : fromRegion.EnclosingRegion;
                    if (fromRegion.Kind == ControlFlowRegionKind.Try)
                    {
                        switch (enclosing.Kind)
                        {
                            case ControlFlowRegionKind.TryAndFinally:
                                Debug.Assert(enclosing.NestedRegions[0] == fromRegion);
                                Debug.Assert(enclosing.NestedRegions[1].Kind == ControlFlowRegionKind.Finally);
                                if (!stepThroughSingleFinally(enclosing.NestedRegions[1]))
                                {
                                    // The point that continues dispatch is not reachable. Cancel the dispatch.
                                    return;
                                }
                                break;

                            case ControlFlowRegionKind.TryAndCatch:
                                Debug.Assert(enclosing.NestedRegions[0] == fromRegion);
                                dispatchExceptionThroughCatches(enclosing, startAt: 1);
                                break;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(enclosing.Kind);
                        }
                    }
                    else if (fromRegion.Kind == ControlFlowRegionKind.Filter)
                    {
                        // If filter throws, dispatch is resumed at the next catch with an original exception
                        Debug.Assert(enclosing.Kind == ControlFlowRegionKind.FilterAndHandler);
                        ControlFlowRegion tryAndCatch = enclosing.EnclosingRegion;
                        Debug.Assert(tryAndCatch.Kind == ControlFlowRegionKind.TryAndCatch);

                        int index = tryAndCatch.NestedRegions.IndexOf(enclosing, startIndex: 1);

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

            void dispatchExceptionThroughCatches(ControlFlowRegion tryAndCatch, int startAt)
            {
                // For simplicity, we do not try to figure out whether a catch clause definitely
                // handles all exceptions.

                Debug.Assert(tryAndCatch.Kind == ControlFlowRegionKind.TryAndCatch);
                Debug.Assert(startAt > 0);
                Debug.Assert(startAt <= tryAndCatch.NestedRegions.Length);

                for (int i = startAt; i < tryAndCatch.NestedRegions.Length; i++)
                {
                    ControlFlowRegion @catch = tryAndCatch.NestedRegions[i];

                    switch (@catch.Kind)
                    {
                        case ControlFlowRegionKind.Catch:
                            toVisit.Add(blocks[@catch.FirstBlockOrdinal]);
                            break;

                        case ControlFlowRegionKind.FilterAndHandler:
                            BasicBlockBuilder entryBlock = blocks[@catch.FirstBlockOrdinal];
                            Debug.Assert(@catch.NestedRegions[0].Kind == ControlFlowRegionKind.Filter);
                            Debug.Assert(entryBlock.Ordinal == @catch.NestedRegions[0].FirstBlockOrdinal);

                            toVisit.Add(entryBlock);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(@catch.Kind);
                    }
                }
            }
        }

        /// <summary>
        /// Do a pass to eliminate blocks without statements that can be merged with predecessor(s) and
        /// to eliminate regions that can be merged with parents.
        /// </summary>
        private static void Pack(ArrayBuilder<BasicBlockBuilder> blocks, RegionBuilder root, PooledDictionary<BasicBlockBuilder, RegionBuilder> regionMap)
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

        private static bool PackRegions(RegionBuilder root, ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<BasicBlockBuilder, RegionBuilder> regionMap)
        {
            return PackRegion(root);

            bool PackRegion(RegionBuilder region)
            {
                Debug.Assert(!region.IsEmpty);
                bool result = false;

                if (region.HasRegions)
                {
                    for (int i = region.Regions.Count - 1; i >= 0; i--)
                    {
                        RegionBuilder r = region.Regions[i];
                        if (PackRegion(r))
                        {
                            result = true;
                        }

                        if (r.Kind == ControlFlowRegionKind.LocalLifetime &&
                            r.Locals.IsEmpty && !r.HasMethods)
                        {
                            MergeSubRegionAndFree(r, blocks, regionMap);
                            result = true;
                        }
                    }
                }

                switch (region.Kind)
                {
                    case ControlFlowRegionKind.Root:
                    case ControlFlowRegionKind.Filter:
                    case ControlFlowRegionKind.Try:
                    case ControlFlowRegionKind.Catch:
                    case ControlFlowRegionKind.Finally:
                    case ControlFlowRegionKind.LocalLifetime:
                    case ControlFlowRegionKind.StaticLocalInitializer:
                    case ControlFlowRegionKind.ErroneousBody:

                        if (region.Regions?.Count == 1)
                        {
                            RegionBuilder subRegion = region.Regions[0];
                            if (subRegion.Kind == ControlFlowRegionKind.LocalLifetime && subRegion.FirstBlock == region.FirstBlock && subRegion.LastBlock == region.LastBlock)
                            {
                                Debug.Assert(region.Kind != ControlFlowRegionKind.Root);

                                // Transfer all content of the sub-region into the current region
                                region.Locals = region.Locals.Concat(subRegion.Locals);
                                region.AddRange(subRegion.Methods);
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

                                if (subRegion.Kind == ControlFlowRegionKind.LocalLifetime && !subRegion.HasMethods &&
                                    !subRegion.HasRegions && subRegion.FirstBlock == subRegion.LastBlock)
                                {
                                    BasicBlockBuilder block = subRegion.FirstBlock;

                                    if (!block.HasStatements && block.BranchValue == null)
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

                    case ControlFlowRegionKind.TryAndCatch:
                    case ControlFlowRegionKind.TryAndFinally:
                    case ControlFlowRegionKind.FilterAndHandler:
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
        private static void MergeSubRegionAndFree(RegionBuilder subRegion, ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<BasicBlockBuilder, RegionBuilder> regionMap)
        {
            Debug.Assert(subRegion.Kind != ControlFlowRegionKind.Root);
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
        private static bool PackBlocks(ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<BasicBlockBuilder, RegionBuilder> regionMap)
        {
            ArrayBuilder<RegionBuilder> fromCurrent = null;
            ArrayBuilder<RegionBuilder> fromDestination = null;
            ArrayBuilder<RegionBuilder> fromPredecessor = null;
            ArrayBuilder<BasicBlockBuilder> predecessorsBuilder = null;

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
                    BasicBlockBuilder block = blocks[i];
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

                    if (block.HasStatements)
                    {
                        // See if we can move all statements to the previous block
                        BasicBlockBuilder predecessor = block.GetSingletonPredecessorOrDefault();
                        if (predecessor != null &&
                            !predecessor.HasCondition &&
                            predecessor.Ordinal < block.Ordinal &&
                            predecessor.Kind != BasicBlockKind.Entry &&
                            predecessor.FallThrough.Destination == block &&
                            regionMap[predecessor] == regionMap[block])
                        {
                            Debug.Assert(predecessor.BranchValue == null);
                            Debug.Assert(predecessor.FallThrough.Kind == ControlFlowBranchSemantics.Regular);

                            predecessor.MoveStatementsFrom(block);
                            retry = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    ref BasicBlockBuilder.Branch next = ref block.FallThrough;

                    Debug.Assert((block.BranchValue != null && !block.HasCondition) == (next.Kind == ControlFlowBranchSemantics.Return || next.Kind == ControlFlowBranchSemantics.Throw));
                    Debug.Assert((next.Destination == null) ==
                                 (next.Kind == ControlFlowBranchSemantics.ProgramTermination ||
                                  next.Kind == ControlFlowBranchSemantics.Throw ||
                                  next.Kind == ControlFlowBranchSemantics.Rethrow ||
                                  next.Kind == ControlFlowBranchSemantics.Error ||
                                  next.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling));

#if DEBUG
                    if (next.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling)
                    {
                        RegionBuilder currentRegion = regionMap[block];
                        Debug.Assert(currentRegion.Kind == ControlFlowRegionKind.Filter ||
                                     currentRegion.Kind == ControlFlowRegionKind.Finally);
                        Debug.Assert(block == currentRegion.LastBlock);
                    }
#endif

                    if (!block.HasCondition)
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
                            if (currentRegion.Kind == ControlFlowRegionKind.Finally &&
                                next.Destination == null && next.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling &&
                                !block.HasPredecessors)
                            {
                                // Nothing useful is happening in this finally, let's remove it
                                RegionBuilder tryAndFinally = currentRegion.Enclosing;
                                Debug.Assert(tryAndFinally.Kind == ControlFlowRegionKind.TryAndFinally);
                                Debug.Assert(tryAndFinally.Regions.Count == 2);

                                RegionBuilder @try = tryAndFinally.Regions.First();
                                Debug.Assert(@try.Kind == ControlFlowRegionKind.Try);
                                Debug.Assert(tryAndFinally.Regions.Last() == currentRegion);

                                // If .try region has locals or methods, let's convert it to .locals, otherwise drop it
                                if (@try.Locals.IsEmpty && !@try.HasMethods)
                                {
                                    i = @try.FirstBlock.Ordinal - 1; // restart at the first block of removed .try region
                                    MergeSubRegionAndFree(@try, blocks, regionMap);
                                }
                                else
                                {
                                    @try.Kind = ControlFlowRegionKind.LocalLifetime;
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

                        if (next.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling)
                        {
                            Debug.Assert(block.HasCondition || block.BranchValue == null);
                            Debug.Assert(next.Destination == null);

                            // It is safe to drop an unreachable empty basic block
                            if (block.HasPredecessors)
                            {
                                BasicBlockBuilder predecessor = block.GetSingletonPredecessorOrDefault();

                                if (predecessor == null)
                                {
                                    continue;
                                }

                                if (predecessor.Ordinal != i - 1 ||
                                    predecessor.FallThrough.Destination != block ||
                                    predecessor.Conditional.Destination == block ||
                                    regionMap[predecessor] != currentRegion)
                                {
                                    // Do not merge StructuredExceptionHandling into the middle of the filter or finally,
                                    // Do not merge StructuredExceptionHandling into conditional branch
                                    // Do not merge StructuredExceptionHandling into a different region
                                    // It is much easier to walk the graph when we can rely on the fact that a StructuredExceptionHandling
                                    // branch is only in the last block in the region, if it is present.
                                    continue;
                                }

                                predecessor.FallThrough = block.FallThrough;
                            }
                        }
                        else
                        {
                            Debug.Assert(next.Kind == ControlFlowBranchSemantics.Regular ||
                                         next.Kind == ControlFlowBranchSemantics.Return ||
                                         next.Kind == ControlFlowBranchSemantics.Throw ||
                                         next.Kind == ControlFlowBranchSemantics.Rethrow ||
                                         next.Kind == ControlFlowBranchSemantics.Error ||
                                         next.Kind == ControlFlowBranchSemantics.ProgramTermination);

                            Debug.Assert(!block.HasCondition); // This is ensured by an "if" above.
                            IOperation value = block.BranchValue;

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
                                if (!block.HasPredecessors && next.Kind == ControlFlowBranchSemantics.Return)
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
                                    BasicBlockBuilder predecessor = block.GetSingletonPredecessorOrDefault();
                                    if (predecessor == null ||
                                        predecessor.BranchValue != null ||
                                        predecessor.Kind == BasicBlockKind.Entry ||
                                        regionMap[predecessor] != currentRegion)
                                    {
                                        // Do not merge return/throw with expression with more than one predecessor
                                        // Do not merge return/throw into a block with conditional branch
                                        // Do not merge return/throw with expression with an entry block
                                        // Do not merge return/throw with expression into a different region
                                        continue;
                                    }

                                    Debug.Assert(predecessor.FallThrough.Destination == block);
                                }
                            }

                            // For throw/re-throw assume there is no specific destination region
                            RegionBuilder destinationRegionOpt = next.Destination == null ? null : regionMap[next.Destination];

                            if (block.HasPredecessors)
                            {
                                if (predecessorsBuilder == null)
                                {
                                    predecessorsBuilder = ArrayBuilder<BasicBlockBuilder>.GetInstance();
                                }
                                else
                                {
                                    predecessorsBuilder.Clear();
                                }

                                block.GetPredecessors(predecessorsBuilder);

                                // If source and destination are in different regions, it might
                                // be unsafe to merge branches.
                                if (currentRegion != destinationRegionOpt)
                                {
                                    fromCurrent?.Clear();
                                    fromDestination?.Clear();

                                    if (!checkBranchesFromPredecessors(predecessorsBuilder, currentRegion, destinationRegionOpt))
                                    {
                                        continue;
                                    }
                                }

                                foreach (BasicBlockBuilder predecessor in predecessorsBuilder)
                                {
                                    if (tryMergeBranch(predecessor, ref predecessor.FallThrough, block))
                                    {
                                        if (value != null)
                                        {
                                            Debug.Assert(predecessor.BranchValue == null);
                                            predecessor.BranchValue = value;
                                        }
                                    }

                                    if (tryMergeBranch(predecessor, ref predecessor.Conditional, block))
                                    {
                                        Debug.Assert(value == null);
                                    }
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
                        if (next.Kind == ControlFlowBranchSemantics.StructuredExceptionHandling)
                        {
                            continue;
                        }

                        Debug.Assert(next.Kind == ControlFlowBranchSemantics.Regular ||
                                     next.Kind == ControlFlowBranchSemantics.Return ||
                                     next.Kind == ControlFlowBranchSemantics.Throw ||
                                     next.Kind == ControlFlowBranchSemantics.Rethrow ||
                                     next.Kind == ControlFlowBranchSemantics.Error ||
                                     next.Kind == ControlFlowBranchSemantics.ProgramTermination);

                        BasicBlockBuilder predecessor = block.GetSingletonPredecessorOrDefault();

                        if (predecessor == null)
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

                        if (predecessor.Kind != BasicBlockKind.Entry &&
                            predecessor.FallThrough.Destination == block &&
                            !predecessor.HasCondition &&
                            regionMap[predecessor] == currentRegion)
                        {
                            Debug.Assert(predecessor != block);
                            Debug.Assert(predecessor.BranchValue == null);

                            mergeBranch(predecessor, ref predecessor.FallThrough, ref next);

                            next.Destination?.RemovePredecessor(block);

                            predecessor.BranchValue = block.BranchValue;
                            predecessor.ConditionKind = block.ConditionKind;
                            predecessor.Conditional = block.Conditional;
                            BasicBlockBuilder destination = block.Conditional.Destination;
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
            predecessorsBuilder?.Free();

            return anyRemoved;

            RegionBuilder tryGetImplicitEntryRegion(BasicBlockBuilder block, RegionBuilder currentRegion)
            {
                do
                {
                    if (currentRegion.FirstBlock != block)
                    {
                        return null;
                    }

                    switch (currentRegion.Kind)
                    {
                        case ControlFlowRegionKind.Filter:
                        case ControlFlowRegionKind.Catch:
                        case ControlFlowRegionKind.Finally:
                            return currentRegion;
                    }

                    currentRegion = currentRegion.Enclosing;
                }
                while (currentRegion != null);

                return null;
            }

            void removeBlock(BasicBlockBuilder block, RegionBuilder region)
            {
                Debug.Assert(region.FirstBlock.Ordinal >= 0);
                Debug.Assert(region.FirstBlock.Ordinal <= region.LastBlock.Ordinal);
                Debug.Assert(region.FirstBlock.Ordinal <= block.Ordinal);
                Debug.Assert(block.Ordinal <= region.LastBlock.Ordinal);

                if (region.FirstBlock == block)
                {
                    BasicBlockBuilder newFirst = blocks[block.Ordinal + 1];
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
                    BasicBlockBuilder newLast = blocks[block.Ordinal - 1];
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
                block.Free();
            }

            bool tryMergeBranch(BasicBlockBuilder predecessor, ref BasicBlockBuilder.Branch predecessorBranch, BasicBlockBuilder successor)
            {
                if (predecessorBranch.Destination == successor)
                {
                    mergeBranch(predecessor, ref predecessorBranch, ref successor.FallThrough);
                    return true;
                }

                return false;
            }

            void mergeBranch(BasicBlockBuilder predecessor, ref BasicBlockBuilder.Branch predecessorBranch, ref BasicBlockBuilder.Branch successorBranch)
            {
                predecessorBranch.Destination = successorBranch.Destination;
                successorBranch.Destination?.AddPredecessor(predecessor);
                Debug.Assert(predecessorBranch.Kind == ControlFlowBranchSemantics.Regular);
                predecessorBranch.Kind = successorBranch.Kind;
            }

            bool checkBranchesFromPredecessors(ArrayBuilder<BasicBlockBuilder> predecessors, RegionBuilder currentRegion, RegionBuilder destinationRegionOpt)
            {
                foreach (BasicBlockBuilder predecessor in predecessors)
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
        private static void CheckUnresolvedBranches(ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<ILabelSymbol, BasicBlockBuilder> labeledBlocks)
        {
            if (labeledBlocks == null)
            {
                return;
            }

            PooledHashSet<BasicBlockBuilder> unresolved = null;
            foreach (BasicBlockBuilder labeled in labeledBlocks.Values)
            {
                if (labeled.Ordinal == -1)
                {
                    if (unresolved == null)
                    {
                        unresolved = PooledHashSet<BasicBlockBuilder>.GetInstance();
                    }

                    unresolved.Add(labeled);
                }
            }

            if (unresolved == null)
            {
                return;
            }

            // Mark branches using unresolved labels as errors.
            foreach (BasicBlockBuilder block in blocks)
            {
                fixupBranch(ref block.Conditional);
                fixupBranch(ref block.FallThrough);
            }

            unresolved.Free();
            return;

            void fixupBranch(ref BasicBlockBuilder.Branch branch)
            {
                if (branch.Destination != null && unresolved.Contains(branch.Destination))
                {
                    Debug.Assert(branch.Kind == ControlFlowBranchSemantics.Regular);
                    branch.Destination = null;
                    branch.Kind = ControlFlowBranchSemantics.Error;
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

        private BasicBlockBuilder CurrentBasicBlock
        {
            get
            {
                if (_currentBasicBlock == null)
                {
                    AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
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
            Debug.Assert(spillingTheStack || _evalStack.All(
                o => o.Kind == OperationKind.FlowCaptureReference
                    || o.Kind == OperationKind.DeclarationExpression
                    || o.Kind == OperationKind.Discard
                    || o.Kind == OperationKind.OmittedArgument));
#endif
            if (statement == null)
            {
                return;
            }

            Operation.SetParentOperation(statement, null);
            CurrentBasicBlock.AddStatement(statement);
        }

        private void AppendNewBlock(BasicBlockBuilder block, bool linkToPrevious = true)
        {
            Debug.Assert(block != null);

            if (linkToPrevious)
            {
                BasicBlockBuilder prevBlock = _blocks.Last();

                if (prevBlock.FallThrough.Destination == null)
                {
                    LinkBlocks(prevBlock, block);
                }
            }

            if (block.Ordinal != -1)
            {
                throw ExceptionUtilities.Unreachable;
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
                AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
            }

            RegionBuilder enclosed = _currentRegion;
            _currentRegion = _currentRegion.Enclosing;
            _currentRegion?.ExtendToInclude(enclosed.LastBlock);
            _currentBasicBlock = null;
        }

        private static void LinkBlocks(BasicBlockBuilder prevBlock, BasicBlockBuilder nextBlock, ControlFlowBranchSemantics branchKind = ControlFlowBranchSemantics.Regular)
        {
            Debug.Assert(prevBlock.HasCondition || prevBlock.BranchValue == null);
            Debug.Assert(prevBlock.FallThrough.Destination == null);
            prevBlock.FallThrough.Destination = nextBlock;
            prevBlock.FallThrough.Kind = branchKind;
            nextBlock.AddPredecessor(prevBlock);
        }

        public override IOperation VisitBlock(IBlockOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));
            VisitStatements(operation.Operations);
            LeaveRegion();

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

        internal override IOperation VisitWith(IWithOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            int captureId = VisitAndCapture(operation.Value);

            IOperation previousInitializedInstance = _currentImplicitInstance;
            _currentImplicitInstance = GetCaptureReference(captureId, operation.Value);

            VisitStatement(operation.Body);

            _currentImplicitInstance = previousInitializedInstance;
            return null;
        }

        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            if (operation.Initializer != null)
            {
                VisitStatement(operation.Initializer);
            }

            VisitMethodBodyBaseOperation(operation);

            LeaveRegion();
            return null;
        }

        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            VisitMethodBodyBaseOperation(operation);
            return null;
        }

        private void VisitMethodBodyBaseOperation(IMethodBodyBaseOperation operation)
        {
            Debug.Assert(_currentStatement == operation);
            VisitMethodBodies(operation.BlockBody, operation.ExpressionBody);
        }

        private void VisitMethodBodies(IBlockOperation blockBody, IBlockOperation expressionBody)
        {
            if (blockBody != null)
            {
                VisitStatement(blockBody);

                // Check for error case with non-null BlockBody and non-null ExpressionBody.
                if (expressionBody != null)
                {
                    // Link last block of visited BlockBody to the exit block.
                    LinkBlocks(CurrentBasicBlock, _exit);
                    _currentBasicBlock = null;

                    // Generate a special region for unreachable erroneous expression body.
                    EnterRegion(new RegionBuilder(ControlFlowRegionKind.ErroneousBody));
                    VisitStatement(expressionBody);
                    LeaveRegion();
                }
            }
            else if (expressionBody != null)
            {
                VisitStatement(expressionBody);
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

                    BasicBlockBuilder afterIf = null;
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

                    BasicBlockBuilder whenFalse = null;
                    VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                    VisitStatement(operation.WhenTrue);

                    var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
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

                BasicBlockBuilder whenFalse = null;
                VisitConditionalBranch(operation.Condition, ref whenFalse, sense: false);

                VisitAndCapture(operation.WhenTrue, captureId);

                var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterIf);
                _currentBasicBlock = null;

                AppendNewBlock(whenFalse);

                VisitAndCapture(operation.WhenFalse, captureId);

                AppendNewBlock(afterIf);

                return GetCaptureReference(captureId, operation);
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
                saveAvailableCaptureId > (captureId = ((IFlowCaptureReferenceOperation)rewritten).Id.Value))
            {
                captureId = _availableCaptureId++;
                AddStatement(new FlowCapture(captureId, operation.Syntax, rewritten));
            }

            return captureId;
        }

        private void CaptureResultIfNotAlready(SyntaxNode syntax, int captureId, IOperation result)
        {
            if (result.Kind != OperationKind.FlowCaptureReference ||
                captureId != ((IFlowCaptureReferenceOperation)result).Id.Value)
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
                if (operation.Kind != OperationKind.FlowCaptureReference
                    && operation.Kind != OperationKind.DeclarationExpression
                    && operation.Kind != OperationKind.Discard
                    && operation.Kind != OperationKind.OmittedArgument)
                {
                    int captureId = _availableCaptureId++;

                    AddStatement(new FlowCapture(captureId, operation.Syntax, operation)
#if DEBUG
                                 , spillingTheStack: true
#endif
                                );

                    _evalStack[i] = GetCaptureReference(captureId, operation);
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
                                             semanticModel: null, originalArgument.Syntax, IsImplicit(originalArgument));
            }
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, int? captureIdForResult)
        {
            _evalStack.Push(Visit(operation.Target));
            IOperation value = Visit(operation.Value);
            return new SimpleAssignmentExpression(_evalStack.Pop(), operation.IsRef, value, null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, int? captureIdForResult)
        {
            var compoundAssignment = (BaseCompoundAssignmentExpression)operation;
            _evalStack.Push(Visit(compoundAssignment.Target));
            IOperation value = Visit(compoundAssignment.Value);

            return new CompoundAssignmentOperation(_evalStack.Pop(), value, compoundAssignment.InConversionConvertible, compoundAssignment.OutConversionConvertible,
                                                   operation.OperatorKind, operation.IsLifted, operation.IsChecked, operation.OperatorMethod, semanticModel: null,
                                                   operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, int? captureIdForResult)
        {
            _evalStack.Push(Visit(operation.ArrayReference));
            PushArray(operation.Indices);
            ImmutableArray<IOperation> visitedIndices = PopArray(operation.Indices);
            IOperation visitedArrayReference = _evalStack.Pop();
            return new ArrayElementReferenceExpression(visitedArrayReference, visitedIndices, semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

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
                if (operation.OperatorMethod == null)
                {
                    if (ITypeSymbolHelpers.IsBooleanType(operation.Type) &&
                        ITypeSymbolHelpers.IsBooleanType(operation.LeftOperand.Type) &&
                        ITypeSymbolHelpers.IsBooleanType(operation.RightOperand.Type))
                    {
                        // Regular boolean logic
                        return VisitBinaryConditionalOperator(operation, sense: true, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
                    }
                    else if (operation.IsLifted &&
                             ITypeSymbolHelpers.IsNullableOfBoolean(operation.Type) &&
                             ITypeSymbolHelpers.IsNullableOfBoolean(operation.LeftOperand.Type) &&
                             ITypeSymbolHelpers.IsNullableOfBoolean(operation.RightOperand.Type))
                    {
                        // Three-value boolean logic (VB).
                        return VisitNullableBinaryConditionalOperator(operation, captureIdForResult);
                    }
                    else if (ITypeSymbolHelpers.IsObjectType(operation.Type) &&
                             ITypeSymbolHelpers.IsObjectType(operation.LeftOperand.Type) &&
                             ITypeSymbolHelpers.IsObjectType(operation.RightOperand.Type))
                    {
                        return VisitObjectBinaryConditionalOperator(operation, captureIdForResult);
                    }
                    else if (ITypeSymbolHelpers.IsDynamicType(operation.Type) &&
                             (ITypeSymbolHelpers.IsDynamicType(operation.LeftOperand.Type) ||
                             ITypeSymbolHelpers.IsDynamicType(operation.RightOperand.Type)))
                    {
                        return VisitDynamicBinaryConditionalOperator(operation, captureIdForResult);
                    }
                }
                else
                {
                    return VisitUserDefinedBinaryConditionalOperator(operation, captureIdForResult);
                }
            }

            _evalStack.Push(Visit(operation.LeftOperand));
            IOperation rightOperand = Visit(operation.RightOperand);
            return new BinaryOperatorExpression(operation.OperatorKind, _evalStack.Pop(), rightOperand, operation.IsLifted, operation.IsChecked, operation.IsCompareText,
                                                operation.OperatorMethod, ((BaseBinaryOperatorExpression)operation).UnaryOperatorMethod, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, int? captureIdForResult)
        {
            (IOperation visitedLeft, IOperation visitedRight) = VisitPreservingTupleOperations(operation.LeftOperand, operation.RightOperand);
            return new TupleBinaryOperatorExpression(operation.OperatorKind, visitedLeft, visitedRight,
                semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): ensure we properly detect logical Not
            //                      For example, Microsoft.CodeAnalysis.CSharp.UnitTests.IOperationTests.Test_UnaryOperatorExpression_Type_LogicalNot_dynamic
            if (IsBooleanLogicalNot(operation))
            {
                return VisitConditionalExpression(operation.Operand, sense: false, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
            }

            return new UnaryOperatorExpression(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.OperatorMethod,
                                               semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        private static bool IsBooleanLogicalNot(IUnaryOperation operation)
        {
            return operation.OperatorKind == UnaryOperatorKind.Not &&
                   operation.OperatorMethod == null &&
                   ITypeSymbolHelpers.IsBooleanType(operation.Type) &&
                   ITypeSymbolHelpers.IsBooleanType(operation.Operand.Type);
        }

        private static bool CalculateAndOrSense(IBinaryOperation binOp, bool sense)
        {
            switch (binOp.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                    // Rewrite (a || b) as ~(~a && ~b)
                    return !sense;

                case BinaryOperatorKind.ConditionalAnd:
                    return sense;

                default:
                    throw ExceptionUtilities.UnexpectedValue(binOp.OperatorKind);
            }
        }

        private IOperation VisitBinaryConditionalOperator(IBinaryOperation binOp, bool sense, int? captureIdForResult,
                                                          BasicBlockBuilder fallToTrueOpt, BasicBlockBuilder fallToFalseOpt)
        {
            // ~(a && b) is equivalent to (~a || ~b)
            if (!CalculateAndOrSense(binOp, sense))
            {
                // generate (~a || ~b)
                return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: sense, stopValue: true, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
            }
            else
            {
                // generate (a && b)
                return VisitShortCircuitingOperator(binOp, sense: sense, stopSense: !sense, stopValue: false, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
            }
        }

        private IOperation VisitNullableBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            Compilation compilation = ((Operation)binOp).SemanticModel.Compilation;
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IOperation condition;

            bool isAndAlso = CalculateAndOrSense(binOp, true);

            // case BinaryOperatorKind.ConditionalOr:
            //        Dim result As Boolean?
            //
            //        If left.GetValueOrDefault Then
            //            result = left
            //            GoTo done
            //        End If
            //
            //        If Not ((Not right).GetValueOrDefault) Then
            //            result = right
            //            GoTo done
            //        End If
            //
            //        result = left
            //done:
            //        Return result

            // case BinaryOperatorKind.ConditionalAnd:
            //        Dim result As Boolean?
            //
            //        If (Not left).GetValueOrDefault() Then
            //            result = left
            //            GoTo done
            //        End If
            //
            //        If Not (right.GetValueOrDefault()) Then
            //            result = right
            //            GoTo done
            //        End If
            //
            //        result = left
            //
            //done:
            //        Return result

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var checkRight = new BasicBlockBuilder(BasicBlockKind.Block);
            var resultIsLeft = new BasicBlockBuilder(BasicBlockKind.Block);

            int leftId = VisitAndCapture(left);
            condition = GetCaptureReference(leftId, left);

            if (isAndAlso)
            {
                condition = negateNullable(condition);
            }

            condition = UnwrapNullableValue(condition, compilation);
            LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(checkRight));
            _currentBasicBlock = null;

            int resultId = captureIdForResult ?? _availableCaptureId++;
            AddStatement(new FlowCapture(resultId, binOp.Syntax, GetCaptureReference(leftId, left)));
            LinkBlocks(CurrentBasicBlock, done);
            _currentBasicBlock = null;

            AppendNewBlock(checkRight);

            int rightId = VisitAndCapture(right);
            condition = GetCaptureReference(rightId, right);

            if (!isAndAlso)
            {
                condition = negateNullable(condition);
            }

            condition = UnwrapNullableValue(condition, compilation);
            LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: true, RegularBranch(resultIsLeft));
            _currentBasicBlock = null;

            AddStatement(new FlowCapture(resultId, binOp.Syntax, GetCaptureReference(rightId, right)));
            LinkBlocks(CurrentBasicBlock, done);
            _currentBasicBlock = null;

            AppendNewBlock(resultIsLeft);
            AddStatement(new FlowCapture(resultId, binOp.Syntax, GetCaptureReference(leftId, left)));

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);

            IOperation negateNullable(IOperation operand)
            {
                return new UnaryOperatorExpression(UnaryOperatorKind.Not, operand, isLifted: true, isChecked: false, operatorMethod: null,
                                                   semanticModel: null, operand.Syntax, operand.Type, constantValue: default, isImplicit: true);

            }
        }

        private IOperation VisitObjectBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            Compilation compilation = ((Operation)binOp).SemanticModel.Compilation;
            INamedTypeSymbol booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IOperation condition;

            bool isAndAlso = CalculateAndOrSense(binOp, true);

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var checkRight = new BasicBlockBuilder(BasicBlockKind.Block);

            condition = CreateConversion(compilation, Visit(left), booleanType);

            LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: isAndAlso, RegularBranch(checkRight));
            _currentBasicBlock = null;

            int resultId = _availableCaptureId++;
            AddStatement(new FlowCapture(resultId, binOp.Syntax, new LiteralExpression(semanticModel: null, left.Syntax, booleanType, constantValue: !isAndAlso, isImplicit: true)));
            LinkBlocks(CurrentBasicBlock, done);
            _currentBasicBlock = null;

            AppendNewBlock(checkRight);

            condition = CreateConversion(compilation, Visit(right), booleanType);

            AddStatement(new FlowCapture(resultId, binOp.Syntax, condition));

            AppendNewBlock(done);

            condition = new FlowCaptureReference(resultId, binOp.Syntax, booleanType, constantValue: default);
            return new ConversionOperation(condition, compilation.ClassifyConvertibleConversion(condition, binOp.Type, out _), isTryCast: false, isChecked: false,
                                           semanticModel: null, binOp.Syntax, binOp.Type, binOp.ConstantValue, isImplicit: true);
        }

        private static IOperation CreateConversion(Compilation compilation, IOperation operand, ITypeSymbol type)
        {
            return new ConversionOperation(operand, compilation.ClassifyConvertibleConversion(operand, type, out Optional<object> constantValue), isTryCast: false, isChecked: false,
                                           semanticModel: null, operand.Syntax, type, constantValue, isImplicit: true);
        }

        private IOperation VisitDynamicBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            Compilation compilation = ((Operation)binOp).SemanticModel.Compilation;
            INamedTypeSymbol booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol unaryOperatorMethod = ((BaseBinaryOperatorExpression)binOp).UnaryOperatorMethod;
            bool isAndAlso = CalculateAndOrSense(binOp, true);
            bool jumpIfTrue;
            IOperation condition;

            // Dynamic logical && and || operators are lowered as follows:
            //   left && right  ->  IsFalse(left) ? left : And(left, right)
            //   left || right  ->  IsTrue(left) ? left : Or(left, right)

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var doBitWise = new BasicBlockBuilder(BasicBlockKind.Block);

            int leftId = VisitAndCapture(left);

            condition = GetCaptureReference(leftId, left);

            if (ITypeSymbolHelpers.IsBooleanType(left.Type))
            {
                Debug.Assert(unaryOperatorMethod == null);
                jumpIfTrue = isAndAlso;
            }
            else if (ITypeSymbolHelpers.IsDynamicType(left.Type) || unaryOperatorMethod != null)
            {
                jumpIfTrue = false;

                if (unaryOperatorMethod == null || 
                    (ITypeSymbolHelpers.IsBooleanType(unaryOperatorMethod.ReturnType) &&
                     (ITypeSymbolHelpers.IsNullableType(left.Type) || !ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type))))
                {
                    condition = new UnaryOperatorExpression(isAndAlso ? UnaryOperatorKind.False : UnaryOperatorKind.True,
                                                            condition, isLifted: false, isChecked: false, operatorMethod: unaryOperatorMethod,
                                                            semanticModel: null, condition.Syntax, booleanType, constantValue: default, isImplicit: true);
                }
                else
                {
                    condition = MakeInvalidOperation(booleanType, condition);
                }
            }
            else
            {
                // This is either an error case, or left is implicitly convertible to boolean
                condition = CreateConversion(compilation, condition, booleanType);
                jumpIfTrue = isAndAlso;
            }

            LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue, RegularBranch(doBitWise));
            _currentBasicBlock = null;

            int resultId = captureIdForResult ?? _availableCaptureId++;
            IOperation resultFromLeft = GetCaptureReference(leftId, left);

            if (!ITypeSymbolHelpers.IsDynamicType(left.Type))
            {
                resultFromLeft = CreateConversion(compilation, resultFromLeft, binOp.Type);
            }

            AddStatement(new FlowCapture(resultId, binOp.Syntax, resultFromLeft));
            LinkBlocks(CurrentBasicBlock, done);
            _currentBasicBlock = null;

            AppendNewBlock(doBitWise);

            AddStatement(new FlowCapture(resultId, binOp.Syntax,
                                         new BinaryOperatorExpression(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                                      GetCaptureReference(leftId, left),
                                                                      Visit(right),
                                                                      isLifted: false,
                                                                      binOp.IsChecked,
                                                                      binOp.IsCompareText,
                                                                      binOp.OperatorMethod,
                                                                      unaryOperatorMethod: null,
                                                                      semanticModel: null,
                                                                      binOp.Syntax,
                                                                      binOp.Type,
                                                                      binOp.ConstantValue, IsImplicit(binOp))));

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);
        }

        private IOperation VisitUserDefinedBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            Compilation compilation = ((Operation)binOp).SemanticModel.Compilation;
            INamedTypeSymbol booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            bool isLifted = binOp.IsLifted;
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol unaryOperatorMethod = ((BaseBinaryOperatorExpression)binOp).UnaryOperatorMethod;
            bool isAndAlso = CalculateAndOrSense(binOp, true);
            IOperation condition;

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var doBitWise = new BasicBlockBuilder(BasicBlockKind.Block);

            int leftId = VisitAndCapture(left);

            condition = GetCaptureReference(leftId, left);

            if (ITypeSymbolHelpers.IsNullableType(left.Type))
            {
                if (unaryOperatorMethod == null ? isLifted : !ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type))
                {
                    condition = MakeIsNullOperation(condition, booleanType);
                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: true, RegularBranch(doBitWise));
                    _currentBasicBlock = null;

                    Debug.Assert(unaryOperatorMethod == null || !ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type));
                    condition = UnwrapNullableValue(GetCaptureReference(leftId, left), compilation);
                }
            }
            else if (unaryOperatorMethod != null && ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type))
            {
                condition = MakeInvalidOperation(unaryOperatorMethod.Parameters[0].Type, condition);
            }

            if (unaryOperatorMethod != null && ITypeSymbolHelpers.IsBooleanType(unaryOperatorMethod.ReturnType))
            {
                condition = new UnaryOperatorExpression(isAndAlso ? UnaryOperatorKind.False : UnaryOperatorKind.True,
                                                        condition, isLifted: false, isChecked: false, operatorMethod: unaryOperatorMethod,
                                                        semanticModel: null, condition.Syntax, unaryOperatorMethod.ReturnType, constantValue: default, isImplicit: true);
            }
            else
            {
                condition = MakeInvalidOperation(booleanType, condition);
            }

            LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(doBitWise));
            _currentBasicBlock = null;

            int resultId = captureIdForResult ?? _availableCaptureId++;
            AddStatement(new FlowCapture(resultId, binOp.Syntax, GetCaptureReference(leftId, left)));
            LinkBlocks(CurrentBasicBlock, done);
            _currentBasicBlock = null;

            AppendNewBlock(doBitWise);

            AddStatement(new FlowCapture(resultId, binOp.Syntax,
                                         new BinaryOperatorExpression(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                                      GetCaptureReference(leftId, left), 
                                                                      Visit(right), 
                                                                      isLifted, 
                                                                      binOp.IsChecked, 
                                                                      binOp.IsCompareText,
                                                                      binOp.OperatorMethod, 
                                                                      unaryOperatorMethod: null, 
                                                                      semanticModel: null,
                                                                      binOp.Syntax, 
                                                                      binOp.Type, 
                                                                      binOp.ConstantValue, IsImplicit(binOp))));

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);
        }

        private IOperation VisitShortCircuitingOperator(IBinaryOperation condition, bool sense, bool stopSense, bool stopValue,
                                                        int? captureIdForResult, BasicBlockBuilder fallToTrueOpt, BasicBlockBuilder fallToFalseOpt)
        {
            Debug.Assert(IsBooleanConditionalOperator(condition));
                    
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

            ref BasicBlockBuilder lazyFallThrough = ref stopSense ? ref fallToTrueOpt : ref fallToFalseOpt;
            bool newFallThroughBlock = (lazyFallThrough == null);

            VisitConditionalBranch(condition.LeftOperand, ref lazyFallThrough, stopSense);

            IOperation resultFromRight = VisitConditionalExpression(condition.RightOperand, sense, captureId, fallToTrueOpt, fallToFalseOpt);

            CaptureResultIfNotAlready(condition.RightOperand.Syntax, captureId, resultFromRight);

            if (newFallThroughBlock)
            {
                var labEnd = new BasicBlockBuilder(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, labEnd);
                _currentBasicBlock = null;

                AppendNewBlock(lazyFallThrough);

                var constantValue = new Optional<object>(stopValue);
                SyntaxNode leftSyntax = (lazyFallThrough.GetSingletonPredecessorOrDefault() != null ? condition.LeftOperand : condition).Syntax;
                AddStatement(new FlowCapture(captureId, leftSyntax, new LiteralExpression(semanticModel: null, leftSyntax, condition.Type, constantValue, isImplicit: true)));

                AppendNewBlock(labEnd);
            }

            return GetCaptureReference(captureId, condition);
        }

        private IOperation VisitConditionalExpression(IOperation condition, bool sense, int? captureIdForResult, BasicBlockBuilder fallToTrueOpt, BasicBlockBuilder fallToFalseOpt)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(condition.Type));

            // PROTOTYPE(dataflow): Unwrap parenthesized and look for other places where it should happen
            // PROTOTYPE(dataflow): Do not erase UnaryOperatorKind.Not if ProduceIsSense below will have to add it back.

            while (condition.Kind == OperationKind.UnaryOperator)
            {
                var unOp = (IUnaryOperation)condition;
                if (!IsBooleanLogicalNot(unOp))
                {
                    break;
                }
                condition = unOp.Operand;
                sense = !sense;
            }

            if (condition.Kind == OperationKind.BinaryOperator)
            {
                var binOp = (IBinaryOperation)condition;
                if (IsBooleanConditionalOperator(binOp))
                {
                    return VisitBinaryConditionalOperator(binOp, sense, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
                }
            }

            return ProduceIsSense(Visit(condition), sense);
        }

        private static bool IsBooleanConditionalOperator(IBinaryOperation binOp)
        {
            return IsConditional(binOp) &&
                   binOp.OperatorMethod == null &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.Type) &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.LeftOperand.Type) &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.RightOperand.Type);
        }

        private IOperation ProduceIsSense(IOperation condition, bool sense)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(condition.Type));

            if (!sense)
            {
                return new UnaryOperatorExpression(UnaryOperatorKind.Not,
                                                   condition,
                                                   isLifted: false,
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

        private void VisitConditionalBranch(IOperation condition, ref BasicBlockBuilder dest, bool sense)
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

                    if (IsBooleanConditionalOperator(binOp))
                    {
                        if (CalculateAndOrSense(binOp, sense))
                        {
                            // gotoif(LeftOperand != sense) fallThrough
                            // gotoif(RightOperand == sense) dest
                            // fallThrough:

                            BasicBlockBuilder fallThrough = null;

                            VisitConditionalBranch(binOp.LeftOperand, ref fallThrough, !sense);
                            VisitConditionalBranch(binOp.RightOperand, ref dest, sense);
                            AppendNewBlock(fallThrough);
                            return;
                        }
                        else
                        {
                            // gotoif(LeftOperand == sense) dest
                            // gotoif(RightOperand == sense) dest

                            VisitConditionalBranch(binOp.LeftOperand, ref dest, sense);
                            condition = binOp.RightOperand;
                            goto oneMoreTime;
                        }
                    }

                    // none of above. 
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

                case OperationKind.UnaryOperator:
                    var unOp = (IUnaryOperation)condition;

                    if (IsBooleanLogicalNot(unOp))
                    {
                        sense = !sense;
                        condition = unOp.Operand;
                        goto oneMoreTime;
                    }
                    goto default;

                case OperationKind.Conditional:
                    if (ITypeSymbolHelpers.IsBooleanType(condition.Type))
                    {
                        var conditional = (IConditionalOperation)condition;

                        if (ITypeSymbolHelpers.IsBooleanType(conditional.WhenTrue.Type) &&
                            ITypeSymbolHelpers.IsBooleanType(conditional.WhenFalse.Type))
                        {
                            BasicBlockBuilder whenFalse = null;
                            VisitConditionalBranch(conditional.Condition, ref whenFalse, sense: false);
                            VisitConditionalBranch(conditional.WhenTrue, ref dest, sense);

                            var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                            LinkBlocks(CurrentBasicBlock, afterIf);
                            _currentBasicBlock = null;

                            AppendNewBlock(whenFalse);
                            VisitConditionalBranch(conditional.WhenFalse, ref dest, sense);
                            AppendNewBlock(afterIf);

                            return;
                        }
                    }
                    goto default;

                case OperationKind.Coalesce:
                    if (ITypeSymbolHelpers.IsBooleanType(condition.Type))
                    {
                        var coalesce = (ICoalesceOperation)condition;

                        if (ITypeSymbolHelpers.IsBooleanType(coalesce.WhenNull.Type))
                        {
                            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
                            IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(coalesce, whenNull);

                            convertedTestExpression = Operation.SetParentOperation(convertedTestExpression, null);
                            dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                            LinkBlocks(CurrentBasicBlock, convertedTestExpression, sense, RegularBranch(dest));
                            _currentBasicBlock = null;

                            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);
                            LinkBlocks(CurrentBasicBlock, afterCoalesce);
                            _currentBasicBlock = null;

                            AppendNewBlock(whenNull);
                            VisitConditionalBranch(coalesce.WhenNull, ref dest, sense);

                            AppendNewBlock(afterCoalesce);

                            return;
                        }
                    }
                    goto default;

                case OperationKind.Conversion:
                    var conversion = (IConversionOperation)condition;

                    if (conversion.Operand.Kind == OperationKind.Throw)
                    {
                        Visit(conversion.Operand);
                        dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                        return;
                    }
                    goto default;

                default:
                    condition = Operation.SetParentOperation(Visit(condition), null);
                    dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, condition, sense, RegularBranch(dest));
                    _currentBasicBlock = null;
                    return;
            }
        }

        private static void LinkBlocks(BasicBlockBuilder previous, IOperation condition, bool jumpIfTrue, BasicBlockBuilder.Branch branch)
        {
            Debug.Assert(previous.BranchValue == null);
            Debug.Assert(!previous.HasCondition);
            Debug.Assert(condition != null);
            Debug.Assert(condition.Parent == null);
            branch.Destination.AddPredecessor(previous);
            previous.BranchValue = condition;
            previous.ConditionKind = jumpIfTrue ? ControlFlowConditionKind.WhenTrue : ControlFlowConditionKind.WhenFalse;
            previous.Conditional = branch;
        }

        /// <summary>
        /// Returns converted test expression
        /// </summary>
        private IOperation NullCheckAndConvertCoalesceValue(ICoalesceOperation operation, BasicBlockBuilder whenNull)
        {
            IOperation operationValue = operation.Value;
            SyntaxNode valueSyntax = operationValue.Syntax;
            ITypeSymbol valueTypeOpt = operationValue.Type;

            SpillEvalStack();
            int testExpressionCaptureId = VisitAndCapture(operationValue);

            Optional<object> constantValue = operationValue.ConstantValue;

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            LinkBlocks(CurrentBasicBlock,
                       Operation.SetParentOperation(MakeIsNullOperation(GetCaptureReference(testExpressionCaptureId, operationValue),
                                                                        compilation),
                                                    null),
                       jumpIfTrue: true,
                       RegularBranch(whenNull));
            _currentBasicBlock = null;

            CommonConversion testConversion = operation.ValueConversion;
            FlowCaptureReference capturedValue = GetCaptureReference(testExpressionCaptureId, operationValue);
            IOperation convertedTestExpression = null;

            if (testConversion.Exists)
            {
                IOperation possiblyUnwrappedValue;

                if (ITypeSymbolHelpers.IsNullableType(valueTypeOpt) &&
                    (!testConversion.IsIdentity || !ITypeSymbolHelpers.IsNullableType(operation.Type)))
                {
                    possiblyUnwrappedValue = TryUnwrapNullableValue(capturedValue, compilation);
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
            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
            IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(operation, whenNull);

            int resultCaptureId = captureIdForResult ?? _availableCaptureId++;

            AddStatement(new FlowCapture(resultCaptureId, operation.Value.Syntax, convertedTestExpression));

            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);
            LinkBlocks(CurrentBasicBlock, afterCoalesce);
            _currentBasicBlock = null;

            AppendNewBlock(whenNull);

            VisitAndCapture(operation.WhenNull, resultCaptureId);

            AppendNewBlock(afterCoalesce);

            return GetCaptureReference(resultCaptureId, operation);
        }

        private static BasicBlockBuilder.Branch RegularBranch(BasicBlockBuilder destination)
        {
            return new BasicBlockBuilder.Branch() { Destination = destination, Kind = ControlFlowBranchSemantics.Regular };
        }

        private static IOperation MakeInvalidOperation(ITypeSymbol type, IOperation child)
        {
            return new InvalidOperation(ImmutableArray.Create<IOperation>(child),
                                        semanticModel: null, child.Syntax, type,
                                        constantValue: default, isImplicit: true);
        }

        private static IOperation MakeInvalidOperation(SyntaxNode syntax, ITypeSymbol type, IOperation child1, IOperation child2)
        {
            return MakeInvalidOperation(syntax, type, ImmutableArray.Create<IOperation>(child1, child2));
        }

        private static IOperation MakeInvalidOperation(SyntaxNode syntax, ITypeSymbol type, ImmutableArray<IOperation> children)
        {
            return new InvalidOperation(children,
                                        semanticModel: null, syntax, type,
                                        constantValue: default, isImplicit: true);
        }

        private static IsNullOperation MakeIsNullOperation(IOperation operand, Compilation compilation)
        {
            return MakeIsNullOperation(operand, compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private static IsNullOperation MakeIsNullOperation(IOperation operand, ITypeSymbol booleanType)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(booleanType));
            Optional<object> constantValue = operand.ConstantValue;
            return new IsNullOperation(operand.Syntax, operand,
                                       booleanType,
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

        private static IOperation UnwrapNullableValue(IOperation value, Compilation compilation)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(value.Type));
            return TryUnwrapNullableValue(value, compilation) ??
                   MakeInvalidOperation(ITypeSymbolHelpers.GetNullableUnderlyingType(value.Type), value);
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

            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);

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
                           Operation.SetParentOperation(MakeIsNullOperation(GetCaptureReference(testExpressionCaptureId, testExpression),
                                                                            compilation),
                                                        null),
                           jumpIfTrue: true,
                           RegularBranch(whenNull));
                _currentBasicBlock = null;

                IOperation receiver = GetCaptureReference(testExpressionCaptureId, testExpression);

                if (ITypeSymbolHelpers.IsNullableType(testExpressionType))
                {
                    receiver = UnwrapNullableValue(receiver, compilation);
                }

                // PROTOTYPE(dataflow): It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
                //                      a None operation is created and all children are dropped.
                //                      See Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics.ConditionalAccessTests.AnonymousTypeMemberName_01
                //                      The following assert is triggered because of that. Disabling it for now.
                //Debug.Assert(_currentConditionalAccessInstance == null);
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
                // PROTOTYPE(dataflow): It looks like there is a bug in IOperation tree,
                //                      See Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics.ConditionalAccessTests.ConditionalAccessToEvent_04
                //                      The following assert is triggered because of that. Disabling it for now.
                //Debug.Assert(_currentConditionalAccessInstance == null);
                _currentConditionalAccessInstance = null;

                if (_currentStatement != operation)
                {
                    var expressionStatement = (IExpressionStatementOperation)_currentStatement;
                    result = new ExpressionStatement(result, semanticModel: null, expressionStatement.Syntax,
                                                     expressionStatement.Type, expressionStatement.ConstantValue,
                                                     IsImplicit(expressionStatement));
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
                        MakeNullable(compilation, access, operation.Type)));
                }
                else
                {
                    VisitAndCapture(currentConditionalAccess.WhenNotNull, resultCaptureId);
                }

                // PROTOTYPE(dataflow): It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
                //                      a None operation is created and all children are dropped.
                //                      See Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests.ExpressionCompilerTests.ConditionalAccessExpressionType
                //                      The following assert is triggered because of that. Disabling it for now.
                //Debug.Assert(_currentConditionalAccessInstance == null);
                _currentConditionalAccessInstance = null;

                var afterAccess = new BasicBlockBuilder(BasicBlockKind.Block);
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

                return GetCaptureReference(resultCaptureId, operation);
            }
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
            else if (operation.Operation.Kind == OperationKind.Throw)
            {
                return null;
            }

            return new ExpressionStatement(underlying, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            var locals = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals);

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
                EnterRegion(locals);

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

                var start = new BasicBlockBuilder(BasicBlockKind.Block);
                AppendNewBlock(start);
                EnterRegion(locals);

                VisitStatement(operation.Body);

                AppendNewBlock(@continue);

                if (operation.Condition != null)
                {
                    VisitConditionalBranch(operation.Condition, ref start, sense: !operation.ConditionIsUntil);
                }
                else
                {
                    LinkBlocks(CurrentBasicBlock, start);
                    _currentBasicBlock = null;
                }
            }

            Debug.Assert(_currentRegion == locals);
            LeaveRegion();

            AppendNewBlock(@break);
            return null;
        }

        public override IOperation VisitTry(ITryOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            var afterTryCatchFinally = GetLabeledOrNewBlock(operation.ExitLabel);

            if (operation.Catches.IsEmpty && operation.Finally == null)
            {
                // Malformed node without handlers
                // It looks like we can get here for VB only. Let's recover the same way C# does, i.e.
                // pretend that there is no Try. Just visit body.
                VisitStatement(operation.Body);
                AppendNewBlock(afterTryCatchFinally);
                return null;
            }

            RegionBuilder tryAndFinallyRegion = null;
            bool haveFinally = operation.Finally != null;
            if (haveFinally)
            {
                tryAndFinallyRegion = new RegionBuilder(ControlFlowRegionKind.TryAndFinally);
                EnterRegion(tryAndFinallyRegion);
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));
            }

            bool haveCatches = !operation.Catches.IsEmpty;
            if (haveCatches)
            {
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndCatch));
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));
            }

            VisitStatement(operation.Body);
            LinkBlocks(CurrentBasicBlock, afterTryCatchFinally);

            if (haveCatches)
            {
                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                LeaveRegion();

                foreach (ICatchClauseOperation catchClause in operation.Catches)
                {
                    RegionBuilder filterAndHandlerRegion = null;

                    IOperation exceptionDeclarationOrExpression = catchClause.ExceptionDeclarationOrExpression;
                    IOperation filter = catchClause.Filter;
                    bool haveFilter = filter != null;
                    var catchBlock = new BasicBlockBuilder(BasicBlockKind.Block);

                    if (haveFilter)
                    {
                        filterAndHandlerRegion = new RegionBuilder(ControlFlowRegionKind.FilterAndHandler, catchClause.ExceptionType, catchClause.Locals);
                        EnterRegion(filterAndHandlerRegion);

                        var filterRegion = new RegionBuilder(ControlFlowRegionKind.Filter, catchClause.ExceptionType);
                        EnterRegion(filterRegion);

                        AddExceptionStore(catchClause.ExceptionType, exceptionDeclarationOrExpression);

                        VisitConditionalBranch(filter, ref catchBlock, sense: true);
                        var continueDispatchBlock = new BasicBlockBuilder(BasicBlockKind.Block);
                        AppendNewBlock(continueDispatchBlock);
                        continueDispatchBlock.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;
                        LeaveRegion();

                        Debug.Assert(filterRegion.LastBlock.FallThrough.Destination == null);
                        Debug.Assert(!filterRegion.FirstBlock.HasPredecessors);
                    }

                    var handlerRegion = new RegionBuilder(ControlFlowRegionKind.Catch, catchClause.ExceptionType,
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
#if DEBUG
                        Debug.Assert(filterAndHandlerRegion.Regions[0].LastBlock.FallThrough.Destination == null);
                        if (handlerRegion.FirstBlock.HasPredecessors)
                        {
                            var predecessors = ArrayBuilder<BasicBlockBuilder>.GetInstance();
                            handlerRegion.FirstBlock.GetPredecessors(predecessors);
                            Debug.Assert(predecessors.All(p => filterAndHandlerRegion.Regions[0].FirstBlock.Ordinal <= p.Ordinal &&
                                                          filterAndHandlerRegion.Regions[0].LastBlock.Ordinal >= p.Ordinal));
                            predecessors.Free();
                        }
#endif 
                    }
                    else
                    {
                        Debug.Assert(!handlerRegion.FirstBlock.HasPredecessors);
                    }
                }

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndCatch);
                LeaveRegion();
            }

            if (haveFinally)
            {
                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                LeaveRegion();

                var finallyRegion = new RegionBuilder(ControlFlowRegionKind.Finally);
                EnterRegion(finallyRegion);
                AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
                VisitStatement(operation.Finally);
                var continueDispatchBlock = new BasicBlockBuilder(BasicBlockKind.Block);
                AppendNewBlock(continueDispatchBlock);
                continueDispatchBlock.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;
                LeaveRegion();
                Debug.Assert(_currentRegion == tryAndFinallyRegion);
                LeaveRegion();
                Debug.Assert(finallyRegion.LastBlock.FallThrough.Destination == null);
                Debug.Assert(!finallyRegion.FirstBlock.HasPredecessors);
            }

            AppendNewBlock(afterTryCatchFinally, linkToPrevious: false);
            Debug.Assert(tryAndFinallyRegion?.Regions[1].LastBlock.FallThrough.Destination == null);

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
                    AddStatement(new ReturnStatement(OperationKind.YieldReturn, returnedValue, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
                    break;

                case OperationKind.YieldBreak:
                case OperationKind.Return:
                    BasicBlockBuilder current = CurrentBasicBlock;
                    LinkBlocks(CurrentBasicBlock, _exit, returnedValue is null ? ControlFlowBranchSemantics.Regular : ControlFlowBranchSemantics.Return);
                    Debug.Assert(current.BranchValue == null);
                    Debug.Assert(!current.HasCondition);
                    current.BranchValue = Operation.SetParentOperation(returnedValue, null);
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

            BasicBlockBuilder labeled = GetLabeledOrNewBlock(operation.Label);

            if (labeled.Ordinal != -1)
            {
                // Must be a duplicate label. Recover by simply allocating a new block.
                labeled = new BasicBlockBuilder(BasicBlockKind.Block);
            }

            AppendNewBlock(labeled);
            VisitStatement(operation.Operation);
            return null;
        }

        private BasicBlockBuilder GetLabeledOrNewBlock(ILabelSymbol labelOpt)
        {
            if (labelOpt == null)
            {
                return new BasicBlockBuilder(BasicBlockKind.Block);
            }

            BasicBlockBuilder labeledBlock;

            if (_labeledBlocks == null)
            {
                _labeledBlocks = PooledDictionary<ILabelSymbol, BasicBlockBuilder>.GetInstance();
            }
            else if (_labeledBlocks.TryGetValue(labelOpt, out labeledBlock))
            {
                return labeledBlock;
            }

            labeledBlock = new BasicBlockBuilder(BasicBlockKind.Block);
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

            BasicBlockBuilder current = CurrentBasicBlock;
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block), linkToPrevious: false);
            Debug.Assert(current.BranchValue == null);
            Debug.Assert(!current.HasCondition);
            Debug.Assert(current.FallThrough.Destination == null);
            Debug.Assert(current.FallThrough.Kind == ControlFlowBranchSemantics.None);
            current.BranchValue = exception;
            current.FallThrough.Kind = operation.Exception == null ? ControlFlowBranchSemantics.Rethrow : ControlFlowBranchSemantics.Throw;

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

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            if (operation.Resources.Kind == OperationKind.VariableDeclarationGroup)
            {
                var declarationGroup = (IVariableDeclarationGroupOperation)operation.Resources;
                var resourceQueue = ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)>.GetInstance(declarationGroup.Declarations.Length);
                
                foreach (IVariableDeclarationOperation declaration in declarationGroup.Declarations)
                {
                    foreach (IVariableDeclaratorOperation declarator in declaration.Declarators)
                    {
                        resourceQueue.Add((declaration, declarator));
                    }
                }

                resourceQueue.ReverseContents();
                
                processQueue(resourceQueue);
            }
            else
            {
                Debug.Assert(operation.Resources.Kind != OperationKind.VariableDeclaration);
                Debug.Assert(operation.Resources.Kind != OperationKind.VariableDeclarator);

                IOperation resource = Visit(operation.Resources);
                int captureId = _availableCaptureId++;

                if (shouldConvertToIDisposableBeforeTry(resource))
                {
                    resource = ConvertToIDisposable(compilation, resource, iDisposable);
                }

                AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                processResource(GetCaptureReference(captureId, resource), resourceQueueOpt: null);
            }

            LeaveRegion();
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
                    resource = ConvertToIDisposable(compilation, resource, iDisposable);
                    int captureId = _availableCaptureId++;
                    AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                    resource = GetCaptureReference(captureId, resource);
                }

                var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);

                EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));

                processQueue(resourceQueueOpt);

                LinkBlocks(CurrentBasicBlock, afterTryFinally);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                LeaveRegion();

                AddDisposingFinally(resource, knownToImplementIDisposable: true, iDisposable, compilation);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
                LeaveRegion();

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }
        }

        private void AddDisposingFinally(IOperation resource, bool knownToImplementIDisposable, ITypeSymbol iDisposable, Compilation compilation)
        {
            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Finally));
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (!knownToImplementIDisposable)
            {
                Debug.Assert(!isNotNullableValueType(resource.Type));
                resource = ConvertToIDisposable(compilation, resource, iDisposable, isTryCast: true);
                int captureId = _availableCaptureId++;
                AddStatement(new FlowCapture(captureId, resource.Syntax, resource));
                resource = GetCaptureReference(captureId, resource);
            }

            if (!knownToImplementIDisposable || !isNotNullableValueType(resource.Type))
            {
                IOperation condition = MakeIsNullOperation(OperationCloner.CloneOperation(resource), compilation);
                condition = Operation.SetParentOperation(condition, null);
                LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: true, RegularBranch(endOfFinally));
                _currentBasicBlock = null;
            }

            if (!resource.Type.Equals(iDisposable))
            {
                resource = ConvertToIDisposable(compilation, resource, iDisposable);
            }

            AddStatement(tryDispose(resource) ??
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

        private static IOperation ConvertToIDisposable(Compilation compilation, IOperation operand, ITypeSymbol iDisposable, bool isTryCast = false)
        {
            Debug.Assert(iDisposable.SpecialType == SpecialType.System_IDisposable);
            return new ConversionOperation(operand, compilation.ClassifyConvertibleConversion(operand, iDisposable, out var constantValue), isTryCast, isChecked: false,
                                           semanticModel: null, operand.Syntax, iDisposable, constantValue, isImplicit: true);
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
                lockedValue = CreateConversion(semanticModel.Compilation, lockedValue, objectType);
            }

            int captureId = _availableCaptureId++;
            AddStatement(new FlowCapture(captureId, lockedValue.Syntax, lockedValue));
            lockedValue = GetCaptureReference(captureId, lockedValue);

            var enterMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2);
            bool legacyMode = (enterMethod == null);

            if (legacyMode)
            {
                enterMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter);

                // Monitor.Enter($lock);
                if (enterMethod == null)
                {
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

            var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));

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

            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
            LeaveRegion();

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Finally));
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (!legacyMode)
            {
                // if ($lockTaken)
                IOperation condition = OperationCloner.CloneOperation(lockTaken);
                condition = Operation.SetParentOperation(condition, null);
                LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: false, RegularBranch(endOfFinally));
                _currentBasicBlock = null;
            }

            // Monitor.Exit($lock);
            var exitMethod = (IMethodSymbol)semanticModel.Compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Exit);
            lockedValue = OperationCloner.CloneOperation(lockedValue);

            if (exitMethod == null)
            {
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
            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
            LeaveRegion();

            AppendNewBlock(afterTryFinally, linkToPrevious: false);

            return null;
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            ForEachLoopOperationInfo info = ((BaseForEachLoopStatement)operation).Info;

            bool createdRegionForCollection = false;

            if (!operation.Locals.IsEmpty && operation.LoopControlVariable.Kind == OperationKind.VariableDeclarator)
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
                        EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: ImmutableArray.Create(local)));
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
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));
            }

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            var @break = GetLabeledOrNewBlock(operation.ExitLabel);

            AppendNewBlock(@continue);

            IOperation condition = Operation.SetParentOperation(getCondition(enumerator), null);
            LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: false, RegularBranch(@break));
            _currentBasicBlock = null;

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            AddStatement(getLoopControlVariableAssignment(applyConversion(info.CurrentConversion, getCurrent(OperationCloner.CloneOperation(enumerator)), info.ElementType)));
            VisitStatement(operation.Body);
            LinkBlocks(CurrentBasicBlock, @continue);

            LeaveRegion();

            AppendNewBlock(@break);

            if (info.NeedsDispose)
            {
                var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);
                LinkBlocks(CurrentBasicBlock, afterTryFinally);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                LeaveRegion();

                Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
                AddDisposingFinally(OperationCloner.CloneOperation(enumerator),
                                    info.KnownToImplementIDisposable,
                                    compilation.GetSpecialType(SpecialType.System_IDisposable),
                                    compilation);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
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

        public override IOperation VisitForToLoop(IForToLoopOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            (ILocalSymbol loopObject, ForToLoopOperationUserDefinedInfo userDefinedInfo) = ((BaseForToLoopStatement)operation).Info;
            bool isObjectLoop = (loopObject != null);
            ImmutableArray<ILocalSymbol> locals = operation.Locals;

            if (isObjectLoop)
            {
                locals = locals.Insert(0, loopObject);
            }

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            ITypeSymbol booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            BasicBlockBuilder @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            BasicBlockBuilder @break = GetLabeledOrNewBlock(operation.ExitLabel);
            BasicBlockBuilder checkConditionBlock = new BasicBlockBuilder(BasicBlockKind.Block);
            BasicBlockBuilder bodyBlock = new BasicBlockBuilder(BasicBlockKind.Block);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals));

            // Handle loop initialization
            int limitValueId = -1;
            int stepValueId = -1;
            IFlowCaptureReferenceOperation positiveFlag = null;
            ITypeSymbol stepEnumUnderlyingTypeOrSelf = ITypeSymbolHelpers.GetEnumUnderlyingTypeOrSelf(operation.StepValue.Type);

            initializeLoop();

            // Now check condition
            AppendNewBlock(checkConditionBlock);
            checkLoopCondition();

            // Handle body
            AppendNewBlock(bodyBlock);
            VisitStatement(operation.Body);

            // Increment
            AppendNewBlock(@continue);
            incrementLoopControlVariable();

            LinkBlocks(CurrentBasicBlock, checkConditionBlock);
            _currentBasicBlock = null;

            LeaveRegion();

            AppendNewBlock(@break);
            return null;

            IOperation tryCallObjectForLoopControlHelper(SyntaxNode syntax, WellKnownMember helper)
            {
                bool isInitialization = (helper == WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);
                var loopObjectReference = new LocalReferenceExpression(loopObject, 
                                                                       isDeclaration: isInitialization, 
                                                                       semanticModel: null,
                                                                       operation.LoopControlVariable.Syntax, loopObject.Type,
                                                                       constantValue: default, isImplicit: true);

                var method = (IMethodSymbol)compilation.CommonGetWellKnownTypeMember(helper);
                int parametersCount = WellKnownMembers.GetDescriptor(helper).ParametersCount;

                if (method is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(--parametersCount, fillWithValue: null);
                    builder[--parametersCount] = loopObjectReference;
                    do
                    {
                        builder[--parametersCount] = _evalStack.Pop();
                    }
                    while (parametersCount != 0);

                    return MakeInvalidOperation(operation.LimitValue.Syntax, booleanType, builder.ToImmutableAndFree());
                }
                else
                {
                    var builder = ArrayBuilder<IArgumentOperation>.GetInstance(parametersCount, fillWithValue: null);

                    builder[--parametersCount] = new ArgumentOperation(getLoopControlVariableReference(forceImplicit: true), // Yes we are going to evaluate it again
                                                                       ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       inConversionOpt: null, outConversionOpt: null,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    builder[--parametersCount] = new ArgumentOperation(loopObjectReference,
                                                                       ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       inConversionOpt: null, outConversionOpt: null,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    do
                    {
                        IOperation value = _evalStack.Pop();
                        builder[--parametersCount] = new ArgumentOperation(value,
                                                                           ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                           inConversionOpt: null, outConversionOpt: null,
                                                                           semanticModel: null, isInitialization ? value.Syntax : syntax, isImplicit: true);
                    }
                    while (parametersCount != 0);

                    return new InvocationExpression(method, instance: null, isVirtual: false, builder.ToImmutableAndFree(),
                                                    semanticModel: null, operation.LimitValue.Syntax, method.ReturnType,
                                                    constantValue: default, isImplicit: true);
                }
            }

            void initializeLoop()
            {
                if (isObjectLoop)
                {
                    // For i as Object = 3 To 6 step 2
                    //    body
                    // Next
                    //
                    // becomes ==>
                    //
                    // {
                    //   Dim loopObj        ' mysterious object that holds the loop state
                    //
                    //   ' helper does internal initialization and tells if we need to do any iterations
                    //   if Not ObjectFlowControl.ForLoopControl.ForLoopInitObj(ctrl, init, limit, step, ref loopObj, ref ctrl) 
                    //                               goto exit:
                    //   start:
                    //       body
                    //
                    //   continue:
                    //       ' helper updates loop state and tells if we need to do another iteration.
                    //       if ObjectFlowControl.ForLoopControl.ForNextCheckObj(ctrl, loopObj, ref ctrl) 
                    //                               GoTo start
                    // }
                    // exit:

#if DEBUG
                    int stackSize = _evalStack.Count;
#endif
                    _evalStack.Push(getLoopControlVariableReference(forceImplicit: false));
                    _evalStack.Push(Visit(operation.InitialValue));
                    _evalStack.Push(Visit(operation.LimitValue));
                    _evalStack.Push(Visit(operation.StepValue));

                    IOperation condition = tryCallObjectForLoopControlHelper(operation.LoopControlVariable.Syntax,
                                                                             WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);
#if DEBUG
                    Debug.Assert(stackSize == _evalStack.Count);
#endif
                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                    LinkBlocks(CurrentBasicBlock, bodyBlock);
                    _currentBasicBlock = null;
                }
                else
                {
                    int captureId = _availableCaptureId++;
                    IOperation controlVarReferenceForInitialization = getLoopControlVariableReference(forceImplicit: false, captureId);
                    CaptureResultIfNotAlready(controlVarReferenceForInitialization.Syntax, captureId, controlVarReferenceForInitialization);

                    int initialValueId = VisitAndCapture(operation.InitialValue);
                    limitValueId = VisitAndCapture(operation.LimitValue);
                    stepValueId = VisitAndCapture(operation.StepValue);
                    IOperation stepValue = GetCaptureReference(stepValueId, operation.StepValue);

                    if (userDefinedInfo != null)
                    {
                        Debug.Assert(_forToLoopBinaryOperatorLeftOperand == null);
                        Debug.Assert(_forToLoopBinaryOperatorRightOperand == null);

                        // calculate and cache result of a positive check := step >= (step - step).
                        _forToLoopBinaryOperatorLeftOperand = GetCaptureReference(stepValueId, operation.StepValue);
                        _forToLoopBinaryOperatorRightOperand = GetCaptureReference(stepValueId, operation.StepValue);

                        IOperation subtraction = Visit(userDefinedInfo.Subtraction.Value);

                        _forToLoopBinaryOperatorLeftOperand = stepValue;
                        _forToLoopBinaryOperatorRightOperand = subtraction;

                        IOperation greaterThanOrEqual = Visit(userDefinedInfo.GreaterThanOrEqual.Value);

                        int positiveFlagId = _availableCaptureId++;
                        AddStatement(new FlowCapture(positiveFlagId, greaterThanOrEqual.Syntax, greaterThanOrEqual));
                        positiveFlag = new FlowCaptureReference(positiveFlagId, greaterThanOrEqual.Syntax, greaterThanOrEqual.Type, constantValue: default);

                        _forToLoopBinaryOperatorLeftOperand = null;
                        _forToLoopBinaryOperatorRightOperand = null;
                    }
                    else if (!operation.StepValue.ConstantValue.HasValue &&
                             !ITypeSymbolHelpers.IsSignedIntegralType(stepEnumUnderlyingTypeOrSelf) &&
                             !ITypeSymbolHelpers.IsUnsignedIntegralType(stepEnumUnderlyingTypeOrSelf))
                    {
                        IOperation stepValueIsNull = null;

                        if (ITypeSymbolHelpers.IsNullableType(stepValue.Type))
                        {
                            stepValueIsNull = MakeIsNullOperation(GetCaptureReference(stepValueId, operation.StepValue), booleanType);
                            stepValue = UnwrapNullableValue(stepValue, compilation);
                        }

                        ITypeSymbol stepValueEnumUnderlyingTypeOrSelf = ITypeSymbolHelpers.GetEnumUnderlyingTypeOrSelf(stepValue.Type);

                        if (ITypeSymbolHelpers.IsNumericType(stepValueEnumUnderlyingTypeOrSelf))
                        {
                            // this one is tricky.
                            // step value is not used directly in the loop condition
                            // however its value determines the iteration direction
                            // isUp = IsTrue(step >= step - step)
                            // which implies that "step = null" ==> "isUp = false"

                            IOperation isUp;
                            int positiveFlagId = _availableCaptureId++;
                            var afterPositiveCheck = new BasicBlockBuilder(BasicBlockKind.Block);

                            if (stepValueIsNull != null)
                            {
                                var whenNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                                LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(stepValueIsNull, null), jumpIfTrue: false, RegularBranch(whenNotNull));
                                _currentBasicBlock = null;

                                // "isUp = false"
                                isUp = new LiteralExpression(semanticModel: null, stepValue.Syntax, booleanType, constantValue: false, isImplicit: true);

                                AddStatement(new FlowCapture(positiveFlagId, isUp.Syntax, isUp));

                                LinkBlocks(CurrentBasicBlock, afterPositiveCheck);
                                AppendNewBlock(whenNotNull);
                            }

                            IOperation literal = new LiteralExpression(semanticModel: null, stepValue.Syntax, stepValue.Type,
                                                                       constantValue: ConstantValue.Default(stepValueEnumUnderlyingTypeOrSelf.SpecialType).Value, 
                                                                       isImplicit: true);

                            isUp = new BinaryOperatorExpression(BinaryOperatorKind.GreaterThanOrEqual,
                                                                stepValue,
                                                                literal,
                                                                isLifted: false,
                                                                isChecked: false,
                                                                isCompareText: false,
                                                                operatorMethod: null,
                                                                unaryOperatorMethod: null,
                                                                semanticModel: null,
                                                                stepValue.Syntax,
                                                                booleanType,
                                                                constantValue: default,
                                                                isImplicit: true);

                            AddStatement(new FlowCapture(positiveFlagId, isUp.Syntax, isUp));

                            AppendNewBlock(afterPositiveCheck);

                            positiveFlag = new FlowCaptureReference(positiveFlagId, isUp.Syntax, isUp.Type, constantValue: default);
                        }
                        else
                        {
                            // This must be an error case.
                            // It is fine to do nothing in this case, we are in recovery mode. 
                        }
                    }

                    AddStatement(new SimpleAssignmentExpression(GetCaptureReference(captureId, controlVarReferenceForInitialization),
                                                                isRef: false,
                                                                GetCaptureReference(initialValueId, operation.InitialValue),
                                                                semanticModel: null, operation.InitialValue.Syntax, type: null,
                                                                constantValue: default, isImplicit: true));
                }
            }

            void checkLoopCondition()
            {
                if (isObjectLoop)
                {
                    // For i as Object = 3 To 6 step 2
                    //    body
                    // Next
                    //
                    // becomes ==>
                    //
                    // {
                    //   Dim loopObj        ' mysterious object that holds the loop state
                    //
                    //   ' helper does internal initialization and tells if we need to do any iterations
                    //   if Not ObjectFlowControl.ForLoopControl.ForLoopInitObj(ctrl, init, limit, step, ref loopObj, ref ctrl) 
                    //                               goto exit:
                    //   start:
                    //       body
                    //
                    //   continue:
                    //       ' helper updates loop state and tells if we need to do another iteration.
                    //       if ObjectFlowControl.ForLoopControl.ForNextCheckObj(ctrl, loopObj, ref ctrl) 
                    //                               GoTo start
                    // }
                    // exit:

#if DEBUG
                    int stackSize = _evalStack.Count;
#endif
                    _evalStack.Push(getLoopControlVariableReference(forceImplicit: true));

                    IOperation condition = tryCallObjectForLoopControlHelper(operation.LimitValue.Syntax,
                                                                             WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj);
#if DEBUG
                    Debug.Assert(stackSize == _evalStack.Count);
#endif
                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                    LinkBlocks(CurrentBasicBlock, bodyBlock);
                    _currentBasicBlock = null;
                    return;
                }
                else if (userDefinedInfo != null)
                {
                    Debug.Assert(_forToLoopBinaryOperatorLeftOperand == null);
                    Debug.Assert(_forToLoopBinaryOperatorRightOperand == null);

                    // Generate If(positiveFlag, controlVariable <= limit, controlVariable >= limit)

                    // Spill control variable reference, we are going to have branches here.
                    int captureId = _availableCaptureId++;
                    IOperation controlVariableReferenceForCondition = getLoopControlVariableReference(forceImplicit: true, captureId); // Yes we are going to evaluate it again
                    CaptureResultIfNotAlready(controlVariableReferenceForCondition.Syntax, captureId, controlVariableReferenceForCondition);
                    controlVariableReferenceForCondition = GetCaptureReference(captureId, controlVariableReferenceForCondition);

                    var notPositive = new BasicBlockBuilder(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(positiveFlag, null), jumpIfTrue: false, RegularBranch(notPositive));
                    _currentBasicBlock = null;

                    _forToLoopBinaryOperatorLeftOperand = controlVariableReferenceForCondition;
                    _forToLoopBinaryOperatorRightOperand = GetCaptureReference(limitValueId, operation.LimitValue);

                    VisitConditionalBranch(userDefinedInfo.LessThanOrEqual.Value, ref @break, sense: false);
                    LinkBlocks(CurrentBasicBlock, bodyBlock);

                    AppendNewBlock(notPositive);

                    _forToLoopBinaryOperatorLeftOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorLeftOperand);
                    _forToLoopBinaryOperatorRightOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorRightOperand);

                    VisitConditionalBranch(userDefinedInfo.GreaterThanOrEqual.Value, ref @break, sense: false);
                    LinkBlocks(CurrentBasicBlock, bodyBlock);
                    _currentBasicBlock = null;

                    _forToLoopBinaryOperatorLeftOperand = null;
                    _forToLoopBinaryOperatorRightOperand = null;
                    return;
                }
                else
                {
                    IOperation controlVariableReferenceforCondition = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    IOperation limitReference = GetCaptureReference(limitValueId, operation.LimitValue);
                    var comparisonKind = BinaryOperatorKind.None;

                    // unsigned step is always Up
                    if (ITypeSymbolHelpers.IsUnsignedIntegralType(stepEnumUnderlyingTypeOrSelf))
                    {
                        comparisonKind = BinaryOperatorKind.LessThanOrEqual;
                    }
                    else if (operation.StepValue.ConstantValue.HasValue)
                    {
                        // Up/Down for numeric constants is also simple 
                        object value = operation.StepValue.ConstantValue.Value;
                        ConstantValueTypeDiscriminator discriminator = ConstantValue.GetDiscriminator(stepEnumUnderlyingTypeOrSelf.SpecialType);

                        if (value != null && discriminator != ConstantValueTypeDiscriminator.Bad)
                        {
                            var constStep = ConstantValue.Create(value, discriminator);

                            if (constStep.IsNegativeNumeric)
                            {
                                comparisonKind = BinaryOperatorKind.GreaterThanOrEqual;
                            }
                            else if (constStep.IsNumeric)
                            {
                                comparisonKind = BinaryOperatorKind.LessThanOrEqual;
                            }
                        }
                    }

                    // for signed integral steps not known at compile time
                    // we do    " (val Xor (step >> 31)) <= (limit Xor (step >> 31)) "
                    // where 31 is actually the size-1
                    if (comparisonKind == BinaryOperatorKind.None && ITypeSymbolHelpers.IsSignedIntegralType(stepEnumUnderlyingTypeOrSelf))
                    {
                        comparisonKind = BinaryOperatorKind.LessThanOrEqual;
                        controlVariableReferenceforCondition = negateIfStepNegative(controlVariableReferenceforCondition);
                        limitReference = negateIfStepNegative(limitReference);
                    }

                    IOperation condition;

                    if (comparisonKind != BinaryOperatorKind.None)
                    {
                        condition = new BinaryOperatorExpression(comparisonKind,
                                                                 controlVariableReferenceforCondition,
                                                                 limitReference,
                                                                 isLifted: false,
                                                                 isChecked: false,
                                                                 isCompareText: false,
                                                                 operatorMethod: null,
                                                                 unaryOperatorMethod: null,
                                                                 semanticModel: null,
                                                                 operation.LimitValue.Syntax,
                                                                 booleanType,
                                                                 constantValue: default,
                                                                 isImplicit: true);

                        LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                        LinkBlocks(CurrentBasicBlock, bodyBlock);
                        _currentBasicBlock = null;
                        return;
                    }

                    if (positiveFlag == null)
                    {
                        // Must be an error case.
                        condition = MakeInvalidOperation(operation.LimitValue.Syntax, booleanType, controlVariableReferenceforCondition, limitReference);
                        LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                        LinkBlocks(CurrentBasicBlock, bodyBlock);
                        _currentBasicBlock = null;
                        return;
                    }

                    IOperation eitherLimitOrControlVariableIsNull = null;

                    if (ITypeSymbolHelpers.IsNullableType(operation.LimitValue.Type))
                    {
                        eitherLimitOrControlVariableIsNull = new BinaryOperatorExpression(BinaryOperatorKind.Or,
                                                                                          MakeIsNullOperation(limitReference, booleanType),
                                                                                          MakeIsNullOperation(controlVariableReferenceforCondition, booleanType),
                                                                                          isLifted: false,
                                                                                          isChecked: false,
                                                                                          isCompareText: false,
                                                                                          operatorMethod: null,
                                                                                          unaryOperatorMethod: null,
                                                                                          semanticModel: null,
                                                                                          operation.StepValue.Syntax,
                                                                                          compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                                          constantValue: default,
                                                                                          isImplicit: true);

                        // if either limit or control variable is null, we exit the loop
                        var whenBothNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                        LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(eitherLimitOrControlVariableIsNull, null), jumpIfTrue: false, RegularBranch(whenBothNotNull));
                        LinkBlocks(CurrentBasicBlock, @break);
                        AppendNewBlock(whenBothNotNull);

                        controlVariableReferenceforCondition = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                        limitReference = GetCaptureReference(limitValueId, operation.LimitValue);

                        Debug.Assert(ITypeSymbolHelpers.IsNullableType(controlVariableReferenceforCondition.Type));
                        controlVariableReferenceforCondition = UnwrapNullableValue(controlVariableReferenceforCondition, compilation);
                        limitReference = UnwrapNullableValue(limitReference, compilation);
                    }

                    // If (positiveFlag, ctrl <= limit, ctrl >= limit)

                    if (controlVariableReferenceforCondition.Kind != OperationKind.FlowCaptureReference)
                    {
                        int captureId = _availableCaptureId++;
                        AddStatement(new FlowCapture(captureId, controlVariableReferenceforCondition.Syntax, controlVariableReferenceforCondition));
                        controlVariableReferenceforCondition = GetCaptureReference(captureId, controlVariableReferenceforCondition);
                    }

                    var notPositive = new BasicBlockBuilder(BasicBlockKind.Block);
                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(positiveFlag, null), jumpIfTrue: false, RegularBranch(notPositive));
                    _currentBasicBlock = null;

                    condition = new BinaryOperatorExpression(BinaryOperatorKind.LessThanOrEqual,
                                                             controlVariableReferenceforCondition,
                                                             limitReference,
                                                             isLifted: false,
                                                             isChecked: false,
                                                             isCompareText: false,
                                                             operatorMethod: null,
                                                             unaryOperatorMethod: null,
                                                             semanticModel: null,
                                                             operation.LimitValue.Syntax,
                                                             booleanType,
                                                             constantValue: default,
                                                             isImplicit: true);

                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                    LinkBlocks(CurrentBasicBlock, bodyBlock);

                    AppendNewBlock(notPositive);

                    condition = new BinaryOperatorExpression(BinaryOperatorKind.GreaterThanOrEqual,
                                                             OperationCloner.CloneOperation(controlVariableReferenceforCondition),
                                                             OperationCloner.CloneOperation(limitReference),
                                                             isLifted: false,
                                                             isChecked: false,
                                                             isCompareText: false,
                                                             operatorMethod: null,
                                                             unaryOperatorMethod: null,
                                                             semanticModel: null,
                                                             operation.LimitValue.Syntax,
                                                             booleanType,
                                                             constantValue: default,
                                                             isImplicit: true);

                    LinkBlocks(CurrentBasicBlock, Operation.SetParentOperation(condition, null), jumpIfTrue: false, RegularBranch(@break));
                    LinkBlocks(CurrentBasicBlock, bodyBlock);
                    _currentBasicBlock = null;
                    return;
                }

                throw ExceptionUtilities.Unreachable;
            }

            // Produce "(operand Xor (step >> 31))"
            // where 31 is actually the size-1
            IOperation negateIfStepNegative(IOperation operand)
            {
                int bits = stepEnumUnderlyingTypeOrSelf.SpecialType.VBForToShiftBits();

                var shiftConst = new LiteralExpression(semanticModel: null, operand.Syntax, compilation.GetSpecialType(SpecialType.System_Int32),
                                                       constantValue: bits, isImplicit: true);

                var shiftedStep = new BinaryOperatorExpression(BinaryOperatorKind.RightShift,
                                                               GetCaptureReference(stepValueId, operation.StepValue),
                                                               shiftConst,
                                                               isLifted: false,
                                                               isChecked: false,
                                                               isCompareText: false,
                                                               operatorMethod: null,
                                                               unaryOperatorMethod: null,
                                                               semanticModel: null,
                                                               operand.Syntax,
                                                               operation.StepValue.Type,
                                                               constantValue: default,
                                                               isImplicit: true);

                return new BinaryOperatorExpression(BinaryOperatorKind.ExclusiveOr,
                                                    shiftedStep,
                                                    operand,
                                                    isLifted: false,
                                                    isChecked: false,
                                                    isCompareText: false,
                                                    operatorMethod: null,
                                                    unaryOperatorMethod: null,
                                                    semanticModel: null,
                                                    operand.Syntax,
                                                    operand.Type,
                                                    constantValue: default,
                                                    isImplicit: true);
            }

            void incrementLoopControlVariable()
            {
                if (isObjectLoop)
                {
                    // there is nothing interesting to do here, increment is folded into the condition check
                    return;
                }
                else if (userDefinedInfo != null)
                {
                    Debug.Assert(_forToLoopBinaryOperatorLeftOperand == null);
                    Debug.Assert(_forToLoopBinaryOperatorRightOperand == null);

                    IOperation controlVariableReferenceForAssignment = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again

                    // We are going to evaluate control variable again and that might require branches
                    _evalStack.Push(controlVariableReferenceForAssignment);

                    // Generate: controlVariable + stepValue
                    _forToLoopBinaryOperatorLeftOperand = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    _forToLoopBinaryOperatorRightOperand = GetCaptureReference(stepValueId, operation.StepValue);

                    IOperation increment = Visit(userDefinedInfo.Addition.Value);

                    _forToLoopBinaryOperatorLeftOperand = null;
                    _forToLoopBinaryOperatorRightOperand = null;

                    controlVariableReferenceForAssignment = _evalStack.Pop();
                    AddStatement(new SimpleAssignmentExpression(controlVariableReferenceForAssignment,
                                                                isRef: false,
                                                                increment,
                                                                semanticModel: null,
                                                                controlVariableReferenceForAssignment.Syntax,
                                                                type: null,
                                                                constantValue: default,
                                                                isImplicit: true));
                }
                else
                {
                    BasicBlockBuilder afterIncrement = new BasicBlockBuilder(BasicBlockKind.Block);
                    IOperation controlVariableReferenceForAssignment;
                    bool isNullable = ITypeSymbolHelpers.IsNullableType(operation.StepValue.Type);

                    if (isNullable)
                    {
                        // Spill control variable reference, we are going to have branches here.
                        int captureId = _availableCaptureId++;
                        controlVariableReferenceForAssignment = getLoopControlVariableReference(forceImplicit: true, captureId); // Yes we are going to evaluate it again
                        CaptureResultIfNotAlready(controlVariableReferenceForAssignment.Syntax, captureId, controlVariableReferenceForAssignment);
                        controlVariableReferenceForAssignment = GetCaptureReference(captureId, controlVariableReferenceForAssignment);

                        BasicBlockBuilder whenNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                        IOperation condition = new BinaryOperatorExpression(BinaryOperatorKind.Or,
                                                                            MakeIsNullOperation(GetCaptureReference(stepValueId, operation.StepValue), booleanType),
                                                                            MakeIsNullOperation(getLoopControlVariableReference(forceImplicit: true), // Yes we are going to evaluate it again
                                                                                                booleanType),
                                                                            isLifted: false,
                                                                            isChecked: false,
                                                                            isCompareText: false,
                                                                            operatorMethod: null,
                                                                            unaryOperatorMethod: null,
                                                                            semanticModel: null,
                                                                            operation.StepValue.Syntax,
                                                                            compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                            constantValue: default,
                                                                            isImplicit: true);

                        condition = Operation.SetParentOperation(condition, null);
                        LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: false, RegularBranch(whenNotNull));
                        _currentBasicBlock = null;

                        AddStatement(new SimpleAssignmentExpression(controlVariableReferenceForAssignment,
                                                                    isRef: false,
                                                                    new DefaultValueExpression(semanticModel: null,
                                                                                               controlVariableReferenceForAssignment.Syntax,
                                                                                               controlVariableReferenceForAssignment.Type, 
                                                                                               constantValue: default,
                                                                                               isImplicit: true), 
                                                                    semanticModel: null,
                                                                    controlVariableReferenceForAssignment.Syntax, 
                                                                    type: null, 
                                                                    constantValue: default, 
                                                                    isImplicit: true));

                        LinkBlocks(CurrentBasicBlock, afterIncrement);

                        AppendNewBlock(whenNotNull);

                        controlVariableReferenceForAssignment = GetCaptureReference(captureId, controlVariableReferenceForAssignment);
                    }
                    else
                    {
                        controlVariableReferenceForAssignment = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    }

                    // We are going to evaluate control variable again and that might require branches
                    _evalStack.Push(controlVariableReferenceForAssignment);

                    IOperation controlVariableReferenceForIncrement = getLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    IOperation stepValueForIncrement = GetCaptureReference(stepValueId, operation.StepValue);

                    if (isNullable)
                    {
                        Debug.Assert(ITypeSymbolHelpers.IsNullableType(controlVariableReferenceForIncrement.Type));
                        controlVariableReferenceForIncrement = UnwrapNullableValue(controlVariableReferenceForIncrement, compilation);
                        stepValueForIncrement = UnwrapNullableValue(stepValueForIncrement, compilation);
                    }

                    IOperation increment = new BinaryOperatorExpression(BinaryOperatorKind.Add,
                                                                        controlVariableReferenceForIncrement,
                                                                        stepValueForIncrement,
                                                                        isLifted: false,
                                                                        isChecked: operation.IsChecked,
                                                                        isCompareText: false,
                                                                        operatorMethod: null,
                                                                        unaryOperatorMethod: null,
                                                                        semanticModel: null,
                                                                        operation.StepValue.Syntax,
                                                                        controlVariableReferenceForIncrement.Type,
                                                                        constantValue: default,
                                                                        isImplicit: true);

                    if (isNullable)
                    {
                        increment = MakeNullable(compilation, increment, controlVariableReferenceForAssignment.Type);
                    }

                    controlVariableReferenceForAssignment = _evalStack.Pop();
                    AddStatement(new SimpleAssignmentExpression(controlVariableReferenceForAssignment,
                                                                isRef: false,
                                                                increment,
                                                                semanticModel: null,
                                                                controlVariableReferenceForAssignment.Syntax,
                                                                type: null,
                                                                constantValue: default,
                                                                isImplicit: true));

                    AppendNewBlock(afterIncrement);
                }
            }

            IOperation getLoopControlVariableReference(bool forceImplicit, int? captureIdForReference = null)
            {
                switch (operation.LoopControlVariable.Kind)
                {
                    case OperationKind.VariableDeclarator:
                        var declarator = (IVariableDeclaratorOperation)operation.LoopControlVariable;
                        ILocalSymbol local = declarator.Symbol;

                        return new LocalReferenceExpression(local, isDeclaration: true, semanticModel: null,
                                                            declarator.Syntax, local.Type, constantValue: default, isImplicit: true);

                    default:
                        Debug.Assert(!_forceImplicit);
                        _forceImplicit = forceImplicit;
                        IOperation result = Visit(operation.LoopControlVariable, captureIdForReference);
                        _forceImplicit = false;
                        return result;
                }
            }
        }

        private static FlowCaptureReference GetCaptureReference(int id, IOperation underlying)
        {
            return new FlowCaptureReference(id, underlying.Syntax, underlying.Type, underlying.ConstantValue);
        }

        internal override IOperation VisitAggregateQuery(IAggregateQueryOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            int groupCaptureId = VisitAndCapture(operation.Group);

            IOperation previousAggregationGroup = _currentAggregationGroup;
            _currentAggregationGroup = GetCaptureReference(groupCaptureId, operation.Group);

            IOperation result = Visit(operation.Aggregation);

            _currentAggregationGroup = previousAggregationGroup;
            return result;
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            Compilation compilation = ((Operation)operation).SemanticModel.Compilation;
            INamedTypeSymbol booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            int expressionCaptureId = VisitAndCapture(operation.Value);
            FlowCaptureReference switchValue = GetCaptureReference(expressionCaptureId, operation.Value);

            ImmutableArray<ILocalSymbol> locals = getLocals();
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals));

            BasicBlockBuilder defaultBody = null; // Adjusted in handleSection
            BasicBlockBuilder @break = GetLabeledOrNewBlock(operation.ExitLabel);

            foreach (ISwitchCaseOperation section in operation.Cases)
            {
                handleSection(section);
            }

            if (defaultBody != null)
            {
                LinkBlocks(CurrentBasicBlock, defaultBody); 
            }

            LeaveRegion();

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
                var body = new BasicBlockBuilder(BasicBlockKind.Block);
                var nextSection = new BasicBlockBuilder(BasicBlockKind.Block);

                IOperation condition = ((BaseSwitchCase)section).Condition;
                if (condition != null)
                {
                    Debug.Assert(section.Clauses.All(c => c.Label == null));
                    Debug.Assert(_currentSwitchOperationExpression == null);
                    _currentSwitchOperationExpression = switchValue;
                    VisitConditionalBranch(condition, ref nextSection, sense: false);
                    _currentSwitchOperationExpression = null;
                }
                else
                {
                    foreach (ICaseClauseOperation caseClause in section.Clauses)
                    {
                        var nextCase = new BasicBlockBuilder(BasicBlockKind.Block);
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

            void handleCase(ICaseClauseOperation caseClause, BasicBlockBuilder body, BasicBlockBuilder nextCase)
            {
                IOperation condition;
                BasicBlockBuilder labeled = GetLabeledOrNewBlock(caseClause.Label);
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
                            IOperation leftOperand = OperationCloner.CloneOperation(switchValue);
                            IOperation rightOperand = Visit(compareWith);

                            if (isLifted)
                            {
                                if (!leftIsNullable)
                                {
                                    if (leftOperand.Type != null)
                                    {
                                        leftOperand = MakeNullable(compilation, leftOperand, compareWith.Type);
                                    }
                                }
                                else if (!rightIsNullable && rightOperand.Type != null)
                                {
                                    rightOperand = MakeNullable(compilation, rightOperand, operation.Value.Type);
                                }
                            }

                            condition = new BinaryOperatorExpression(BinaryOperatorKind.Equals,
                                                                     leftOperand,
                                                                     rightOperand,
                                                                     isLifted,
                                                                     isChecked: false,
                                                                     isCompareText: false,
                                                                     operatorMethod: null,
                                                                     unaryOperatorMethod: null,
                                                                     semanticModel: null,
                                                                     compareWith.Syntax,
                                                                     booleanType,
                                                                     constantValue: default,
                                                                     isImplicit: true);

                            condition = Operation.SetParentOperation(condition, null);
                            LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: false, RegularBranch(nextCase));
                            AppendNewBlock(labeled);
                            _currentBasicBlock = null;
                        }

                    case CaseKind.Pattern:
                        var patternClause = (IPatternCaseClauseOperation)caseClause;

                        condition = new IsPatternExpression(OperationCloner.CloneOperation(switchValue), Visit(patternClause.Pattern), semanticModel: null,
                                                            patternClause.Pattern.Syntax, booleanType, constantValue: default, isImplicit: true);
                        condition = Operation.SetParentOperation(condition, null);
                        LinkBlocks(CurrentBasicBlock, condition, jumpIfTrue: false, RegularBranch(nextCase));

                        if (patternClause.Guard != null)
                        {
                            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
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

                        // A switch section with a relational case other than an equality must have 
                        // a condition associated with it. This point should not be reachable.
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

                    case CaseKind.Range:
                        // A switch section with a range case must have a condition associated with it.
                        // This point should not be reachable.
                    default:
                        throw ExceptionUtilities.UnexpectedValue(caseClause.CaseKind);
                }
            }
        }

        private static IOperation MakeNullable(Compilation compilation, IOperation operand, ITypeSymbol type)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(type));
            Debug.Assert(ITypeSymbolHelpers.GetNullableUnderlyingType(type).Equals(operand.Type));

            return CreateConversion(compilation, operand, type);
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
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitEnd(IEndOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            BasicBlockBuilder current = CurrentBasicBlock;
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block), linkToPrevious: false);
            Debug.Assert(current.BranchValue == null);
            Debug.Assert(!current.HasCondition);
            Debug.Assert(current.FallThrough.Destination == null);
            Debug.Assert(current.FallThrough.Kind == ControlFlowBranchSemantics.None);
            current.FallThrough.Kind = ControlFlowBranchSemantics.ProgramTermination;
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

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            ImmutableArray<IOperation> initialization = operation.Before;

            if (initialization.Length == 1 && initialization[0].Kind == OperationKind.VariableDeclarationGroup)
            {
                HandleVariableDeclarations((VariableDeclarationGroupOperation)initialization.Single());
            }
            else
            {
                VisitStatements(initialization);
            }

            var start = new BasicBlockBuilder(BasicBlockKind.Block);
            AppendNewBlock(start);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.ConditionLocals));

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

            LeaveRegion(); // ConditionLocals
            LeaveRegion(); // Locals

            AppendNewBlock(@break);

            return null;
        }

        internal override IOperation VisitFixed(IFixedOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            HandleVariableDeclarations(operation.Variables);

            VisitStatement(operation.Body);

            LeaveRegion();
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

            // If the local is a static (possible in VB), then we create a semaphore for conditional execution of the initializer.
            BasicBlockBuilder afterInitialization = null;
            if (localSymbol.IsStatic && (declarator.Initializer != null || declaration.Initializer != null))
            {
                afterInitialization = new BasicBlockBuilder(BasicBlockKind.Block);

                ITypeSymbol booleanType = ((Operation)declaration).SemanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);
                var initializationSemaphore = new StaticLocalInitializationSemaphoreOperation(localSymbol, declarator.Syntax, booleanType);
                Operation.SetParentOperation(initializationSemaphore, null);

                LinkBlocks(CurrentBasicBlock, initializationSemaphore, jumpIfTrue: false, RegularBranch(afterInitialization));

                _currentBasicBlock = null;
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.StaticLocalInitializer,
                                              allowMethods: false)); // Lambdas shouldn't be associated with this region
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
            // PROTOTYPE(dataflow): It looks like there is a bug in IOperation tree generation for non-error scenario in 
            //                      Microsoft.CodeAnalysis.CSharp.UnitTests.SemanticModelGetSemanticInfoTests.ObjectCreation3
            //                      We have ICollectionElementInitializerOperation, but not IObjectCreationOperation.
            IOperation instance = operation.TargetMethod.IsStatic ? null : operation.Instance;
            (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            return new InvocationExpression(operation.TargetMethod, visitedInstance, operation.IsVirtual, visitedArguments, semanticModel: null, operation.Syntax,
                                            operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        private (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) VisitInstanceWithArguments(IOperation instance, ImmutableArray<IArgumentOperation> arguments)
        {
            if (instance != null)
            {
                _evalStack.Push(Visit(instance));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(arguments);
            IOperation visitedInstance = instance == null ? null : _evalStack.Pop();

            return (visitedInstance, visitedArguments);
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, int? captureIdForResult)
        {
            ImmutableArray<IArgumentOperation> visitedArgs = VisitArguments(operation.Arguments);

            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            IOperation initializedInstance = new ObjectCreationExpression(operation.Constructor, initializer: null, visitedArgs, semanticModel: null, operation.Syntax, operation.Type,
                                                                          operation.ConstantValue, IsImplicit(operation));

            if (operation.Initializer != null)
            {
                SpillEvalStack();

                int initializerCaptureId = _availableCaptureId++;
                AddStatement(new FlowCapture(initializerCaptureId, initializedInstance.Syntax, initializedInstance));

                initializedInstance = GetCaptureReference(initializerCaptureId, initializedInstance);
                HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance);
            }

            return initializedInstance;
        }

        private void HandleObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation initializer, IOperation initializedInstance)
        {
            // PROTOTYPE(dataflow): Handle collection initializers
            IOperation previousInitializedInstance = _currentImplicitInstance;
            _currentImplicitInstance = initializedInstance;

            foreach (IOperation innerInitializer in initializer.Initializers)
            {
                handleInitializer(innerInitializer);
            }

            _currentImplicitInstance = previousInitializedInstance;
            return;

            void handleInitializer(IOperation innerInitializer)
            {
                switch (innerInitializer.Kind)
                {
                    case OperationKind.MemberInitializer:
                        handleMemberInitializer((IMemberInitializerOperation)innerInitializer);
                        return;

                    case OperationKind.SimpleAssignment:
                        AddStatement(handleSimpleAssignment((ISimpleAssignmentOperation)innerInitializer));
                        return;

                    case OperationKind.Invocation:
                    case OperationKind.DynamicInvocation:
                        // PROTOTYPE(dataflow): Dynamic invocation needs adjustment in the OperationFactory to ensure that
                        // dynamic member references are generated appropriately.
                        AddStatement(Visit(innerInitializer));
                        return;

                    case OperationKind.Increment:
                        // PROTOTYPE(dataflow): It looks like we can get here with an increment
                        //                      See Microsoft.CodeAnalysis.CSharp.UnitTests.SemanticModelGetSemanticInfoTests.ObjectInitializer_InvalidElementInitializer_IdentifierNameSyntax
                        //                      Just drop it for now to enable other test scenarios.
                        return;

                    case OperationKind.Literal:
                        // PROTOTYPE(dataflow): See Microsoft.CodeAnalysis.VisualBasic.UnitTests.BindingErrorTests.BC30994ERR_AggrInitInvalidForObject
                        return;

                    case OperationKind.Invalid:
                        AddStatement(Visit(innerInitializer));
                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(innerInitializer.Kind);
                }
            }

            IOperation handleSimpleAssignment(ISimpleAssignmentOperation assignmentOperation)
            {
                (bool pushSuccess, ImmutableArray<IOperation> arguments) = tryPushTarget(assignmentOperation.Target);

                if (!pushSuccess)
                {
                    // Error case. We don't try any error recovery here, just return whatever the default visit would.

                    // PROTOTYPE(dataflow): At the moment we can get here in other cases as well, see tryPushTarget
                    // Debug.Assert(assignmentOperation.Target.Kind == OperationKind.Invalid);
                    return Visit(assignmentOperation);
                }

                // We push the target, which effectively pushes individual components of the target (ie the instance, and arguments if present).
                // After that has been pushed, we visit the value of the assignment, to ensure that the instance is captured if
                // needed. Finally, we reassemble the target, which will pull the potentially captured instance from the stack
                // and reassemble the member reference from the parts.
                IOperation right = Visit(assignmentOperation.Value);
                IOperation left = popTarget(assignmentOperation.Target, arguments);

                return new SimpleAssignmentExpression(left, assignmentOperation.IsRef, right, semanticModel: null, assignmentOperation.Syntax,
                                                      assignmentOperation.Type, assignmentOperation.ConstantValue, IsImplicit(assignmentOperation));
            }

            void handleMemberInitializer(IMemberInitializerOperation memberInitializer)
            {
                // We explicitly do not push the initialized member onto the stack here. We visit the initialized member to get the implicit receiver that will be substituted in when an
                // IInstanceReferenceOperation with InstanceReferenceKind.ImplicitReceiver is encountered. If that receiver needs to be pushed onto the stack, its parent will handle it.
                // In member initializers, the code generated will evaluate InitializedMember multiple times. For example, if you have the following:
                //
                // class C1
                // {
                //   public C2 C2 { get; set; } = new C2();
                //   public void M()
                //   {
                //     var x = new C1 { C2 = { P1 = 1, P2 = 2 } };
                //   }
                // }
                // class C2
                // {
                //   public int P1 { get; set; }
                //   public int P2 { get; set; }
                // }
                //
                // We generate the following code for C1.M(). Note the multiple calls to C1::get_C2().
                //   IL_0000: nop
                //   IL_0001: newobj instance void C1::.ctor()
                //   IL_0006: dup
                //   IL_0007: callvirt instance class C2 C1::get_C2()
                //   IL_000c: ldc.i4.1
                //   IL_000d: callvirt instance void C2::set_P1(int32)
                //   IL_0012: nop
                //   IL_0013: dup
                //   IL_0014: callvirt instance class C2 C1::get_C2()
                //   IL_0019: ldc.i4.2
                //   IL_001a: callvirt instance void C2::set_P2(int32)
                //   IL_001f: nop
                //   IL_0020: stloc.0
                //   IL_0021: ret
                //
                // We therefore visit the InitializedMember to get the implicit receiver for the contained initializer, and that implicit receiver will be cloned everywhere it encounters
                // an IInstanceReferenceOperation with ReferenceKind InstanceReferenceKind.ImplicitReceiver

                (bool pushSuccess, ImmutableArray<IOperation> arguments) = tryPushTarget(memberInitializer.InitializedMember);
                IOperation instance = pushSuccess ? popTarget(memberInitializer.InitializedMember, arguments) : Visit(memberInitializer.InitializedMember);
                HandleObjectOrCollectionInitializer(memberInitializer.Initializer, instance);
            }

            (bool success, ImmutableArray<IOperation> arguments) tryPushTarget(IOperation instance)
            {
                switch (instance.Kind)
                {
                    case OperationKind.FieldReference:
                    case OperationKind.EventReference:
                    case OperationKind.PropertyReference:
                        var memberReference = (IMemberReferenceOperation)instance;
                        IPropertyReferenceOperation propertyReference = null;
                        ImmutableArray<IArgumentOperation> propertyArguments = ImmutableArray<IArgumentOperation>.Empty;

                        if (memberReference.Kind == OperationKind.PropertyReference &&
                            !(propertyReference = (IPropertyReferenceOperation)memberReference).Arguments.IsEmpty)
                        {
                            var propertyArgumentsBuilder = ArrayBuilder<IArgumentOperation>.GetInstance(propertyReference.Arguments.Length);
                            foreach (IArgumentOperation arg in propertyReference.Arguments)
                            {
                                // We assume all arguments have side effects and spill them. We only avoid capturing literals, and
                                // recapturing things that have already been captured once.
                                IOperation value = arg.Value;
                                int captureId = VisitAndCapture(value);
                                IOperation capturedValue = GetCaptureReference(captureId, value);
                                BaseArgument baseArgument = (BaseArgument)arg;
                                propertyArgumentsBuilder.Add(new ArgumentOperation(capturedValue, arg.ArgumentKind, arg.Parameter,
                                                                                   baseArgument.InConversionConvertibleOpt,
                                                                                   baseArgument.OutConversionConvertibleOpt,
                                                                                   semanticModel: null, arg.Syntax, IsImplicit(arg)));
                            }

                            propertyArguments = propertyArgumentsBuilder.ToImmutableAndFree();
                        }

                        Debug.Assert((propertyReference == null && propertyArguments.IsEmpty) ||
                                     (propertyArguments.Length == propertyReference.Arguments.Length));

                        // If there is control flow in the value being assigned, we want to make sure that
                        // the instance is captured appropriately, but the setter/field load in the reference will only be evaluated after
                        // the value has been evaluated. So we assemble the reference after visiting the value.

                        _evalStack.Push(Visit(memberReference.Instance));
                        return (success: true, ImmutableArray<IOperation>.CastUp(propertyArguments));

                    case OperationKind.ArrayElementReference:
                        var arrayReference = (IArrayElementReferenceOperation)instance;
                        ImmutableArray<IOperation> indicies = arrayReference.Indices.SelectAsArray(indexExpr =>
                        {
                            int captureId = VisitAndCapture(indexExpr);
                            return (IOperation)GetCaptureReference(captureId, indexExpr);
                        });
                        _evalStack.Push(Visit(arrayReference.ArrayReference));
                        return (success: true, indicies);

                    case OperationKind.DynamicIndexerAccess: // PROTOTYPE(dataflow): For now handle as invalid case to enable other test scenarios
                    case OperationKind.None: // PROTOTYPE(dataflow): For now handle as invalid case to enable other test scenarios.
                                             //                      see Microsoft.CodeAnalysis.CSharp.UnitTests.IOperationTests.ObjectCreationWithDynamicMemberInitializer_01
                    case OperationKind.Invalid:
                        return (success: false, arguments: ImmutableArray<IOperation>.Empty);

                    default:
                        // PROTOTYPE(dataflow): Probably it will be more robust to handle all remaining cases as an invalid case.
                        //                      Likely not worth it throwing for some edge error case. Asserting would be good though
                        //                      so that tests could catch the things we though are impossible.
                        throw ExceptionUtilities.UnexpectedValue(instance.Kind);
                }
            }

            IOperation popTarget(IOperation originalTarget, ImmutableArray<IOperation> arguments)
            {
                IOperation instance = _evalStack.Pop();
                switch (originalTarget.Kind)
                {
                    case OperationKind.FieldReference:
                        var fieldReference = (IFieldReferenceOperation)originalTarget;
                        return new FieldReferenceExpression(fieldReference.Field, fieldReference.IsDeclaration, instance, semanticModel: null,
                                                            fieldReference.Syntax, fieldReference.Type, fieldReference.ConstantValue, IsImplicit(fieldReference));
                    case OperationKind.EventReference:
                        var eventReference = (IEventReferenceOperation)originalTarget;
                        return new EventReferenceExpression(eventReference.Event, instance, semanticModel: null, eventReference.Syntax,
                                                            eventReference.Type, eventReference.ConstantValue, IsImplicit(eventReference));
                    case OperationKind.PropertyReference:
                        var propertyReference = (IPropertyReferenceOperation)originalTarget;
                        Debug.Assert(propertyReference.Arguments.Length == arguments.Length);
                        var castArguments = arguments.CastDown<IOperation, IArgumentOperation>();
                        return new PropertyReferenceExpression(propertyReference.Property, instance, castArguments, semanticModel: null, propertyReference.Syntax,
                                                               propertyReference.Type, propertyReference.ConstantValue, IsImplicit(propertyReference));
                    case OperationKind.ArrayElementReference:
                        Debug.Assert(((IArrayElementReferenceOperation)originalTarget).Indices.Length == arguments.Length);
                        return new ArrayElementReferenceExpression(instance, arguments, semanticModel: null, originalTarget.Syntax, originalTarget.Type, originalTarget.ConstantValue, IsImplicit(originalTarget));
                    default:
                        throw ExceptionUtilities.UnexpectedValue(originalTarget.Kind);
                }
            }
        }

        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): It looks like we need to handle the standalone case, happens in error scenarios,
            //                      see DefaultValueNonNullForNullableParameterTypeWithMissingNullableReference_IndexerInObjectCreationInitializer unit-test.
            //                      For now will just visit children and recreate the node, should add tests to confirm that this is an appropriate
            //                      rewrite (probably not appropriate, see comment in VisitMemberInitializer).  

            IOperation save = _currentImplicitInstance;
            _currentImplicitInstance = null;
            PushArray(operation.Initializers);
            _currentImplicitInstance = save;
            return new ObjectOrCollectionInitializerExpression(PopArray(operation.Initializers), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, operation.IsImplicit);
        }

        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, int? captureIdForResult)
        {
            // PROTOTYPE(dataflow): It looks like this is reachable at the moment for error scenarios.
            //                      See Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenTupleTests.SimpleTupleNew2
            //                      Going to return invalid operation for now to unblock testing.
            //throw ExceptionUtilities.Unreachable;
            return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
        }

        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            // PROTOTYPE(dataflow): Should we do anything special with attributes and parameter initializers?
            //                      Should we also expose graphs for those only as "nested" graphs?
            Debug.Assert(_currentRegion.AllowMethods);
            _currentRegion.Add(operation.Symbol, operation);
            return null;
        }

        private IOperation VisitLocalFunctionAsRoot(ILocalFunctionOperation operation)
        {
            Debug.Assert(_currentStatement == null);
            VisitMethodBodies(operation.Body, operation.IgnoredBody);
            return null;
        }

        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, int? captureIdForResult)
        {
            RegionBuilder owner = _currentRegion;

            while(!owner.AllowMethods)
            {
                owner = owner.Enclosing;
            }

            owner.Add(operation.Symbol, operation);
            return new FlowAnonymousFunctionOperation(operation.Symbol, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitArrayCreation(IArrayCreationOperation operation, int? captureIdForResult)
        {
            // We have couple of options on how to rewrite an array creation with an initializer:
            //       1) Retain the original tree shape so the visited IArrayCreationOperation still has an IArrayInitializerOperation child node.
            //       2) Lower the IArrayCreationOperation so it always has a null initializer, followed by explicit assignments
            //          of the form "IArrayElementReference = value" for the array initializer values.
            //          There will be no IArrayInitializerOperation in the tree with approach.
            //
            //  We are going ahead with approach #1 for couple of reasons:
            //  1. Simplicity: The implementation is much simpler, and has a lot lower risk associated with it.
            //  2. Lack of array instance access in the initializer: Unlike the object/collection initializer scenario,
            //     where the initializer can access the instance being initialized, array initializer does not have access
            //     to the array instance being initialized, and hence it does not matter if the array allocation is done
            //     before visiting the initializers or not.
            //
            //  In future, based on the customer feedback, we can consider switching to approach #2 and lower the initializer into assignment(s).

            PushArray(operation.DimensionSizes);
            IArrayInitializerOperation visitedInitializer = Visit(operation.Initializer);
            ImmutableArray<IOperation> visitedDimensions = PopArray(operation.DimensionSizes);
            return new ArrayCreationExpression(visitedDimensions, visitedInitializer, semanticModel: null,
                                               operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, int? captureIdForResult)
        {
            visitAndPushArrayInitializerValues(operation);
            return popAndAssembleArrayInitializerValues(operation);

            void visitAndPushArrayInitializerValues(IArrayInitializerOperation initializer)
            {
                foreach (IOperation elementValue in initializer.ElementValues)
                {
                    // We need to retain the tree shape for nested array initializer.
                    if (elementValue.Kind == OperationKind.ArrayInitializer)
                    {
                        visitAndPushArrayInitializerValues((IArrayInitializerOperation)elementValue);
                    }
                    else
                    {
                        _evalStack.Push(Visit(elementValue));
                    }
                }
            }

            IArrayInitializerOperation popAndAssembleArrayInitializerValues(IArrayInitializerOperation initializer)
            {
                var builder = ArrayBuilder<IOperation>.GetInstance(initializer.ElementValues.Length);
                for (int i = initializer.ElementValues.Length - 1; i >= 0; i--)
                {
                    IOperation elementValue = initializer.ElementValues[i];

                    IOperation visitedElementValue;
                    if (elementValue.Kind == OperationKind.ArrayInitializer)
                    {
                        visitedElementValue = popAndAssembleArrayInitializerValues((IArrayInitializerOperation)elementValue);
                    }
                    else
                    {
                        visitedElementValue = _evalStack.Pop();
                    }

                    builder.Add(visitedElementValue);
                }

                builder.ReverseContents();
                return new ArrayInitializer(builder.ToImmutableAndFree(), semanticModel: null, initializer.Syntax, initializer.ConstantValue, IsImplicit(initializer));
            }
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, int? captureIdForResult)
        {
            if (operation.ReferenceKind == InstanceReferenceKind.ImplicitReceiver)
            {
                // When we're in an object or collection initializer, we need to replace the instance reference with a reference to the object being initialized
                Debug.Assert(operation.IsImplicit);

                if (_currentImplicitInstance != null)
                {
                    return OperationCloner.CloneOperation(_currentImplicitInstance);
                }
                else
                {
                    // PROTOTYPE(dataflow): We can get here in some error scenarios, for example
                    //                      Microsoft.CodeAnalysis.CSharp.UnitTests.IOperationTests.DefaultValueNonNullForNullableParameterTypeWithMissingNullableReference_IndexerInObjectCreationInitializer
                    //                      To enable other test scenarios, I will simply produce an invalid operation, but we need to confirm with specific tests that this is good enough
                    return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
                }
            }
            else
            {
                return new InstanceReferenceExpression(operation.ReferenceKind, semanticModel: null, operation.Syntax, operation.Type, 
                                                       operation.ConstantValue, IsImplicit(operation));
            }
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, int? captureIdForResult)
        {
            if (operation.Operation != null)
            {
                if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
                {
                    var instance = ((IDynamicMemberReferenceOperation)operation.Operation).Instance;
                    if (instance != null)
                    {
                        _evalStack.Push(Visit(instance));
                    }
                }
                else
                {
                    _evalStack.Push(Visit(operation.Operation));
                }
            }

            PushArray(operation.Arguments);
            ImmutableArray<IOperation> rewrittenArguments = PopArray(operation.Arguments);

            IOperation rewrittenOperation;
            if (operation.Operation == null)
            {
                rewrittenOperation = null;
            }
            else if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
            {
                var dynamicMemberReference = (IDynamicMemberReferenceOperation)operation.Operation;
                IOperation rewrittenInstance = dynamicMemberReference.Instance != null ? _evalStack.Pop() : null;
                rewrittenOperation = new DynamicMemberReferenceExpression(rewrittenInstance, dynamicMemberReference.MemberName, dynamicMemberReference.TypeArguments,
                    dynamicMemberReference.ContainingType, semanticModel: null, dynamicMemberReference.Syntax, dynamicMemberReference.Type, dynamicMemberReference.ConstantValue, IsImplicit(dynamicMemberReference));
            }
            else
            {
                rewrittenOperation = _evalStack.Pop();
            }

            return new DynamicInvocationExpression(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, int? captureIdForResult)
        {
            if (operation.Operation != null)
            {
                _evalStack.Push(Visit(operation.Operation));
            }

            PushArray(operation.Arguments);
            ImmutableArray<IOperation> rewrittenArguments = PopArray(operation.Arguments);
            IOperation rewrittenOperation = operation.Operation != null ? _evalStack.Pop() : null;

            return new DynamicIndexerAccessExpression(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, int? captureIdForResult)
        {
            return new DynamicMemberReferenceExpression(Visit(operation.Instance), operation.MemberName, operation.TypeArguments,
                operation.ContainingType, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, int? captureIdForResult)
        {
            (IOperation visitedTarget, IOperation visitedValue) = VisitPreservingTupleOperations(operation.Target, operation.Value);
            return new DeconstructionAssignmentExpression(visitedTarget, visitedValue, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
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
                return new TupleExpression(elementBuilder.ToImmutableAndFree(), semanticModel: null, tuple.Syntax, tuple.Type, tuple.NaturalType, tuple.ConstantValue, IsImplicit(tuple));
            }
            else
            {
                return _evalStack.Pop();
            }
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, int? captureIdForResult)
        {
            return new DeclarationExpression(VisitPreservingTupleOperations(operation.Expression), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        private IOperation VisitPreservingTupleOperations(IOperation operation)
        {
            PushTargetAndUnwrapTupleIfNecessary(operation);
            return PopTargetAndWrapTupleIfNecessary(operation);
        }

        private (IOperation visitedLeft, IOperation visitedRight) VisitPreservingTupleOperations(IOperation left, IOperation right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // If the left is a tuple, we want to decompose the tuple and push each element back onto the stack, so that if the right
            // has control flow the individual elements are captured. Then we can recompose the tuple after the right has been visited.
            // We do this to keep the graph sane, so that users don't have to track a tuple captured via flow control when it's not really
            // the tuple that's been captured, it's the operands to the tuple.
            PushTargetAndUnwrapTupleIfNecessary(left);
            IOperation visitedRight = Visit(right);
            IOperation visitedLeft = PopTargetAndWrapTupleIfNecessary(left);
            return (visitedLeft, visitedRight);
        }

        public override IOperation VisitTuple(ITupleOperation operation, int? captureIdForResult)
        {
            return VisitPreservingTupleOperations(operation);
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
                return Operation.CreateOperationNone(semanticModel: null, operation.Syntax, operation.ConstantValue, ImmutableArray<IOperation>.Empty, IsImplicit(operation));
            }

            var childrenBuilder = ArrayBuilder<IOperation>.GetInstance(numChildren);
            for (int i = 0; i < numChildren; i++)
            {
                childrenBuilder.Add(_evalStack.Pop());
            }

            childrenBuilder.ReverseContents();

            return Operation.CreateOperationNone(semanticModel: null, operation.Syntax, operation.ConstantValue, childrenBuilder.ToImmutableAndFree(), IsImplicit(operation));
        }

        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, int? captureIdForResult)
        {
            // We visit and rewrite the interpolation parts in two phases:
            //  1. Visit all the non-literal parts of the interpolation and push them onto the eval stack.
            //  2. Traverse the parts in reverse order, popping the non-literal values from the eval stack and visiting the literal values.

            foreach (IInterpolatedStringContentOperation element in operation.Parts)
            {
                if (element.Kind == OperationKind.Interpolation)
                {
                    var interpolation = (IInterpolationOperation)element;
                    _evalStack.Push(Visit(interpolation.Expression));

                    if (interpolation.Alignment != null)
                    {
                        _evalStack.Push(Visit(interpolation.Alignment));
                    }
                }
            }

            var partsBuilder = ArrayBuilder<IInterpolatedStringContentOperation>.GetInstance(operation.Parts.Length);
            for (int i = operation.Parts.Length - 1; i >= 0; i--)
            {
                IInterpolatedStringContentOperation element = operation.Parts[i];
                IInterpolatedStringContentOperation rewrittenElement;
                if (element.Kind == OperationKind.Interpolation)
                {
                    var interpolation = (IInterpolationOperation)element;

                    IOperation rewrittenFormatString;
                    if (interpolation.FormatString != null)
                    {
                        Debug.Assert(interpolation.FormatString.Kind == OperationKind.Literal);
                        rewrittenFormatString = VisitLiteral((ILiteralOperation)interpolation.FormatString, captureIdForResult: null);
                    }
                    else
                    {
                        rewrittenFormatString = null;
                    }

                    var rewrittenAlignment = interpolation.Alignment != null ? _evalStack.Pop() : null;
                    var rewrittenExpression = _evalStack.Pop();
                    rewrittenElement = new Interpolation(rewrittenExpression, rewrittenAlignment, rewrittenFormatString, semanticModel: null, element.Syntax,
                                                         element.Type, element.ConstantValue, IsImplicit(element));
                }
                else
                {
                    var interpolatedStringText = (IInterpolatedStringTextOperation)element;
                    Debug.Assert(interpolatedStringText.Text.Kind == OperationKind.Literal);
                    var rewrittenInterpolationText = VisitLiteral((ILiteralOperation)interpolatedStringText.Text, captureIdForResult: null);
                    rewrittenElement = new InterpolatedStringText(rewrittenInterpolationText, semanticModel: null, element.Syntax, element.Type, element.ConstantValue, IsImplicit(element));
                }

                partsBuilder.Add(rewrittenElement);
            }

            partsBuilder.ReverseContents();
            return new InterpolatedStringExpression(partsBuilder.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitNameOf(INameOfOperation operation, int? captureIdForResult)
        {
            Debug.Assert(operation.ConstantValue.HasValue);
            return new LiteralExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, int? captureIdForResult)
        {
            return new LiteralExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, int? captureIdForResult)
        {
            return new LocalReferenceExpression(operation.Local, operation.IsDeclaration, semanticModel: null, operation.Syntax,
                                                operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, int? captureIdForResult)
        {
            return new ParameterReferenceExpression(operation.Parameter, semanticModel: null, operation.Syntax,
                                                    operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Field.IsStatic ? null : Visit(operation.Instance);
            return new FieldReferenceExpression(operation.Field, operation.IsDeclaration, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Method.IsStatic ? null : Visit(operation.Instance);
            return new MethodReferenceExpression(operation.Method, operation.IsVirtual, visitedInstance, semanticModel: null,
                                                 operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, int? captureIdForResult)
        {
            IOperation instance = operation.Property.IsStatic ? null : operation.Instance;
            (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            return new PropertyReferenceExpression(operation.Property, visitedInstance, visitedArguments, semanticModel: null,
                                                   operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Event.IsStatic ? null : Visit(operation.Instance);
            return new EventReferenceExpression(operation.Event, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, int? captureIdForResult)
        {
            return new TypeOfExpression(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, int? captureIdForResult)
        {
            return new ParenthesizedExpression(Visit(operation.Operand), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitAwait(IAwaitOperation operation, int? captureIdForResult)
        {
            return new AwaitExpression(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, int? captureIdForResult)
        {
            return new SizeOfExpression(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }
        
        public override IOperation VisitStop(IStopOperation operation, int? captureIdForResult)
        {
            return new StopStatement(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, int? captureIdForResult)
        {
            return new IsTypeExpression(Visit(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            var parameterRef = new ParameterReferenceExpression(operation.Parameter, semanticModel: null,
                operation.Syntax, operation.Parameter.Type, constantValue: default, isImplicit: true);
            VisitInitializer(rewrittenTarget: parameterRef, initializer: operation);
            return null;
        }

        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            foreach (IFieldSymbol fieldSymbol in operation.InitializedFields)
            {
                IInstanceReferenceOperation instance = fieldSymbol.IsStatic ?
                    null :
                    new InstanceReferenceExpression(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
                        operation.Syntax, fieldSymbol.ContainingType, constantValue: default, isImplicit: true);
                var fieldRef = new FieldReferenceExpression(fieldSymbol, isDeclaration: false, instance, semanticModel: null,
                    operation.Syntax, fieldSymbol.Type, constantValue: default, isImplicit: true);
                VisitInitializer(rewrittenTarget: fieldRef, initializer: operation);
            }

            return null;
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            foreach (IPropertySymbol propertySymbol in operation.InitializedProperties)
            {
                var instance = propertySymbol.IsStatic ?
                    null :
                    new InstanceReferenceExpression(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
                        operation.Syntax, propertySymbol.ContainingType, constantValue: default, isImplicit: true);

                ImmutableArray<IArgumentOperation> arguments;
                if (!propertySymbol.Parameters.IsEmpty)
                {
                    // Must be an error case of initializing a property with parameters.
                    var builder = ArrayBuilder<IArgumentOperation>.GetInstance(propertySymbol.Parameters.Length);
                    foreach (var parameter in propertySymbol.Parameters)
                    {
                        var value = new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null,
                            operation.Syntax, parameter.Type, constantValue: default, isImplicit: true);
                        var argument = new ArgumentOperation(value, ArgumentKind.Explicit, parameter, inConversionOpt: null,
                            outConversionOpt: null, semanticModel: null, operation.Syntax, isImplicit: true);
                        builder.Add(argument);
                    }

                    arguments = builder.ToImmutableAndFree();
                }
                else
                {
                    arguments = ImmutableArray<IArgumentOperation>.Empty;
                }

                IOperation propertyRef = new PropertyReferenceExpression(propertySymbol, instance, arguments,
                    semanticModel: null, operation.Syntax, propertySymbol.Type, constantValue: default, isImplicit: true);
                VisitInitializer(rewrittenTarget: propertyRef, initializer: operation);
            }

            return null;
        }

        private void VisitInitializer(IOperation rewrittenTarget, ISymbolInitializerOperation initializer)
        {
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: initializer.Locals));

            var assignment = new SimpleAssignmentExpression(rewrittenTarget, isRef: false, Visit(initializer.Value), semanticModel: null,
                    initializer.Syntax, rewrittenTarget.Type, constantValue: default, isImplicit: true);
            AddStatement(assignment);

            LeaveRegion();
    	}

        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, int? captureIdForResult)
        {
            IOperation visitedEventReference, visitedHandler;

            // Get the IEventReferenceOperation, digging through IParenthesizedOperation.
            // Note that for error cases, the event reference might be an IInvalidOperation.
            IEventReferenceOperation eventReference = getEventReference();
            if (eventReference != null)
            {
                // Preserve the IEventReferenceOperation.
                var eventReferenceInstance = eventReference.Event.IsStatic ? null : eventReference.Instance;
                if (eventReferenceInstance != null)
                {
                    _evalStack.Push(Visit(eventReferenceInstance));
                }

                visitedHandler = Visit(operation.HandlerValue);

                IOperation visitedInstance = eventReferenceInstance == null ? null : _evalStack.Pop();
                visitedEventReference = new EventReferenceExpression(eventReference.Event, visitedInstance,
                    semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, operation.EventReference.ConstantValue, IsImplicit(operation.EventReference));
            }
            else
            {
                Debug.Assert(operation.EventReference != null);
                
                _evalStack.Push(Visit(operation.EventReference));
                visitedHandler = Visit(operation.HandlerValue);
                visitedEventReference = _evalStack.Pop();
            }
            
            return new EventAssignmentOperation(visitedEventReference, visitedHandler, operation.Adds, semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));

            IEventReferenceOperation getEventReference()
            {
                IOperation current = operation.EventReference;

                while (true)
                {
                    switch (current.Kind)
                    {
                        case OperationKind.EventReference:
                            return (IEventReferenceOperation)current;

                        case OperationKind.Parenthesized:
                            current = ((IParenthesizedOperation)current).Operand;
                            continue;

                        default:
                            return null;
                    }
                }
            }
        }

        public override IOperation VisitRaiseEvent(IRaiseEventOperation operation, int? captureIdForResult)
        {
            Debug.Assert(_currentStatement == operation);

            var instance = operation.EventReference.Event.IsStatic ? null : operation.EventReference.Instance;
            if (instance != null)
            {
                _evalStack.Push(Visit(instance));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(operation.Arguments);
            IOperation visitedInstance = instance == null ? null : _evalStack.Pop();
            var visitedEventReference = new EventReferenceExpression(operation.EventReference.Event, visitedInstance,
                semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, operation.EventReference.ConstantValue, IsImplicit(operation.EventReference));
            return new RaiseEventStatement(visitedEventReference, visitedArguments, semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, int? captureIdForResult)
        {
            return new AddressOfExpression(Visit(operation.Reference), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, int? captureIdForResult)
        {
            bool isDecrement = operation.Kind == OperationKind.Decrement;
            return new IncrementExpression(isDecrement, operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.OperatorMethod, 
                semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDiscardOperation(IDiscardOperation operation, int? captureIdForResult)
        {
            return new DiscardOperation(operation.DiscardSymbol, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, int? captureIdForResult)
        {
            return new OmittedArgumentExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        internal override IOperation VisitPlaceholder(IPlaceholderOperation operation, int? captureIdForResult)
        {
            switch (operation.PlaceholderKind)
            {
                case PlaceholderKind.SwitchOperationExpression:
                    if (_currentSwitchOperationExpression != null)
                    {
                        return OperationCloner.CloneOperation(_currentSwitchOperationExpression);
                    }
                    break;
                case PlaceholderKind.ForToLoopBinaryOperatorLeftOperand:
                    if (_forToLoopBinaryOperatorLeftOperand != null)
                    {
                        return _forToLoopBinaryOperatorLeftOperand;
                    }
                    break;
                case PlaceholderKind.ForToLoopBinaryOperatorRightOperand:
                    if (_forToLoopBinaryOperatorRightOperand != null)
                    {
                        return _forToLoopBinaryOperatorRightOperand;
                    }
                    break;
                case PlaceholderKind.AggregationGroup:
                    if (_currentAggregationGroup != null)
                    {
                        return OperationCloner.CloneOperation(_currentAggregationGroup);
                    }
                    break;
            }

            Debug.Assert(false, "All placeholders should be handled above. Have we introduced a new scenario where placeholders are used?");
            return new PlaceholderExpression(operation.PlaceholderKind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, int? captureIdForResult)
        {
            _evalStack.Push(Visit(operation.Value));
            IPatternOperation visitedPattern = Visit(operation.Pattern);
            IOperation visitedValue = _evalStack.Pop();
            return new IsPatternExpression(visitedValue, visitedPattern, semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, int? captureIdForResult)
        {
            return new ConstantPattern(Visit(operation.Value), semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, int? captureIdForResult)
        {
            return new DeclarationPattern(operation.DeclaredSymbol, semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, int? captureIdForResult)
        {
            return new DelegateCreationExpression(Visit(operation.Target), semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
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
            // PROTOTYPE(dataflow): All usages of this should be removed the following line uncommented when support is added for object creation, property reference, and raise events.
            // throw ExceptionUtilities.Unreachable;
            var baseArgument = (BaseArgument)operation;
            return new ArgumentOperation(Visit(operation.Value), operation.ArgumentKind, operation.Parameter, baseArgument.InConversionConvertibleOpt, baseArgument.OutConversionConvertibleOpt, semanticModel: null, operation.Syntax, IsImplicit(operation));
        }

        public override IOperation VisitConversion(IConversionOperation operation, int? captureIdForResult)
        {
            return new ConversionOperation(Visit(operation.Operand), ((BaseConversionExpression)operation).ConvertibleConversion, operation.IsTryCast, operation.IsChecked, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        internal override IOperation VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, int? captureIdForResult)
        {
            return new PointerIndirectionReferenceExpression(Visit(operation.Pointer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, int? captureIdForResult)
        {
            return new AnonymousObjectCreationExpression(VisitArray(operation.Initializers), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, int? captureIdForResult)
        {
            return new DynamicObjectCreationExpression(VisitArray(operation.Arguments), ((HasDynamicArgumentsExpression)operation).ArgumentNames, ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, int? captureIdForResult)
        {
            return new DefaultValueExpression(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, int? captureIdForResult)
        {
            return new TypeParameterObjectCreationExpression(Visit(operation.Initializer), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, int? captureIdForResult)
        {
            return new InvalidOperation(VisitArray(operation.Children.ToImmutableArray()), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, int? captureIdForResult)
        {
            return new TranslatedQueryExpression(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        #endregion
    }
}
