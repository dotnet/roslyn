﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly Compilation _compilation;
        private readonly BasicBlockBuilder _entry = new BasicBlockBuilder(BasicBlockKind.Entry);
        private readonly BasicBlockBuilder _exit = new BasicBlockBuilder(BasicBlockKind.Exit);

        private ArrayBuilder<BasicBlockBuilder> _blocks;
        private PooledDictionary<BasicBlockBuilder, RegionBuilder> _regionMap;
        private BasicBlockBuilder _currentBasicBlock;
        private RegionBuilder _currentRegion;
        private PooledDictionary<ILabelSymbol, BasicBlockBuilder> _labeledBlocks;
        private bool _haveAnonymousFunction;

        private IOperation _currentStatement;
        private ArrayBuilder<(EvalStackFrame frameOpt, IOperation operationOpt)> _evalStack;
        private int _startSpillingAt;
        private IOperation _currentConditionalAccessInstance;
        private IOperation _currentSwitchOperationExpression;
        private IOperation _forToLoopBinaryOperatorLeftOperand;
        private IOperation _forToLoopBinaryOperatorRightOperand;
        private IOperation _currentAggregationGroup;
        private bool _forceImplicit; // Force all rewritten nodes to be marked as implicit regardless of their original state.

        private readonly CaptureIdDispenser _captureIdDispenser;

        /// <summary>
        /// Holds the current object being initialized if we're visiting an object initializer.
        /// Or the current anonymous type object being initialized if we're visiting an anonymous type object initializer.
        /// Or the target of a VB With statement.
        /// </summary>
        private ImplicitInstanceInfo _currentImplicitInstance;

        private ControlFlowGraphBuilder(Compilation compilation, CaptureIdDispenser captureIdDispenser)
        {
            Debug.Assert(compilation != null);
            _compilation = compilation;
            _captureIdDispenser = captureIdDispenser ?? new CaptureIdDispenser();
        }

        private bool IsImplicit(IOperation operation)
        {
            return _forceImplicit || operation.IsImplicit;
        }

        public static ControlFlowGraph Create(IOperation body, ControlFlowGraph parent = null, ControlFlowRegion enclosing = null, CaptureIdDispenser captureIdDispenser = null, in Context context = default)
        {
            Debug.Assert(body != null);
            Debug.Assert(((Operation)body).OwningSemanticModel != null);

#if DEBUG
            if (enclosing == null)
            {
                Debug.Assert(body.Parent == null);
                Debug.Assert(body.Kind == OperationKind.Block ||
                    body.Kind == OperationKind.MethodBody ||
                    body.Kind == OperationKind.ConstructorBody ||
                    body.Kind == OperationKind.FieldInitializer ||
                    body.Kind == OperationKind.PropertyInitializer ||
                    body.Kind == OperationKind.ParameterInitializer,
                    $"Unexpected root operation kind: {body.Kind}");
                Debug.Assert(parent == null);
            }
            else
            {
                Debug.Assert(body.Kind == OperationKind.LocalFunction || body.Kind == OperationKind.AnonymousFunction);
                Debug.Assert(parent != null);
            }
#endif

            var builder = new ControlFlowGraphBuilder(((Operation)body).OwningSemanticModel.Compilation, captureIdDispenser);
            var blocks = ArrayBuilder<BasicBlockBuilder>.GetInstance();
            builder._blocks = blocks;
            builder._evalStack = ArrayBuilder<(EvalStackFrame frameOpt, IOperation operationOpt)>.GetInstance();
            builder._regionMap = PooledDictionary<BasicBlockBuilder, RegionBuilder>.GetInstance();

            var root = new RegionBuilder(ControlFlowRegionKind.Root);
            builder.EnterRegion(root);
            builder.AppendNewBlock(builder._entry, linkToPrevious: false);
            builder._currentBasicBlock = null;
            builder.SetCurrentContext(context);

            builder.EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime));

            switch (body.Kind)
            {
                case OperationKind.LocalFunction:
                    Debug.Assert(captureIdDispenser != null);
                    builder.VisitLocalFunctionAsRoot((ILocalFunctionOperation)body);
                    break;
                case OperationKind.AnonymousFunction:
                    Debug.Assert(captureIdDispenser != null);
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
            builder._currentImplicitInstance.Free();
            Debug.Assert(builder._currentRegion == null);

            CheckUnresolvedBranches(blocks, builder._labeledBlocks);
            Pack(blocks, root, builder._regionMap);
            var localFunctions = ArrayBuilder<IMethodSymbol>.GetInstance();
            var localFunctionsMap = ImmutableDictionary.CreateBuilder<IMethodSymbol, (ControlFlowRegion, ILocalFunctionOperation, int)>();
            ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion, int)>.Builder anonymousFunctionsMapOpt = null;

            if (builder._haveAnonymousFunction)
            {
                anonymousFunctionsMapOpt = ImmutableDictionary.CreateBuilder<IFlowAnonymousFunctionOperation, (ControlFlowRegion, int)>();
            }

            ControlFlowRegion region = root.ToImmutableRegionAndFree(blocks, localFunctions, localFunctionsMap, anonymousFunctionsMapOpt, enclosing);
            root = null;
            MarkReachableBlocks(blocks);

            Debug.Assert(builder._evalStack.Count == 0);
            builder._evalStack.Free();
            builder._regionMap.Free();
            builder._labeledBlocks?.Free();

            return new ControlFlowGraph(body, parent, builder._captureIdDispenser, ToImmutableBlocks(blocks), region,
                                        localFunctions.ToImmutableAndFree(), localFunctionsMap.ToImmutable(),
                                        anonymousFunctionsMapOpt?.ToImmutable() ?? ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion, int)>.Empty);
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
            // NOTE: This flow graph walking algorithm has been forked into Workspaces layer's
            //       implementation of "CustomDataFlowAnalysis",
            //       we should keep them in sync as much as possible.
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
                            r.Locals.IsEmpty && !r.HasLocalFunctions && !r.HasCaptureIds)
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
                                region.AddRange(subRegion.LocalFunctions);
                                region.AddCaptureIds(subRegion.CaptureIds);
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

                                if (subRegion.Kind == ControlFlowRegionKind.LocalLifetime && !subRegion.HasLocalFunctions &&
                                    !subRegion.HasRegions && subRegion.FirstBlock == subRegion.LastBlock)
                                {
                                    BasicBlockBuilder block = subRegion.FirstBlock;

                                    if (!block.HasStatements && block.BranchValue == null)
                                    {
                                        Debug.Assert(!subRegion.HasCaptureIds);

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
        private static void MergeSubRegionAndFree(RegionBuilder subRegion, ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<BasicBlockBuilder, RegionBuilder> regionMap, bool canHaveEmptyRegion = false)
        {
            Debug.Assert(subRegion.Kind != ControlFlowRegionKind.Root);
            RegionBuilder enclosing = subRegion.Enclosing;

#if DEBUG
            subRegion.AboutToFree();
#endif

            if (subRegion.FirstBlock is null)
            {
                Debug.Assert(canHaveEmptyRegion);
                Debug.Assert(!subRegion.HasRegions);

                enclosing.Remove(subRegion);
                subRegion.Free();
                return;
            }

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
                // pass until no such changes are made.
                retry = false;

                int count = blocks.Count - 1;
                for (int i = 1; i < count; i++)
                {
                    BasicBlockBuilder block = blocks[i];
                    block.Ordinal = i;

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

                                // If .try region has locals or methods or captures, let's convert it to .locals, otherwise drop it
                                if (@try.Locals.IsEmpty && !@try.HasLocalFunctions && !@try.HasCaptureIds)
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
#if DEBUG
            int stackDepth = _evalStack.Count;
            Debug.Assert(stackDepth == 0 || _evalStack.Peek().frameOpt != null);
#endif
            if (operation == null)
            {
                return;
            }

            IOperation saveCurrentStatement = _currentStatement;
            _currentStatement = operation;

            EvalStackFrame frame = PushStackFrame();
            AddStatement(base.Visit(operation, null));
            PopStackFrameAndLeaveRegion(frame);
#if DEBUG
            Debug.Assert(_evalStack.Count == stackDepth);
            Debug.Assert(stackDepth == 0 || _evalStack.Peek().frameOpt != null);
#endif
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
                slot => slot.operationOpt == null
                    || slot.operationOpt.Kind == OperationKind.FlowCaptureReference
                    || slot.operationOpt.Kind == OperationKind.DeclarationExpression
                    || slot.operationOpt.Kind == OperationKind.Discard
                    || slot.operationOpt.Kind == OperationKind.OmittedArgument));
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

        private void EnterRegion(RegionBuilder region, bool spillingStack = false)
        {
            if (!spillingStack)
            {
                // Make sure all pending stack spilling regions are realised
                SpillEvalStack();
#if DEBUG
                Debug.Assert(_evalStack.Count == _startSpillingAt);
                VerifySpilledStackFrames();
#endif
            }

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

#if DEBUG
            // We shouldn't be leaving regions that are still associated with stack frames
            foreach ((EvalStackFrame frameOpt, IOperation operationOpt) in _evalStack)
            {
                Debug.Assert((frameOpt == null) != (operationOpt == null));

                if (frameOpt != null)
                {
                    Debug.Assert(enclosed != frameOpt.RegionBuilderOpt);
                }
            }
#endif
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

        private void UnconditionalBranch(BasicBlockBuilder nextBlock)
        {
            LinkBlocks(CurrentBasicBlock, nextBlock);
            _currentBasicBlock = null;
        }

        public override IOperation VisitBlock(IBlockOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));
            VisitStatements(operation.Operations);
            LeaveRegion();

            return FinishVisitingStatement(operation);
        }

        private void StartVisitingStatement(IOperation operation)
        {
            Debug.Assert(_currentStatement == operation);
            Debug.Assert(_evalStack.Count == 0 || _evalStack.Peek().frameOpt != null);
            SpillEvalStack();
        }

        private IOperation FinishVisitingStatement(IOperation originalOperation, IOperation result = null)
        {
            Debug.Assert(((Operation)originalOperation).OwningSemanticModel != null, "Not an original node.");
            Debug.Assert(_currentStatement == originalOperation);
            Debug.Assert(_evalStack.Count == 0 || _evalStack.Peek().frameOpt != null);

            if (_currentStatement == originalOperation)
            {
                return result;
            }

            return result ?? MakeInvalidOperation(originalOperation.Syntax, originalOperation.Type, ImmutableArray<IOperation>.Empty);
        }

        private void VisitStatements(ImmutableArray<IOperation> statements)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                if (VisitStatementsOneOrAll(statements[i], statements, i))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Either visits a single operation, or a using <see cref="IVariableDeclarationGroupOperation"/> and all subsequent statements
        /// </summary>
        /// <param name="operation">The statement to visit</param>
        /// <param name="statements">All statements in the block containing this node</param>
        /// <param name="startIndex">The current statement being visited in <paramref name="statements"/></param>
        /// <returns>True if this visited all of the statements</returns>
        /// <remarks>
        /// The operation being visited is not necessarily equal to statements[startIndex]. 
        /// When traversing down a set of labels, we set operation to the label.Operation and recurse, but statements[startIndex] still refers to the original parent label 
        /// as we haven't actually moved down the original statement list
        /// </remarks>
        private bool VisitStatementsOneOrAll(IOperation operation, ImmutableArray<IOperation> statements, int startIndex)
        {
            switch (operation)
            {
                case IUsingDeclarationOperation usingDeclarationOperation:
                    var followingStatements = ImmutableArray.Create(statements, startIndex + 1, statements.Length - startIndex - 1);
                    VisitUsingVariableDeclarationOperation(usingDeclarationOperation, followingStatements);
                    return true;
                case ILabeledOperation { Operation: { } } labelOperation:
                    return visitPossibleUsingDeclarationInLabel(labelOperation);
                default:
                    VisitStatement(operation);
                    return false;
            }

            bool visitPossibleUsingDeclarationInLabel(ILabeledOperation labelOperation)
            {
                var savedCurrentStatement = _currentStatement;
                _currentStatement = labelOperation;

                StartVisitingStatement(labelOperation);
                VisitLabel(labelOperation.Label);
                bool visitedAll = VisitStatementsOneOrAll(labelOperation.Operation, statements, startIndex);
                FinishVisitingStatement(labelOperation);

                _currentStatement = savedCurrentStatement;
                return visitedAll;
            }
        }

        internal override IOperation VisitWithStatement(IWithStatementOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            ImplicitInstanceInfo previousInitializedInstance = _currentImplicitInstance;
            _currentImplicitInstance = new ImplicitInstanceInfo(VisitAndCapture(operation.Value));

            VisitStatement(operation.Body);

            _currentImplicitInstance = previousInitializedInstance;
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitConstructorBodyOperation(IConstructorBodyOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            if (operation.Initializer != null)
            {
                VisitStatement(operation.Initializer);
            }

            VisitMethodBodyBaseOperation(operation);

            LeaveRegion();
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitMethodBodyOperation(IMethodBodyOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            VisitMethodBodyBaseOperation(operation);
            return FinishVisitingStatement(operation);
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
                    UnconditionalBranch(_exit);

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
                    VisitConditionalBranch(operation.Condition, ref afterIf, jumpIfTrue: false);
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
                    VisitConditionalBranch(operation.Condition, ref whenFalse, jumpIfTrue: false);

                    VisitStatement(operation.WhenTrue);

                    var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                    UnconditionalBranch(afterIf);

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
                // result = capture

                SpillEvalStack();

                BasicBlockBuilder whenFalse = null;
                VisitConditionalBranch(operation.Condition, ref whenFalse, jumpIfTrue: false);

                var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                IOperation result;

                // Specially handle cases with "throw" as operation.WhenTrue or operation.WhenFalse. We don't need to create an additional
                // capture for the result because there won't be any result from the throwing branches.
                if (operation.WhenTrue is IConversionOperation whenTrueConversion && whenTrueConversion.Operand.Kind == OperationKind.Throw)
                {
                    IOperation rewrittenThrow = base.Visit(whenTrueConversion.Operand, null);
                    Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                    Debug.Assert(rewrittenThrow.Children.IsEmpty());

                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    result = Visit(operation.WhenFalse);
                }
                else if (operation.WhenFalse is IConversionOperation whenFalseConversion && whenFalseConversion.Operand.Kind == OperationKind.Throw)
                {
                    result = Visit(operation.WhenTrue);

                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    IOperation rewrittenThrow = base.Visit(whenFalseConversion.Operand, null);
                    Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                    Debug.Assert(rewrittenThrow.Children.IsEmpty());
                }
                else
                {
                    var resultCaptureRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, isStackSpillRegion: true);
                    EnterRegion(resultCaptureRegion);

                    int captureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

                    VisitAndCapture(operation.WhenTrue, captureId);

                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    VisitAndCapture(operation.WhenFalse, captureId);

                    result = GetCaptureReference(captureId, operation);
                }

                AppendNewBlock(afterIf);

                return result;
            }
        }

        private void VisitAndCapture(IOperation operation, int captureId)
        {
            EvalStackFrame frame = PushStackFrame();
            IOperation result = base.Visit(operation, captureId);
            PopStackFrame(frame);
            CaptureResultIfNotAlready(operation.Syntax, captureId, result);
            LeaveRegionIfAny(frame);
        }

        private IOperation VisitAndCapture(IOperation operation)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(base.Visit(operation, null));
            SpillEvalStack();
            return PopStackFrame(frame, PopOperand());
        }

        private void CaptureResultIfNotAlready(SyntaxNode syntax, int captureId, IOperation result)
        {
            Debug.Assert(_startSpillingAt == _evalStack.Count);
            if (result.Kind != OperationKind.FlowCaptureReference ||
                captureId != ((IFlowCaptureReferenceOperation)result).Id.Value)
            {
                SpillEvalStack();
                AddStatement(new FlowCaptureOperation(captureId, syntax, result));
            }
        }

        /// <summary>
        /// This class captures information about beginning of stack frame
        /// and corresponding <see cref="RegionBuilder"/> if one was allocated to
        /// track <see cref="CaptureId"/>s used by the stack spilling, etc.
        /// Do not create instances of this type manually, use <see cref="PushStackFrame"/>
        /// helper instead. Also, do not assign <see cref="RegionBuilderOpt"/> explicitly.
        /// Let the builder machinery do this when appropriate.
        /// </summary>
        private class EvalStackFrame
        {
            private RegionBuilder _lazyRegionBuilder;

            public RegionBuilder RegionBuilderOpt
            {
                get
                {
                    return _lazyRegionBuilder;
                }
                set
                {
                    Debug.Assert(_lazyRegionBuilder == null);
                    Debug.Assert(value != null);
                    _lazyRegionBuilder = value;
                }
            }
        }

        private EvalStackFrame PushStackFrame()
        {
            var frame = new EvalStackFrame();
            _evalStack.Push((frame, operationOpt: null));
            return frame;
        }

        private void PopStackFrame(EvalStackFrame frame, bool mergeNestedRegions = true)
        {
            Debug.Assert(frame != null);
            int stackDepth = _evalStack.Count;
            Debug.Assert(_startSpillingAt <= stackDepth);

            if (_startSpillingAt == stackDepth)
            {
                _startSpillingAt--;
            }

            (EvalStackFrame frameOpt, IOperation operationOpt) = _evalStack.Pop();
            Debug.Assert(frame == frameOpt);
            Debug.Assert(operationOpt == null);

            if (frame.RegionBuilderOpt != null && mergeNestedRegions)
            {
                while (_currentRegion != frame.RegionBuilderOpt)
                {
                    RegionBuilder toMerge = _currentRegion;
                    _currentRegion = toMerge.Enclosing;

                    Debug.Assert(toMerge.IsStackSpillRegion);
                    Debug.Assert(!toMerge.HasLocalFunctions);
                    Debug.Assert(toMerge.Locals.IsEmpty);

                    _currentRegion.AddCaptureIds(toMerge.CaptureIds);
                    // This region can be empty in certain error scenarios, such as `new T {}`, where T does not
                    // have a class constraint. There are no arguments or initializers, so nothing will have
                    // been put into the region at this point
                    if (toMerge.FirstBlock is null)
                    {
                        Debug.Assert(toMerge.LastBlock is null);
                    }
                    else
                    {
                        _currentRegion.ExtendToInclude(toMerge.LastBlock);
                    }
                    MergeSubRegionAndFree(toMerge, _blocks, _regionMap, canHaveEmptyRegion: true);
                }
            }
        }

        private void PopStackFrameAndLeaveRegion(EvalStackFrame frame)
        {
            PopStackFrame(frame);
            LeaveRegionIfAny(frame);
        }

        private void LeaveRegionIfAny(EvalStackFrame frame)
        {
            RegionBuilder toLeave = frame.RegionBuilderOpt;
            if (toLeave != null)
            {
                while (_currentRegion != toLeave)
                {
                    Debug.Assert(_currentRegion.IsStackSpillRegion);
                    LeaveRegion();
                }

                LeaveRegion();
            }
        }

        private T PopStackFrame<T>(EvalStackFrame frame, T value)
        {
            PopStackFrame(frame);
            return value;
        }

        private void LeaveRegionsUpTo(RegionBuilder resultCaptureRegion)
        {
            while (_currentRegion != resultCaptureRegion)
            {
                LeaveRegion();
            }
        }

        private int GetNextCaptureId(RegionBuilder owner)
        {
            Debug.Assert(owner != null);
            int captureId = _captureIdDispenser.GetNextId();
            owner.AddCaptureId(captureId);
            return captureId;
        }

        private void SpillEvalStack()
        {
            Debug.Assert(_startSpillingAt <= _evalStack.Count);
#if DEBUG
            VerifySpilledStackFrames();
#endif
            int currentFrameIndex = -1;

            for (int i = _startSpillingAt - 1; i >= 0; i--)
            {
                (EvalStackFrame frameOpt, _) = _evalStack[i];
                if (frameOpt != null)
                {
                    currentFrameIndex = i;
                    Debug.Assert(frameOpt.RegionBuilderOpt != null);
                    break;
                }
            }

            for (int i = _startSpillingAt; i < _evalStack.Count; i++)
            {
                (EvalStackFrame frameOpt, IOperation operationOpt) = _evalStack[i];
                Debug.Assert((frameOpt == null) != (operationOpt == null));

                if (frameOpt != null)
                {
                    currentFrameIndex = i;
                    Debug.Assert(frameOpt.RegionBuilderOpt == null);
                    frameOpt.RegionBuilderOpt = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, isStackSpillRegion: true);
                    EnterRegion(frameOpt.RegionBuilderOpt, spillingStack: true);
                    continue;
                }

                // Declarations cannot have control flow, so we don't need to spill them.
                if (operationOpt.Kind != OperationKind.FlowCaptureReference
                    && operationOpt.Kind != OperationKind.DeclarationExpression
                    && operationOpt.Kind != OperationKind.Discard
                    && operationOpt.Kind != OperationKind.OmittedArgument)
                {
                    // Here we need to decide what region should own the new capture. Due to the spilling operations occurred before,
                    // we currently might be in a region that is not associated with the stack frame we are in, but it is one of its
                    // directly or indirectly nested regions. The operation that we are about to spill is likely to remove references
                    // to some captures from the stack. That means that, after the spilling, we should be able to leave the spill
                    // regions that no longer own captures referenced on the stack. The new capture that we create, should belong to
                    // the region that will become current after that. Here we are trying to compute what will be that region.
                    // Obviously, we shouldn’t be leaving the region associated with the frame.
                    RegionBuilder currentSpillRegion = _evalStack[currentFrameIndex].frameOpt.RegionBuilderOpt;
                    Debug.Assert(currentSpillRegion != null);

                    if (_currentRegion != currentSpillRegion)
                    {
                        var idsStillOnTheStack = PooledHashSet<CaptureId>.GetInstance();

                        for (int j = currentFrameIndex + 1; j < _evalStack.Count; j++)
                        {
                            IOperation operation = _evalStack[j].operationOpt;
                            if (operation != null)
                            {
                                if (j < i)
                                {
                                    if (operation is IFlowCaptureReferenceOperation reference)
                                    {
                                        idsStillOnTheStack.Add(reference.Id);
                                    }
                                }
                                else if (j > i)
                                {
                                    foreach (IFlowCaptureReferenceOperation reference in operation.DescendantsAndSelf().OfType<IFlowCaptureReferenceOperation>())
                                    {
                                        idsStillOnTheStack.Add(reference.Id);
                                    }
                                }
                            }
                        }

                        RegionBuilder candidate = _currentRegion;
                        do
                        {
                            Debug.Assert(candidate.IsStackSpillRegion);
                            if (candidate.HasCaptureIds && candidate.CaptureIds.Any((id, set) => set.Contains(id), idsStillOnTheStack))
                            {
                                currentSpillRegion = candidate;
                                break;
                            }

                            candidate = candidate.Enclosing;
                        }
                        while (candidate != currentSpillRegion);

                        idsStillOnTheStack.Free();
                    }

                    int captureId = GetNextCaptureId(currentSpillRegion);

                    AddStatement(new FlowCaptureOperation(captureId, operationOpt.Syntax, operationOpt)
#if DEBUG
                                 , spillingTheStack: true
#endif
                                );

                    _evalStack[i] = (frameOpt: null, operationOpt: GetCaptureReference(captureId, operationOpt));

                    while (_currentRegion != currentSpillRegion)
                    {
                        Debug.Assert(_currentRegion.IsStackSpillRegion);
                        LeaveRegion();
                    }
                }
            }

            _startSpillingAt = _evalStack.Count;
        }

#if DEBUG
        private void VerifySpilledStackFrames()
        {
            for (int i = 0; i < _startSpillingAt; i++)
            {
                (EvalStackFrame frameOpt, IOperation operationOpt) = _evalStack[i];
                if (frameOpt != null)
                {
                    Debug.Assert(operationOpt == null);
                    Debug.Assert(frameOpt.RegionBuilderOpt != null);
                }
                else
                {
                    Debug.Assert(operationOpt != null);
                }
            }
        }
#endif

        private void PushOperand(IOperation operation)
        {
            Debug.Assert(_evalStack.Count != 0);
            Debug.Assert(_evalStack.First().frameOpt != null);
            Debug.Assert(_evalStack.First().operationOpt == null);
            Debug.Assert(_startSpillingAt <= _evalStack.Count);
            Debug.Assert(operation != null);
            _evalStack.Push((frameOpt: null, operation));
        }

        private IOperation PopOperand()
        {
            int stackDepth = _evalStack.Count;
            Debug.Assert(_startSpillingAt <= stackDepth);

            if (_startSpillingAt == stackDepth)
            {
                _startSpillingAt--;
            }

            (EvalStackFrame frameOpt, IOperation operationOpt) = _evalStack.Pop();
            Debug.Assert(frameOpt == null);
            Debug.Assert(operationOpt != null);

            return operationOpt;
        }

        private IOperation PeekOperand()
        {
            Debug.Assert(_startSpillingAt <= _evalStack.Count);

            (EvalStackFrame frameOpt, IOperation operationOpt) = _evalStack.Peek();
            Debug.Assert(frameOpt == null);
            Debug.Assert(operationOpt != null);

            return operationOpt;
        }

        private void VisitAndPushArray<T>(ImmutableArray<T> array, Func<T, IOperation> unwrapper = null) where T : IOperation
        {
            Debug.Assert(unwrapper != null || typeof(T) == typeof(IOperation));
            foreach (var element in array)
            {
                PushOperand(Visit(unwrapper == null ? element : unwrapper(element)));
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
                    IOperation visitedElement = PopOperand();
                    builder.Add(wrapper != null ? wrapper(visitedElement, i, originalArray) : (T)visitedElement);
                }
                builder.ReverseContents();
                return builder.ToImmutableAndFree();
            }
        }

        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> originalArray, Func<T, IOperation> unwrapper = null, Func<IOperation, int, ImmutableArray<T>, T> wrapper = null) where T : IOperation
        {
#if DEBUG
            int stackSizeBefore = _evalStack.Count;
#endif

            VisitAndPushArray(originalArray, unwrapper);
            ImmutableArray<T> visitedArray = PopArray(originalArray, wrapper);

#if DEBUG
            Debug.Assert(stackSizeBefore == _evalStack.Count);
#endif

            return visitedArray;
        }

        private ImmutableArray<IArgumentOperation> VisitArguments(ImmutableArray<IArgumentOperation> arguments)
        {
            return VisitArray(arguments, UnwrapArgument, RewriteArgumentFromArray);
        }

        private static IOperation UnwrapArgument(IArgumentOperation argument)
        {
            return argument.Value;
        }

        private IArgumentOperation RewriteArgumentFromArray(IOperation visitedArgument, int index, ImmutableArray<IArgumentOperation> args)
        {
            Debug.Assert(index >= 0 && index < args.Length);
            var originalArgument = (BaseArgumentOperation)args[index];
            return new ArgumentOperation(visitedArgument, originalArgument.ArgumentKind, originalArgument.Parameter,
                                         originalArgument.InConversionConvertibleOpt, originalArgument.OutConversionConvertibleOpt,
                                         semanticModel: null, originalArgument.Syntax, IsImplicit(originalArgument));
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(Visit(operation.Target));
            IOperation value = Visit(operation.Value);
            return PopStackFrame(frame, new SimpleAssignmentOperation(operation.IsRef, PopOperand(), value, null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            var compoundAssignment = (BaseCompoundAssignmentOperation)operation;
            PushOperand(Visit(compoundAssignment.Target));
            IOperation value = Visit(compoundAssignment.Value);

            return PopStackFrame(frame, new CompoundAssignmentOperation(compoundAssignment.InConversionConvertible, compoundAssignment.OutConversionConvertible, operation.OperatorKind, operation.IsLifted,
                operation.IsChecked, operation.OperatorMethod, PopOperand(), value, semanticModel: null,
                syntax: operation.Syntax, type: operation.Type, constantValue: operation.ConstantValue, isImplicit: IsImplicit(operation)));
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(Visit(operation.ArrayReference));
            ImmutableArray<IOperation> visitedIndices = VisitArray(operation.Indices);
            IOperation visitedArrayReference = PopOperand();
            PopStackFrame(frame);
            return new ArrayElementReferenceOperation(visitedArrayReference, visitedIndices, semanticModel: null,
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

            EvalStackFrame frame = PushStackFrame();
            PushOperand(Visit(operation.LeftOperand));
            IOperation rightOperand = Visit(operation.RightOperand);
            return PopStackFrame(frame, new BinaryOperation(operation.OperatorKind, PopOperand(), rightOperand, operation.IsLifted, operation.IsChecked, operation.IsCompareText,
                                                                     operation.OperatorMethod, ((BaseBinaryOperation)operation).UnaryOperatorMethod, semanticModel: null,
                                                                     operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, int? captureIdForResult)
        {
            (IOperation visitedLeft, IOperation visitedRight) = VisitPreservingTupleOperations(operation.LeftOperand, operation.RightOperand);
            return new TupleBinaryOperation(operation.OperatorKind, visitedLeft, visitedRight,
                semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, int? captureIdForResult)
        {
            if (IsBooleanLogicalNot(operation))
            {
                return VisitConditionalExpression(operation, sense: true, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
            }

            return new UnaryOperation(operation.OperatorKind, Visit(operation.Operand), operation.IsLifted, operation.IsChecked, operation.OperatorMethod,
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

            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IOperation condition;

            bool isAndAlso = CalculateAndOrSense(binOp, true);

            // case BinaryOperatorKind.ConditionalOr:
            //        Dim result As Boolean?
            //
            //        If left.GetValueOrDefault Then
            //            GoTo resultIsLeft
            //        End If
            //
            //        If Not ((Not right).GetValueOrDefault) Then
            //            result = right
            //            GoTo done
            //        End If
            //
            //resultIsLeft:
            //        result = left
            //
            //done:
            //        Return result

            // case BinaryOperatorKind.ConditionalAnd:
            //        Dim result As Boolean?
            //
            //        If (Not left).GetValueOrDefault() Then
            //            GoTo resultIsLeft
            //        End If
            //
            //        If Not (right.GetValueOrDefault()) Then
            //            result = right
            //            GoTo done
            //        End If
            //
            //resultIsLeft:
            //        result = left
            //
            //done:
            //        Return result

            var resultCaptureRegion = _currentRegion;

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var checkRight = new BasicBlockBuilder(BasicBlockKind.Block);
            var resultIsLeft = new BasicBlockBuilder(BasicBlockKind.Block);

            IOperation capturedLeft = VisitAndCapture(left);

            condition = capturedLeft;

            if (isAndAlso)
            {
                condition = negateNullable(condition);
            }

            condition = CallNullableMember(condition, SpecialMember.System_Nullable_T_GetValueOrDefault);
            ConditionalBranch(condition, jumpIfTrue: true, resultIsLeft);
            UnconditionalBranch(checkRight);

            int resultId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

            AppendNewBlock(checkRight);

            EvalStackFrame frame = PushStackFrame();
            IOperation capturedRight = VisitAndCapture(right);

            condition = capturedRight;

            if (!isAndAlso)
            {
                condition = negateNullable(condition);
            }

            condition = CallNullableMember(condition, SpecialMember.System_Nullable_T_GetValueOrDefault);
            ConditionalBranch(condition, jumpIfTrue: true, resultIsLeft);
            _currentBasicBlock = null;

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, OperationCloner.CloneOperation(capturedRight)));
            UnconditionalBranch(done);

            PopStackFrameAndLeaveRegion(frame);

            AppendNewBlock(resultIsLeft);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, OperationCloner.CloneOperation(capturedLeft)));

            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);

            IOperation negateNullable(IOperation operand)
            {
                return new UnaryOperation(UnaryOperatorKind.Not, operand, isLifted: true, isChecked: false, operatorMethod: null,
                                                   semanticModel: null, operand.Syntax, operand.Type, constantValue: default, isImplicit: true);

            }
        }

        private IOperation VisitObjectBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IOperation condition;

            bool isAndAlso = CalculateAndOrSense(binOp, true);

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var checkRight = new BasicBlockBuilder(BasicBlockKind.Block);

            EvalStackFrame frame = PushStackFrame();

            condition = CreateConversion(Visit(left), booleanType);

            ConditionalBranch(condition, jumpIfTrue: isAndAlso, checkRight);
            _currentBasicBlock = null;

            PopStackFrameAndLeaveRegion(frame);

            var resultCaptureRegion = _currentRegion;

            int resultId = GetNextCaptureId(resultCaptureRegion);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, new LiteralOperation(semanticModel: null, left.Syntax, booleanType, constantValue: !isAndAlso, isImplicit: true)));
            UnconditionalBranch(done);

            AppendNewBlock(checkRight);

            frame = PushStackFrame();

            condition = CreateConversion(Visit(right), booleanType);

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, condition));

            PopStackFrame(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            condition = new FlowCaptureReferenceOperation(resultId, binOp.Syntax, booleanType, constantValue: default);
            return new ConversionOperation(condition, _compilation.ClassifyConvertibleConversion(condition, binOp.Type, out _), isTryCast: false, isChecked: false,
                                           semanticModel: null, binOp.Syntax, binOp.Type, binOp.ConstantValue, isImplicit: true);
        }

        private IOperation CreateConversion(IOperation operand, ITypeSymbol type)
        {
            return new ConversionOperation(operand, _compilation.ClassifyConvertibleConversion(operand, type, out Optional<object> constantValue), isTryCast: false, isChecked: false,
                                           semanticModel: null, operand.Syntax, type, constantValue, isImplicit: true);
        }

        private IOperation VisitDynamicBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            var resultCaptureRegion = _currentRegion;

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol unaryOperatorMethod = ((BaseBinaryOperation)binOp).UnaryOperatorMethod;
            bool isAndAlso = CalculateAndOrSense(binOp, true);
            bool jumpIfTrue;
            IOperation condition;

            // Dynamic logical && and || operators are lowered as follows:
            //   left && right  ->  IsFalse(left) ? left : And(left, right)
            //   left || right  ->  IsTrue(left) ? left : Or(left, right)

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var doBitWise = new BasicBlockBuilder(BasicBlockKind.Block);

            IOperation capturedLeft = VisitAndCapture(left);

            condition = capturedLeft;

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
                    condition = new UnaryOperation(isAndAlso ? UnaryOperatorKind.False : UnaryOperatorKind.True,
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
                condition = CreateConversion(condition, booleanType);
                jumpIfTrue = isAndAlso;
            }

            ConditionalBranch(condition, jumpIfTrue, doBitWise);
            _currentBasicBlock = null;

            int resultId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);
            IOperation resultFromLeft = OperationCloner.CloneOperation(capturedLeft);

            if (!ITypeSymbolHelpers.IsDynamicType(left.Type))
            {
                resultFromLeft = CreateConversion(resultFromLeft, binOp.Type);
            }

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, resultFromLeft));
            UnconditionalBranch(done);

            AppendNewBlock(doBitWise);

            EvalStackFrame frame = PushStackFrame();
            PushOperand(OperationCloner.CloneOperation(capturedLeft));
            IOperation visitedRight = Visit(right);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax,
                                         new BinaryOperation(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                                      PopOperand(),
                                                                      visitedRight,
                                                                      isLifted: false,
                                                                      binOp.IsChecked,
                                                                      binOp.IsCompareText,
                                                                      binOp.OperatorMethod,
                                                                      unaryOperatorMethod: null,
                                                                      semanticModel: null,
                                                                      binOp.Syntax,
                                                                      binOp.Type,
                                                                      binOp.ConstantValue, IsImplicit(binOp))));

            PopStackFrameAndLeaveRegion(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);
        }

        private IOperation VisitUserDefinedBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();

            var resultCaptureRegion = _currentRegion;

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            bool isLifted = binOp.IsLifted;
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol unaryOperatorMethod = ((BaseBinaryOperation)binOp).UnaryOperatorMethod;
            bool isAndAlso = CalculateAndOrSense(binOp, true);
            IOperation condition;

            var done = new BasicBlockBuilder(BasicBlockKind.Block);
            var doBitWise = new BasicBlockBuilder(BasicBlockKind.Block);

            IOperation capturedLeft = VisitAndCapture(left);

            condition = capturedLeft;

            if (ITypeSymbolHelpers.IsNullableType(left.Type))
            {
                if (unaryOperatorMethod == null ? isLifted : !ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type))
                {
                    condition = MakeIsNullOperation(condition, booleanType);
                    ConditionalBranch(condition, jumpIfTrue: true, doBitWise);
                    _currentBasicBlock = null;

                    Debug.Assert(unaryOperatorMethod == null || !ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type));
                    condition = CallNullableMember(OperationCloner.CloneOperation(capturedLeft), SpecialMember.System_Nullable_T_GetValueOrDefault);
                }
            }
            else if (unaryOperatorMethod != null && ITypeSymbolHelpers.IsNullableType(unaryOperatorMethod.Parameters[0].Type))
            {
                condition = MakeInvalidOperation(unaryOperatorMethod.Parameters[0].Type, condition);
            }

            if (unaryOperatorMethod != null && ITypeSymbolHelpers.IsBooleanType(unaryOperatorMethod.ReturnType))
            {
                condition = new UnaryOperation(isAndAlso ? UnaryOperatorKind.False : UnaryOperatorKind.True,
                                                        condition, isLifted: false, isChecked: false, operatorMethod: unaryOperatorMethod,
                                                        semanticModel: null, condition.Syntax, unaryOperatorMethod.ReturnType, constantValue: default, isImplicit: true);
            }
            else
            {
                condition = MakeInvalidOperation(booleanType, condition);
            }

            ConditionalBranch(condition, jumpIfTrue: false, doBitWise);
            _currentBasicBlock = null;

            int resultId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, OperationCloner.CloneOperation(capturedLeft)));
            UnconditionalBranch(done);

            AppendNewBlock(doBitWise);

            EvalStackFrame frame = PushStackFrame();
            PushOperand(OperationCloner.CloneOperation(capturedLeft));
            IOperation visitedRight = Visit(right);

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax,
                                         new BinaryOperation(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                                      PopOperand(),
                                                                      visitedRight,
                                                                      isLifted,
                                                                      binOp.IsChecked,
                                                                      binOp.IsCompareText,
                                                                      binOp.OperatorMethod,
                                                                      unaryOperatorMethod: null,
                                                                      semanticModel: null,
                                                                      binOp.Syntax,
                                                                      binOp.Type,
                                                                      binOp.ConstantValue, IsImplicit(binOp))));

            PopStackFrameAndLeaveRegion(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

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

            ref BasicBlockBuilder lazyFallThrough = ref stopValue ? ref fallToTrueOpt : ref fallToFalseOpt;
            bool newFallThroughBlock = (lazyFallThrough == null);

            VisitConditionalBranch(condition.LeftOperand, ref lazyFallThrough, stopSense);

            var resultCaptureRegion = _currentRegion;
            int captureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

            IOperation resultFromRight = VisitConditionalExpression(condition.RightOperand, sense, captureId, fallToTrueOpt, fallToFalseOpt);

            CaptureResultIfNotAlready(condition.RightOperand.Syntax, captureId, resultFromRight);

            LeaveRegionsUpTo(resultCaptureRegion);

            if (newFallThroughBlock)
            {
                var labEnd = new BasicBlockBuilder(BasicBlockKind.Block);
                UnconditionalBranch(labEnd);

                AppendNewBlock(lazyFallThrough);

                var constantValue = new Optional<object>(stopValue);
                SyntaxNode leftSyntax = (lazyFallThrough.GetSingletonPredecessorOrDefault() != null ? condition.LeftOperand : condition).Syntax;
                AddStatement(new FlowCaptureOperation(captureId, leftSyntax, new LiteralOperation(semanticModel: null, leftSyntax, condition.Type, constantValue, isImplicit: true)));

                AppendNewBlock(labEnd);
            }

            return GetCaptureReference(captureId, condition);
        }

        private IOperation VisitConditionalExpression(IOperation condition, bool sense, int? captureIdForResult, BasicBlockBuilder fallToTrueOpt, BasicBlockBuilder fallToFalseOpt)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(condition.Type));

            IUnaryOperation lastUnary = null;

            do
            {
                switch (condition)
                {
                    case IParenthesizedOperation parenthesized:
                        condition = parenthesized.Operand;
                        continue;
                    case IUnaryOperation unary when IsBooleanLogicalNot(unary):
                        lastUnary = unary;
                        condition = unary.Operand;
                        sense = !sense;
                        continue;
                }

                break;
            } while (true);

            if (condition.Kind == OperationKind.Binary)
            {
                var binOp = (IBinaryOperation)condition;
                if (IsBooleanConditionalOperator(binOp))
                {
                    return VisitBinaryConditionalOperator(binOp, sense, captureIdForResult, fallToTrueOpt, fallToFalseOpt);
                }
            }

            condition = Visit(condition);
            if (!sense)
            {
                return lastUnary != null
                    ? new UnaryOperation(lastUnary.OperatorKind, condition, lastUnary.IsLifted, lastUnary.IsChecked,
                                                  lastUnary.OperatorMethod, semanticModel: null, lastUnary.Syntax,
                                                  lastUnary.Type, lastUnary.ConstantValue, IsImplicit(lastUnary))
                    : new UnaryOperation(UnaryOperatorKind.Not, condition, isLifted: false, isChecked: false,
                                                  operatorMethod: null, semanticModel: null, condition.Syntax,
                                                  condition.Type, constantValue: default, isImplicit: true);
            }

            return condition;
        }

        private static bool IsBooleanConditionalOperator(IBinaryOperation binOp)
        {
            return IsConditional(binOp) &&
                   binOp.OperatorMethod == null &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.Type) &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.LeftOperand.Type) &&
                   ITypeSymbolHelpers.IsBooleanType(binOp.RightOperand.Type);
        }

        private void VisitConditionalBranch(IOperation condition, ref BasicBlockBuilder dest, bool jumpIfTrue)
        {
            SpillEvalStack();
#if DEBUG
            RegionBuilder current = _currentRegion;
#endif
            VisitConditionalBranchCore(condition, ref dest, jumpIfTrue);
#if DEBUG
            Debug.Assert(current == _currentRegion);
#endif
        }

        /// <summary>
        /// This function does not change the current region. The stack should be spilled before calling it.
        /// </summary>
        private void VisitConditionalBranchCore(IOperation condition, ref BasicBlockBuilder dest, bool jumpIfTrue)
        {
oneMoreTime:
            Debug.Assert(_startSpillingAt == _evalStack.Count);

            while (condition.Kind == OperationKind.Parenthesized)
            {
                condition = ((IParenthesizedOperation)condition).Operand;
            }

            switch (condition.Kind)
            {
                case OperationKind.Binary:
                    var binOp = (IBinaryOperation)condition;

                    if (IsBooleanConditionalOperator(binOp))
                    {
                        if (CalculateAndOrSense(binOp, jumpIfTrue))
                        {
                            // gotoif(LeftOperand != sense) fallThrough
                            // gotoif(RightOperand == sense) dest
                            // fallThrough:

                            BasicBlockBuilder fallThrough = null;

                            VisitConditionalBranchCore(binOp.LeftOperand, ref fallThrough, !jumpIfTrue);
                            VisitConditionalBranchCore(binOp.RightOperand, ref dest, jumpIfTrue);
                            AppendNewBlock(fallThrough);
                            return;
                        }
                        else
                        {
                            // gotoif(LeftOperand == sense) dest
                            // gotoif(RightOperand == sense) dest

                            VisitConditionalBranchCore(binOp.LeftOperand, ref dest, jumpIfTrue);
                            condition = binOp.RightOperand;
                            goto oneMoreTime;
                        }
                    }

                    // none of above.
                    // then it is regular binary expression - Or, And, Xor ...
                    goto default;

                case OperationKind.Unary:
                    var unOp = (IUnaryOperation)condition;

                    if (IsBooleanLogicalNot(unOp))
                    {
                        jumpIfTrue = !jumpIfTrue;
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
                            VisitConditionalBranchCore(conditional.Condition, ref whenFalse, jumpIfTrue: false);
                            VisitConditionalBranchCore(conditional.WhenTrue, ref dest, jumpIfTrue);

                            var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                            UnconditionalBranch(afterIf);

                            AppendNewBlock(whenFalse);
                            VisitConditionalBranchCore(conditional.WhenFalse, ref dest, jumpIfTrue);
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

                            EvalStackFrame frame = PushStackFrame();

                            IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(coalesce, whenNull);

                            dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                            ConditionalBranch(convertedTestExpression, jumpIfTrue, dest);
                            _currentBasicBlock = null;

                            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);
                            UnconditionalBranch(afterCoalesce);

                            PopStackFrameAndLeaveRegion(frame);

                            AppendNewBlock(whenNull);
                            VisitConditionalBranchCore(coalesce.WhenNull, ref dest, jumpIfTrue);

                            AppendNewBlock(afterCoalesce);

                            return;
                        }
                    }
                    goto default;

                case OperationKind.Conversion:
                    var conversion = (IConversionOperation)condition;

                    if (conversion.Operand.Kind == OperationKind.Throw)
                    {
                        IOperation rewrittenThrow = base.Visit(conversion.Operand, null);
                        Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                        Debug.Assert(rewrittenThrow.Children.IsEmpty());
                        dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                        return;
                    }
                    goto default;

                default:
                    {
                        EvalStackFrame frame = PushStackFrame();

                        condition = Visit(condition);
                        dest = dest ?? new BasicBlockBuilder(BasicBlockKind.Block);
                        ConditionalBranch(condition, jumpIfTrue, dest);
                        _currentBasicBlock = null;

                        PopStackFrameAndLeaveRegion(frame);
                        return;
                    }
            }
        }

        private void ConditionalBranch(IOperation condition, bool jumpIfTrue, BasicBlockBuilder destination)
        {
            BasicBlockBuilder previous = CurrentBasicBlock;
            BasicBlockBuilder.Branch branch = RegularBranch(destination);
            Debug.Assert(previous.BranchValue == null);
            Debug.Assert(!previous.HasCondition);
            Debug.Assert(condition != null);
            Operation.SetParentOperation(condition, null);
            branch.Destination.AddPredecessor(previous);
            previous.BranchValue = condition;
            previous.ConditionKind = jumpIfTrue ? ControlFlowConditionKind.WhenTrue : ControlFlowConditionKind.WhenFalse;
            previous.Conditional = branch;
        }

        /// <summary>
        /// Returns converted test expression.
        /// Caller is responsible for spilling the stack and pushing a stack frame before calling this helper.
        /// </summary>
        private IOperation NullCheckAndConvertCoalesceValue(ICoalesceOperation operation, BasicBlockBuilder whenNull)
        {
            Debug.Assert(_evalStack.Last().frameOpt != null);
            Debug.Assert(_startSpillingAt >= _evalStack.Count - 1);

            IOperation operationValue = operation.Value;
            SyntaxNode valueSyntax = operationValue.Syntax;
            ITypeSymbol valueTypeOpt = operationValue.Type;

            PushOperand(Visit(operationValue));
            SpillEvalStack();
            IOperation testExpression = PopOperand();

            ConditionalBranch(MakeIsNullOperation(testExpression),
                jumpIfTrue: true,
                whenNull);
            _currentBasicBlock = null;

            CommonConversion testConversion = operation.ValueConversion;
            IOperation capturedValue = OperationCloner.CloneOperation(testExpression);
            IOperation convertedTestExpression = null;

            if (testConversion.Exists)
            {
                IOperation possiblyUnwrappedValue;

                if (ITypeSymbolHelpers.IsNullableType(valueTypeOpt) &&
                    (!testConversion.IsIdentity || !ITypeSymbolHelpers.IsNullableType(operation.Type)))
                {
                    possiblyUnwrappedValue = TryCallNullableMember(capturedValue, SpecialMember.System_Nullable_T_GetValueOrDefault);
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
                        convertedTestExpression = new ConversionOperation(possiblyUnwrappedValue, ((BaseCoalesceOperation)operation).ValueConversionConvertible,
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
            SpillEvalStack();

            var conversion = operation.WhenNull as IConversionOperation;
            bool alternativeThrows = conversion?.Operand.Kind == OperationKind.Throw;

            RegionBuilder resultCaptureRegion = _currentRegion;

            EvalStackFrame frame = PushStackFrame();

            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
            IOperation convertedTestExpression = NullCheckAndConvertCoalesceValue(operation, whenNull);

            IOperation result;
            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);

            if (alternativeThrows)
            {
                // This is a special case with "throw" as an alternative. We don't need to create an additional
                // capture for the result because there won't be any result from the alternative branch.
                result = convertedTestExpression;

                UnconditionalBranch(afterCoalesce);
                PopStackFrame(frame);

                AppendNewBlock(whenNull);

                IOperation rewrittenThrow = base.Visit(conversion.Operand, null);
                Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                Debug.Assert(rewrittenThrow.Children.IsEmpty());
            }
            else
            {
                int resultCaptureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

                AddStatement(new FlowCaptureOperation(resultCaptureId, operation.Value.Syntax, convertedTestExpression));
                result = GetCaptureReference(resultCaptureId, operation);

                UnconditionalBranch(afterCoalesce);

                PopStackFrameAndLeaveRegion(frame);

                AppendNewBlock(whenNull);

                VisitAndCapture(operation.WhenNull, resultCaptureId);

                LeaveRegionsUpTo(resultCaptureRegion);
            }

            AppendNewBlock(afterCoalesce);

            return result;
        }

        public override IOperation VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            // If we're in a statement context, we elide the capture of the result of the assignment, as it will
            // just be wrapped in an expression statement that isn't used anywhere and isn't observed by anything.
            bool isStatement = _currentStatement == operation || operation?.Parent.Kind == OperationKind.ExpressionStatement;
            Debug.Assert(captureIdForResult == null || !isStatement);

            RegionBuilder resultCaptureRegion = _currentRegion;

            EvalStackFrame frame = PushStackFrame();

            PushOperand(Visit(operation.Target));
            SpillEvalStack();
            IOperation locationCapture = PopOperand();

            // Capture the value, as it will only be evaluated once. The location will be used separately later for
            // the null case
            EvalStackFrame valueFrame = PushStackFrame();
            SpillEvalStack();
            int valueCaptureId = GetNextCaptureId(valueFrame.RegionBuilderOpt);
            AddStatement(new FlowCaptureOperation(valueCaptureId, locationCapture.Syntax, locationCapture));
            IOperation valueCapture = GetCaptureReference(valueCaptureId, locationCapture);

            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);

            int resultCaptureId = isStatement ? -1 : captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

            if (operation.Target?.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                ((INamedTypeSymbol)operation.Target.Type).TypeArguments[0].Equals(operation.Type))
            {
                nullableValueTypeReturn();
            }
            else
            {
                standardReturn();
            }

            PopStackFrame(frame);

            LeaveRegionsUpTo(resultCaptureRegion);
            AppendNewBlock(afterCoalesce);

            return isStatement ? null : GetCaptureReference(resultCaptureId, operation);

            void nullableValueTypeReturn()
            {
                // We'll transform this into one of two possibilities, depending on whether we're using
                // this as an expression or a statement.
                //
                // Expression Form:
                // intermediate1 = valueCapture.GetValueOrDefault();
                // branch if false to whenNull: valueCapture.HasValue()
                // result = intermediate
                // branch to after
                //
                // whenNull:
                // intermediate2 = rightValue
                // result = intermediate2
                // locationCapture = Convert(intermediate2)
                //
                // after:
                // result
                //
                // Statement Form
                // branch if false to whenNull: valueCapture.HasValue()
                // branch to after
                //
                // whenNull:
                // locationCapture = Convert(rightValue)
                //
                // after:

                int intermediateResult = -1;
                EvalStackFrame intermediateFrame = null;

                if (!isStatement)
                {
                    intermediateFrame = PushStackFrame();
                    SpillEvalStack();
                    intermediateResult = GetNextCaptureId(intermediateFrame.RegionBuilderOpt);
                    AddStatement(
                        new FlowCaptureOperation(intermediateResult,
                                                 operation.Target.Syntax,
                                                 CallNullableMember(valueCapture, SpecialMember.System_Nullable_T_GetValueOrDefault)));
                }

                ConditionalBranch(
                    CallNullableMember(OperationCloner.CloneOperation(valueCapture), SpecialMember.System_Nullable_T_get_HasValue),
                    jumpIfTrue: false,
                    whenNull);

                if (!isStatement)
                {
                    _currentBasicBlock = null;
                    AddStatement(
                        new FlowCaptureOperation(resultCaptureId,
                                                 operation.Syntax,
                                                 GetCaptureReference(intermediateResult, operation.Target)));
                    PopStackFrame(intermediateFrame);
                }

                PopStackFrame(valueFrame);

                UnconditionalBranch(afterCoalesce);

                AppendNewBlock(whenNull);

                EvalStackFrame whenNullFrame = PushStackFrame();
                SpillEvalStack();

                IOperation whenNullValue = Visit(operation.Value);
                if (!isStatement)
                {
                    int intermediateValueCaptureId = GetNextCaptureId(whenNullFrame.RegionBuilderOpt);
                    AddStatement(new FlowCaptureOperation(intermediateValueCaptureId, whenNullValue.Syntax, whenNullValue));
                    whenNullValue = GetCaptureReference(intermediateValueCaptureId, whenNullValue);
                    AddStatement(
                        new FlowCaptureOperation(
                            resultCaptureId,
                            operation.Syntax,
                            GetCaptureReference(intermediateValueCaptureId, whenNullValue)));
                }

                AddStatement(
                    new SimpleAssignmentOperation(
                        isRef: false,
                        target: OperationCloner.CloneOperation(locationCapture),
                        value: CreateConversion(whenNullValue, operation.Target.Type),
                        semanticModel: null,
                        syntax: operation.Syntax,
                        type: operation.Target.Type,
                        constantValue: operation.ConstantValue,
                        isImplicit: true));

                PopStackFrameAndLeaveRegion(whenNullFrame);
            }

            void standardReturn()
            {
                ConditionalBranch(MakeIsNullOperation(valueCapture),
                    jumpIfTrue: true,
                    whenNull);

                if (!isStatement)
                {
                    _currentBasicBlock = null;

                    AddStatement(new FlowCaptureOperation(resultCaptureId, operation.Syntax, OperationCloner.CloneOperation(valueCapture)));
                }

                PopStackFrameAndLeaveRegion(valueFrame);

                UnconditionalBranch(afterCoalesce);

                AppendNewBlock(whenNull);

                // The return of Visit(operation.WhenNull) can be a flow capture that wasn't used in the non-null branch. We want to create a
                // region around it to ensure that the scope of the flow capture is as narrow as possible. If there was no flow capture, region
                // packing will take care of removing the empty region.
                EvalStackFrame whenNullFrame = PushStackFrame();

                IOperation whenNullValue = Visit(operation.Value);
                IOperation whenNullAssignment = new SimpleAssignmentOperation(isRef: false, OperationCloner.CloneOperation(locationCapture), whenNullValue, semanticModel: null,
                    operation.Syntax, operation.Type, constantValue: operation.ConstantValue, isImplicit: true);

                if (isStatement)
                {
                    AddStatement(whenNullAssignment);
                }
                else
                {
                    AddStatement(new FlowCaptureOperation(resultCaptureId, operation.Syntax, whenNullAssignment));
                }

                PopStackFrameAndLeaveRegion(whenNullFrame);
            }
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

        private IsNullOperation MakeIsNullOperation(IOperation operand)
        {
            return MakeIsNullOperation(operand, _compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private static IsNullOperation MakeIsNullOperation(IOperation operand, ITypeSymbol booleanType)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(booleanType));
            Optional<object> constantValue = operand.ConstantValue;
            return new IsNullOperation(operand.Syntax, operand,
                                       booleanType,
                                       constantValue.HasValue ? new Optional<object>(constantValue.Value == null) : default);
        }

        private IOperation TryCallNullableMember(IOperation value, SpecialMember nullableMember)
        {
            Debug.Assert(nullableMember == SpecialMember.System_Nullable_T_GetValueOrDefault ||
                         nullableMember == SpecialMember.System_Nullable_T_get_HasValue ||
                         nullableMember == SpecialMember.System_Nullable_T_get_Value ||
                         nullableMember == SpecialMember.System_Nullable_T__op_Explicit_ToT ||
                         nullableMember == SpecialMember.System_Nullable_T__op_Implicit_FromT);
            ITypeSymbol valueType = value.Type;

            Debug.Assert(ITypeSymbolHelpers.IsNullableType(valueType));

            var method = (IMethodSymbol)_compilation.CommonGetSpecialTypeMember(nullableMember)?.GetISymbol();

            if (method != null)
            {
                foreach (ISymbol candidate in valueType.GetMembers(method.Name))
                {
                    if (candidate.OriginalDefinition.Equals(method))
                    {
                        method = (IMethodSymbol)candidate;
                        return new InvocationOperation(method, value, isVirtual: false,
                                                        ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, value.Syntax,
                                                        method.ReturnType, constantValue: default, isImplicit: true);
                    }
                }
            }

            return null;
        }

        private IOperation CallNullableMember(IOperation value, SpecialMember nullableMember)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(value.Type));
            return TryCallNullableMember(value, nullableMember) ??
                   MakeInvalidOperation(ITypeSymbolHelpers.GetNullableUnderlyingType(value.Type), value);
        }

        public override IOperation VisitConditionalAccess(IConditionalAccessOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            RegionBuilder resultCaptureRegion = _currentRegion;

            // Avoid creation of default values and FlowCapture for conditional access on a statement level.
            bool isOnStatementLevel = _currentStatement == operation || (_currentStatement == operation.Parent && _currentStatement?.Kind == OperationKind.ExpressionStatement);
            var frames = ArrayBuilder<EvalStackFrame>.GetInstance();

            if (!isOnStatementLevel)
            {
                frames.Push(PushStackFrame());
            }

            IConditionalAccessOperation currentConditionalAccess = operation;

            while (true)
            {
                frames.Push(PushStackFrame());

                if (currentConditionalAccess.WhenNotNull.Kind != OperationKind.ConditionalAccess)
                {
                    break;
                }

                currentConditionalAccess = (IConditionalAccessOperation)currentConditionalAccess.WhenNotNull;
            }

            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);

            currentConditionalAccess = operation;
            IOperation testExpression;

            while (true)
            {
                testExpression = currentConditionalAccess.Operation;
                SyntaxNode testExpressionSyntax = testExpression.Syntax;
                ITypeSymbol testExpressionType = testExpression.Type;

                PushOperand(Visit(testExpression));
                SpillEvalStack();
                IOperation spilledTestExpression = PopOperand();
                PopStackFrame(frames.Pop());

                ConditionalBranch(MakeIsNullOperation(spilledTestExpression),
                    jumpIfTrue: true,
                    whenNull);
                _currentBasicBlock = null;

                IOperation receiver = OperationCloner.CloneOperation(spilledTestExpression);

                if (ITypeSymbolHelpers.IsNullableType(testExpressionType))
                {
                    receiver = CallNullableMember(receiver, SpecialMember.System_Nullable_T_GetValueOrDefault);
                }

                // https://github.com/dotnet/roslyn/issues/27564: It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
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

            if (isOnStatementLevel)
            {
                Debug.Assert(captureIdForResult == null);
                Debug.Assert(frames.Count == 0);
                frames.Free();

                IOperation result = Visit(currentConditionalAccess.WhenNotNull);
                Debug.Assert(_currentConditionalAccessInstance == null);
                _currentConditionalAccessInstance = null;

                if (_currentStatement != operation)
                {
                    var expressionStatement = (IExpressionStatementOperation)_currentStatement;
                    result = new ExpressionStatementOperation(result, semanticModel: null, expressionStatement.Syntax,
                                                     expressionStatement.Type, expressionStatement.ConstantValue,
                                                     IsImplicit(expressionStatement));
                }

                AddStatement(result);
                AppendNewBlock(whenNull);
                return null;
            }
            else
            {
                EvalStackFrame frame = frames.Pop();
                Debug.Assert(frames.Count == 0);
                frames.Free();

                int resultCaptureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

                if (ITypeSymbolHelpers.IsNullableType(operation.Type) && !ITypeSymbolHelpers.IsNullableType(currentConditionalAccess.WhenNotNull.Type))
                {
                    IOperation access = Visit(currentConditionalAccess.WhenNotNull);
                    AddStatement(new FlowCaptureOperation(resultCaptureId, currentConditionalAccess.WhenNotNull.Syntax,
                        MakeNullable(access, operation.Type)));
                }
                else
                {
                    CaptureResultIfNotAlready(currentConditionalAccess.WhenNotNull.Syntax, resultCaptureId,
                                              Visit(currentConditionalAccess.WhenNotNull, resultCaptureId));
                }

                PopStackFrame(frame);
                LeaveRegionsUpTo(resultCaptureRegion);

                // https://github.com/dotnet/roslyn/issues/27564: It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
                //                      a None operation is created and all children are dropped.
                //                      See Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests.ExpressionCompilerTests.ConditionalAccessExpressionType
                //                      The following assert is triggered because of that. Disabling it for now.
                //Debug.Assert(_currentConditionalAccessInstance == null);
                _currentConditionalAccessInstance = null;

                var afterAccess = new BasicBlockBuilder(BasicBlockKind.Block);
                UnconditionalBranch(afterAccess);

                AppendNewBlock(whenNull);

                SyntaxNode defaultValueSyntax = (operation.Operation == testExpression ? testExpression : operation).Syntax;

                AddStatement(new FlowCaptureOperation(resultCaptureId,
                                             defaultValueSyntax,
                                             new DefaultValueOperation(semanticModel: null, defaultValueSyntax, operation.Type,
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
            StartVisitingStatement(operation);

            IOperation underlying = Visit(operation.Operation);

            if (underlying == null)
            {
                Debug.Assert(operation.Operation.Kind == OperationKind.ConditionalAccess || operation.Operation.Kind == OperationKind.CoalesceAssignment);
                return FinishVisitingStatement(operation);
            }
            else if (operation.Operation.Kind == OperationKind.Throw)
            {
                return FinishVisitingStatement(operation);
            }

            return FinishVisitingStatement(operation, new ExpressionStatementOperation(underlying, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
        }

        public override IOperation VisitWhileLoop(IWhileLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
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

                VisitConditionalBranch(operation.Condition, ref @break, jumpIfTrue: operation.ConditionIsUntil);

                VisitStatement(operation.Body);
                UnconditionalBranch(@continue);
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
                    VisitConditionalBranch(operation.Condition, ref start, jumpIfTrue: !operation.ConditionIsUntil);
                }
                else
                {
                    UnconditionalBranch(start);
                }
            }

            Debug.Assert(_currentRegion == locals);
            LeaveRegion();

            AppendNewBlock(@break);
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitTry(ITryOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            var afterTryCatchFinally = GetLabeledOrNewBlock(operation.ExitLabel);

            if (operation.Catches.IsEmpty && operation.Finally == null)
            {
                // Malformed node without handlers
                // It looks like we can get here for VB only. Let's recover the same way C# does, i.e.
                // pretend that there is no Try. Just visit body.
                VisitStatement(operation.Body);
                AppendNewBlock(afterTryCatchFinally);
                return FinishVisitingStatement(operation);
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

            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
            VisitStatement(operation.Body);
            UnconditionalBranch(afterTryCatchFinally);

            if (haveCatches)
            {
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

                        VisitConditionalBranch(filter, ref catchBlock, jumpIfTrue: true);
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
                    UnconditionalBranch(afterTryCatchFinally);

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

            return FinishVisitingStatement(operation);
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
                    exceptionTarget = new LocalReferenceOperation(local,
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
                    AddStatement(new SimpleAssignmentOperation(
                        isRef: false,
                        target: exceptionTarget,
                        value: new CaughtExceptionOperation(syntax, exceptionType),
                        semanticModel: null,
                        syntax: syntax,
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
            StartVisitingStatement(operation);
            IOperation returnedValue = Visit(operation.ReturnedValue);

            switch (operation.Kind)
            {
                case OperationKind.YieldReturn:
                    AddStatement(new ReturnOperation(returnedValue, OperationKind.YieldReturn, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
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

            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitLabeled(ILabeledOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            VisitLabel(operation.Label);
            VisitStatement(operation.Operation);
            return FinishVisitingStatement(operation);
        }

        public void VisitLabel(ILabelSymbol operation)
        {
            BasicBlockBuilder labeled = GetLabeledOrNewBlock(operation);

            if (labeled.Ordinal != -1)
            {
                // Must be a duplicate label. Recover by simply allocating a new block.
                labeled = new BasicBlockBuilder(BasicBlockKind.Block);
            }

            AppendNewBlock(labeled);
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
            StartVisitingStatement(operation);
            UnconditionalBranch(GetLabeledOrNewBlock(operation.Target));
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitEmpty(IEmptyOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitThrow(IThrowOperation operation, int? captureIdForResult)
        {
            bool isStatement = (_currentStatement == operation);

            if (!isStatement)
            {
                SpillEvalStack();
            }

            EvalStackFrame frame = PushStackFrame();
            LinkThrowStatement(Visit(operation.Exception));
            PopStackFrameAndLeaveRegion(frame);
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block), linkToPrevious: false);

            if (isStatement)
            {
                return null;
            }
            else
            {
                return new NoneOperation(children: ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, constantValue: default, isImplicit: true, type: null);
            }
        }

        private void LinkThrowStatement(IOperation exception)
        {
            BasicBlockBuilder current = CurrentBasicBlock;
            Debug.Assert(current.BranchValue == null);
            Debug.Assert(!current.HasCondition);
            Debug.Assert(current.FallThrough.Destination == null);
            Debug.Assert(current.FallThrough.Kind == ControlFlowBranchSemantics.None);
            current.BranchValue = Operation.SetParentOperation(exception, null);
            current.FallThrough.Kind = exception == null ? ControlFlowBranchSemantics.Rethrow : ControlFlowBranchSemantics.Throw;
        }

        public override IOperation VisitUsing(IUsingOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            HandleUsingOperationParts(operation.Resources, operation.Body, operation.Locals, operation.IsAsynchronous);
            return FinishVisitingStatement(operation);
        }

        private void HandleUsingOperationParts(IOperation resources, IOperation body, ImmutableArray<ILocalSymbol> locals, bool isAsynchronous)
        {
            var usingRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(usingRegion);

            ITypeSymbol iDisposable = isAsynchronous
                ? _compilation.CommonGetWellKnownType(WellKnownType.System_IAsyncDisposable)?.GetITypeSymbol()
                : _compilation.GetSpecialType(SpecialType.System_IDisposable);

            if (resources is IVariableDeclarationGroupOperation declarationGroup)
            {
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
                Debug.Assert(resources.Kind != OperationKind.VariableDeclaration);
                Debug.Assert(resources.Kind != OperationKind.VariableDeclarator);

                EvalStackFrame frame = PushStackFrame();
                IOperation resource = Visit(resources);

                if (shouldConvertToIDisposableBeforeTry(resource))
                {
                    resource = ConvertToIDisposable(resource, iDisposable);
                }

                PushOperand(resource);
                SpillEvalStack();
                resource = PopOperand();
                PopStackFrame(frame);

                processResource(resource, resourceQueueOpt: null);
                LeaveRegionIfAny(frame);
            }

            Debug.Assert(_currentRegion == usingRegion);
            LeaveRegion();

            void processQueue(ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)> resourceQueueOpt)
            {
                if (resourceQueueOpt == null || resourceQueueOpt.Count == 0)
                {
                    VisitStatement(body);
                }
                else
                {
                    (IVariableDeclarationOperation declaration, IVariableDeclaratorOperation declarator) = resourceQueueOpt.Pop();
                    HandleVariableDeclarator(declaration, declarator);
                    ILocalSymbol localSymbol = declarator.Symbol;
                    processResource(new LocalReferenceOperation(localSymbol, isDeclaration: false, semanticModel: null, declarator.Syntax, localSymbol.Type,
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

                RegionBuilder resourceRegion = null;

                if (shouldConvertToIDisposableBeforeTry(resource))
                {
                    resourceRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime);
                    EnterRegion(resourceRegion);
                    resource = ConvertToIDisposable(resource, iDisposable);
                    int captureId = GetNextCaptureId(resourceRegion);
                    AddStatement(new FlowCaptureOperation(captureId, resource.Syntax, resource));
                    resource = GetCaptureReference(captureId, resource);
                }

                var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);

                EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));

                processQueue(resourceQueueOpt);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                UnconditionalBranch(afterTryFinally);

                LeaveRegion();

                AddDisposingFinally(resource, knownToImplementIDisposable: true, iDisposable, isAsynchronous);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
                LeaveRegion();

                if (resourceRegion != null)
                {
                    Debug.Assert(_currentRegion == resourceRegion);
                    LeaveRegion();
                }

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }
        }

        private void AddDisposingFinally(IOperation resource, bool knownToImplementIDisposable, ITypeSymbol iDisposable, bool isAsynchronous)
        {
            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            var finallyRegion = new RegionBuilder(ControlFlowRegionKind.Finally);
            EnterRegion(finallyRegion);
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (!knownToImplementIDisposable)
            {
                Debug.Assert(!isNotNullableValueType(resource.Type));
                resource = ConvertToIDisposable(resource, iDisposable, isTryCast: true);
                int captureId = GetNextCaptureId(finallyRegion);
                AddStatement(new FlowCaptureOperation(captureId, resource.Syntax, resource));
                resource = GetCaptureReference(captureId, resource);
            }

            if (!knownToImplementIDisposable || !isNotNullableValueType(resource.Type))
            {
                IOperation condition = MakeIsNullOperation(OperationCloner.CloneOperation(resource));
                ConditionalBranch(condition, jumpIfTrue: true, endOfFinally);
                _currentBasicBlock = null;
            }

            if (!resource.Type.Equals(iDisposable))
            {
                resource = ConvertToIDisposable(resource, iDisposable);
            }

            AddStatement(tryDispose(resource) ??
                         MakeInvalidOperation(type: null, resource));

            AppendNewBlock(endOfFinally);

            Debug.Assert(_currentRegion == finallyRegion);
            LeaveRegion();
            return;

            IOperation tryDispose(IOperation value)
            {
                Debug.Assert(value.Type.Equals(iDisposable));

                var method = isAsynchronous
                    ? (IMethodSymbol)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync)?.GetISymbol()
                    : (IMethodSymbol)_compilation.CommonGetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose)?.GetISymbol();
                if (method != null)
                {
                    var invocation = new InvocationOperation(method, value, isVirtual: true,
                                                    ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, value.Syntax,
                                                    method.ReturnType, constantValue: default, isImplicit: true);

                    if (isAsynchronous)
                    {
                        return new AwaitOperation(invocation, semanticModel: null, value.Syntax, _compilation.GetSpecialType(SpecialType.System_Void), constantValue: default, isImplicit: true);
                    }

                    return invocation;
                }

                return null;
            }

            bool isNotNullableValueType(ITypeSymbol type)
            {
                return type?.IsValueType == true && !ITypeSymbolHelpers.IsNullableType(type);
            }
        }

        private IOperation ConvertToIDisposable(IOperation operand, ITypeSymbol iDisposable, bool isTryCast = false)
        {
            Debug.Assert(iDisposable.SpecialType == SpecialType.System_IDisposable ||
                iDisposable.Equals(_compilation.CommonGetWellKnownType(WellKnownType.System_IAsyncDisposable)?.GetITypeSymbol()));
            return new ConversionOperation(operand, _compilation.ClassifyConvertibleConversion(operand, iDisposable, out var constantValue), isTryCast, isChecked: false,
                                           semanticModel: null, operand.Syntax, iDisposable, constantValue, isImplicit: true);
        }

        public override IOperation VisitLock(ILockOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            ITypeSymbol objectType = _compilation.GetSpecialType(SpecialType.System_Object);

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
            var baseLockStatement = (BaseLockOperation)operation;

            var lockRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime,
                                               locals: baseLockStatement.LockTakenSymbol != null ?
                                                   ImmutableArray.Create(baseLockStatement.LockTakenSymbol) :
                                                   ImmutableArray<ILocalSymbol>.Empty);
            EnterRegion(lockRegion);

            EvalStackFrame frame = PushStackFrame();
            IOperation lockedValue = Visit(operation.LockedValue);

            if (!objectType.Equals(lockedValue.Type))
            {
                lockedValue = CreateConversion(lockedValue, objectType);
            }

            PushOperand(lockedValue);
            SpillEvalStack();
            lockedValue = PopOperand();
            PopStackFrame(frame);

            var enterMethod = (IMethodSymbol)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2)?.GetISymbol();
            bool legacyMode = (enterMethod == null);

            if (legacyMode)
            {
                Debug.Assert(baseLockStatement.LockTakenSymbol == null);
                enterMethod = (IMethodSymbol)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter)?.GetISymbol();

                // Monitor.Enter($lock);
                if (enterMethod == null)
                {
                    AddStatement(MakeInvalidOperation(type: null, lockedValue));
                }
                else
                {
                    AddStatement(new InvocationOperation(enterMethod, instance: null, isVirtual: false,
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
                lockTaken = new LocalReferenceOperation(baseLockStatement.LockTakenSymbol, isDeclaration: true, semanticModel: null, lockedValue.Syntax,
                                                         baseLockStatement.LockTakenSymbol.Type, constantValue: default, isImplicit: true);
                AddStatement(new InvocationOperation(enterMethod, instance: null, isVirtual: false,
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

            Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
            UnconditionalBranch(afterTryFinally);

            LeaveRegion();

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Finally));
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (!legacyMode)
            {
                // if ($lockTaken)
                IOperation condition = new LocalReferenceOperation(baseLockStatement.LockTakenSymbol, isDeclaration: false, semanticModel: null, lockedValue.Syntax,
                                                                    baseLockStatement.LockTakenSymbol.Type, constantValue: default, isImplicit: true);
                ConditionalBranch(condition, jumpIfTrue: false, endOfFinally);
                _currentBasicBlock = null;
            }

            // Monitor.Exit($lock);
            var exitMethod = (IMethodSymbol)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Exit)?.GetISymbol();
            lockedValue = OperationCloner.CloneOperation(lockedValue);

            if (exitMethod == null)
            {
                AddStatement(MakeInvalidOperation(type: null, lockedValue));
            }
            else
            {
                AddStatement(new InvocationOperation(exitMethod, instance: null, isVirtual: false,
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

            LeaveRegionsUpTo(lockRegion);
            LeaveRegion();

            AppendNewBlock(afterTryFinally, linkToPrevious: false);

            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitForEachLoop(IForEachLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            var enumeratorCaptureRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime);
            EnterRegion(enumeratorCaptureRegion);

            ForEachLoopOperationInfo info = ((BaseForEachLoopOperation)operation).Info;

            RegionBuilder regionForCollection = null;

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
                        regionForCollection = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: ImmutableArray.Create(local));
                        EnterRegion(regionForCollection);
                        break;
                    }
                }
            }

            IOperation enumerator = getEnumerator();

            if (regionForCollection != null)
            {
                Debug.Assert(regionForCollection == _currentRegion);
                LeaveRegion();
            }

            if (info?.NeedsDispose == true)
            {
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));
            }

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            var @break = GetLabeledOrNewBlock(operation.ExitLabel);

            AppendNewBlock(@continue);

            EvalStackFrame frame = PushStackFrame();
            ConditionalBranch(getCondition(enumerator), jumpIfTrue: false, @break);
            _currentBasicBlock = null;
            PopStackFrameAndLeaveRegion(frame);

            var localsRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals);
            EnterRegion(localsRegion);

            frame = PushStackFrame();
            AddStatement(getLoopControlVariableAssignment(applyConversion(info?.CurrentConversion, getCurrent(OperationCloner.CloneOperation(enumerator)), info?.ElementType)));
            PopStackFrameAndLeaveRegion(frame);

            VisitStatement(operation.Body);
            Debug.Assert(localsRegion == _currentRegion);
            UnconditionalBranch(@continue);

            LeaveRegion();

            AppendNewBlock(@break);

            if (info?.NeedsDispose == true)
            {
                var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);
                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.Try);
                UnconditionalBranch(afterTryFinally);

                LeaveRegion();

                bool isAsynchronous = info.IsAsynchronous;
                var iDisposable = isAsynchronous
                    ? _compilation.CommonGetWellKnownType(WellKnownType.System_IAsyncDisposable)?.GetITypeSymbol()
                    : _compilation.GetSpecialType(SpecialType.System_IDisposable);

                AddDisposingFinally(OperationCloner.CloneOperation(enumerator),
                                    info.KnownToImplementIDisposable,
                                    iDisposable,
                                    isAsynchronous);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
                LeaveRegion();

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }

            Debug.Assert(_currentRegion == enumeratorCaptureRegion);
            LeaveRegion();

            return FinishVisitingStatement(operation);

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
                IOperation result;
                EvalStackFrame getEnumeratorFrame = PushStackFrame();

                if (info?.GetEnumeratorMethod != null)
                {
                    IOperation invocation = makeInvocation(operation.Collection.Syntax,
                                                           info.GetEnumeratorMethod,
                                                           info.GetEnumeratorMethod.IsStatic ? null : Visit(operation.Collection),
                                                           info.GetEnumeratorArguments);

                    int enumeratorCaptureId = GetNextCaptureId(enumeratorCaptureRegion);
                    AddStatement(new FlowCaptureOperation(enumeratorCaptureId, operation.Collection.Syntax, invocation));

                    result = new FlowCaptureReferenceOperation(enumeratorCaptureId, operation.Collection.Syntax, info.GetEnumeratorMethod.ReturnType, constantValue: default);
                }
                else
                {
                    // This must be an error case
                    AddStatement(MakeInvalidOperation(type: null, Visit(operation.Collection)));
                    result = new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Collection.Syntax,
                                                  type: null, constantValue: default, isImplicit: true);
                }

                PopStackFrameAndLeaveRegion(getEnumeratorFrame);
                return result;
            }

            IOperation getCondition(IOperation enumeratorRef)
            {
                if (info?.MoveNextMethod != null)
                {
                    var moveNext = makeInvocationDroppingInstanceForStaticMethods(info.MoveNextMethod, enumeratorRef, info.MoveNextArguments);
                    if (operation.IsAsynchronous)
                    {
                        return new AwaitOperation(moveNext, semanticModel: null, operation.Syntax, _compilation.GetSpecialType(SpecialType.System_Boolean), constantValue: default, isImplicit: true);
                    }
                    return moveNext;
                }
                else
                {
                    // This must be an error case
                    return MakeInvalidOperation(_compilation.GetSpecialType(SpecialType.System_Boolean), enumeratorRef);
                }
            }

            IOperation getCurrent(IOperation enumeratorRef)
            {
                if (info?.CurrentProperty != null)
                {
                    return new PropertyReferenceOperation(info.CurrentProperty,
                                                          makeArguments(info.CurrentArguments),
                                                          info.CurrentProperty.IsStatic ? null : enumeratorRef,
                                                          semanticModel: null,
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
                        current = applyConversion(info?.ElementConversion, current, local.Type);

                        return new SimpleAssignmentOperation(isRef: local.RefKind != RefKind.None,
                                                             new LocalReferenceOperation(local,
                                                                                         isDeclaration: true,
                                                                                         semanticModel: null,
                                                                                         declarator.Syntax,
                                                                                         local.Type,
                                                                                         constantValue: default,
                                                                                         isImplicit: true),
                                                             current,
                                                             semanticModel: null,
                                                             declarator.Syntax,
                                                             type: null,
                            constantValue: default, isImplicit: true);

                    case OperationKind.Tuple:
                    case OperationKind.DeclarationExpression:
                        Debug.Assert(info?.ElementConversion?.ToCommonConversion().IsIdentity != false);

                        return new DeconstructionAssignmentOperation(VisitPreservingTupleOperations(operation.LoopControlVariable),
                                                                     current, semanticModel: null,
                                                                     operation.LoopControlVariable.Syntax, operation.LoopControlVariable.Type,
                                                                     constantValue: default, isImplicit: true);
                    default:
                        return new SimpleAssignmentOperation(isRef: false, // In C# this is an error case and VB doesn't support ref locals
                            Visit(operation.LoopControlVariable),
                            current, semanticModel: null, operation.LoopControlVariable.Syntax,
                            operation.LoopControlVariable.Type,
                            constantValue: default, isImplicit: true);
                }
            }

            InvocationOperation makeInvocationDroppingInstanceForStaticMethods(IMethodSymbol method, IOperation instance, ImmutableArray<IArgumentOperation> arguments)
            {
                return makeInvocation(instance.Syntax, method, method.IsStatic ? null : instance, arguments);
            }

            InvocationOperation makeInvocation(SyntaxNode syntax, IMethodSymbol method, IOperation instanceOpt, ImmutableArray<IArgumentOperation> arguments)
            {
                Debug.Assert(method.IsStatic == (instanceOpt == null));
                return new InvocationOperation(method, instanceOpt,
                                                isVirtual: method.IsVirtual || method.IsAbstract || method.IsOverride,
                                                makeArguments(arguments), semanticModel: null, syntax,
                                                method.ReturnType, constantValue: default, isImplicit: true);
            }

            ImmutableArray<IArgumentOperation> makeArguments(ImmutableArray<IArgumentOperation> arguments)
            {
                if (arguments != null)
                {
                    return VisitArguments(arguments);
                }

                return ImmutableArray<IArgumentOperation>.Empty;
            }
        }

        public override IOperation VisitForToLoop(IForToLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            (ILocalSymbol loopObject, ForToLoopOperationUserDefinedInfo userDefinedInfo) = ((BaseForToLoopOperation)operation).Info;
            bool isObjectLoop = (loopObject != null);
            ImmutableArray<ILocalSymbol> locals = operation.Locals;

            if (isObjectLoop)
            {
                locals = locals.Insert(0, loopObject);
            }

            ITypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            BasicBlockBuilder @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            BasicBlockBuilder @break = GetLabeledOrNewBlock(operation.ExitLabel);
            BasicBlockBuilder checkConditionBlock = new BasicBlockBuilder(BasicBlockKind.Block);
            BasicBlockBuilder bodyBlock = new BasicBlockBuilder(BasicBlockKind.Block);

            var loopRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(loopRegion);

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

            UnconditionalBranch(checkConditionBlock);

            LeaveRegion();

            AppendNewBlock(@break);
            return FinishVisitingStatement(operation);

            IOperation tryCallObjectForLoopControlHelper(SyntaxNode syntax, WellKnownMember helper)
            {
                bool isInitialization = (helper == WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);
                var loopObjectReference = new LocalReferenceOperation(loopObject,
                                                                       isDeclaration: isInitialization,
                                                                       semanticModel: null,
                                                                       operation.LoopControlVariable.Syntax, loopObject.Type,
                                                                       constantValue: default, isImplicit: true);

                var method = (IMethodSymbol)_compilation.CommonGetWellKnownTypeMember(helper)?.GetISymbol();
                int parametersCount = WellKnownMembers.GetDescriptor(helper).ParametersCount;

                if (method is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(--parametersCount, fillWithValue: null);
                    builder[--parametersCount] = loopObjectReference;
                    do
                    {
                        builder[--parametersCount] = PopOperand();
                    }
                    while (parametersCount != 0);

                    return MakeInvalidOperation(operation.LimitValue.Syntax, booleanType, builder.ToImmutableAndFree());
                }
                else
                {
                    var builder = ArrayBuilder<IArgumentOperation>.GetInstance(parametersCount, fillWithValue: null);

                    builder[--parametersCount] = new ArgumentOperation(visitLoopControlVariableReference(forceImplicit: true), // Yes we are going to evaluate it again
                                                                       ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       inConversionOpt: null, outConversionOpt: null,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    builder[--parametersCount] = new ArgumentOperation(loopObjectReference,
                                                                       ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       inConversionOpt: null, outConversionOpt: null,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    do
                    {
                        IOperation value = PopOperand();
                        builder[--parametersCount] = new ArgumentOperation(value,
                                                                           ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                           inConversionOpt: null, outConversionOpt: null,
                                                                           semanticModel: null, isInitialization ? value.Syntax : syntax, isImplicit: true);
                    }
                    while (parametersCount != 0);

                    return new InvocationOperation(method, instance: null, isVirtual: false, builder.ToImmutableAndFree(),
                                                    semanticModel: null, operation.LimitValue.Syntax, method.ReturnType,
                                                    constantValue: default, isImplicit: true);
                }
            }

            void initializeLoop()
            {
                EvalStackFrame frame = PushStackFrame();

                PushOperand(visitLoopControlVariableReference(forceImplicit: false));
                PushOperand(Visit(operation.InitialValue));

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

                    PushOperand(Visit(operation.LimitValue));
                    PushOperand(Visit(operation.StepValue));

                    IOperation condition = tryCallObjectForLoopControlHelper(operation.LoopControlVariable.Syntax,
                                                                             WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);

                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);
                }
                else
                {
                    SpillEvalStack();
                    RegionBuilder currentRegion = _currentRegion;

                    limitValueId = GetNextCaptureId(loopRegion);
                    VisitAndCapture(operation.LimitValue, limitValueId);

                    stepValueId = GetNextCaptureId(loopRegion);
                    VisitAndCapture(operation.StepValue, stepValueId);

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

                        int positiveFlagId = GetNextCaptureId(loopRegion);
                        VisitAndCapture(userDefinedInfo.GreaterThanOrEqual.Value, positiveFlagId);

                        positiveFlag = GetCaptureReference(positiveFlagId, userDefinedInfo.GreaterThanOrEqual.Value);

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
                            stepValue = CallNullableMember(stepValue, SpecialMember.System_Nullable_T_GetValueOrDefault);
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
                            int positiveFlagId = GetNextCaptureId(loopRegion);
                            var afterPositiveCheck = new BasicBlockBuilder(BasicBlockKind.Block);

                            if (stepValueIsNull != null)
                            {
                                var whenNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                                ConditionalBranch(stepValueIsNull, jumpIfTrue: false, whenNotNull);
                                _currentBasicBlock = null;

                                // "isUp = false"
                                isUp = new LiteralOperation(semanticModel: null, stepValue.Syntax, booleanType, constantValue: false, isImplicit: true);

                                AddStatement(new FlowCaptureOperation(positiveFlagId, isUp.Syntax, isUp));

                                UnconditionalBranch(afterPositiveCheck);
                                AppendNewBlock(whenNotNull);
                            }

                            IOperation literal = new LiteralOperation(semanticModel: null, stepValue.Syntax, stepValue.Type,
                                                                       constantValue: ConstantValue.Default(stepValueEnumUnderlyingTypeOrSelf.SpecialType).Value,
                                                                       isImplicit: true);

                            isUp = new BinaryOperation(BinaryOperatorKind.GreaterThanOrEqual,
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

                            AddStatement(new FlowCaptureOperation(positiveFlagId, isUp.Syntax, isUp));

                            AppendNewBlock(afterPositiveCheck);

                            positiveFlag = GetCaptureReference(positiveFlagId, isUp);
                        }
                        else
                        {
                            // This must be an error case.
                            // It is fine to do nothing in this case, we are in recovery mode.
                        }
                    }

                    IOperation initialValue = PopOperand();
                    AddStatement(new SimpleAssignmentOperation(isRef: false, // Loop control variable
                        PopOperand(),
                        initialValue,
                        semanticModel: null, operation.InitialValue.Syntax, type: null,
                        constantValue: default, isImplicit: true));

                }

                PopStackFrameAndLeaveRegion(frame);
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

                    EvalStackFrame frame = PushStackFrame();
                    PushOperand(visitLoopControlVariableReference(forceImplicit: true));

                    IOperation condition = tryCallObjectForLoopControlHelper(operation.LimitValue.Syntax,
                                                                             WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForNextCheckObj);
                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);

                    PopStackFrameAndLeaveRegion(frame);
                    return;
                }
                else if (userDefinedInfo != null)
                {
                    Debug.Assert(_forToLoopBinaryOperatorLeftOperand == null);
                    Debug.Assert(_forToLoopBinaryOperatorRightOperand == null);

                    // Generate If(positiveFlag, controlVariable <= limit, controlVariable >= limit)
                    EvalStackFrame frame = PushStackFrame();

                    // Spill control variable reference, we are going to have branches here.
                    PushOperand(visitLoopControlVariableReference(forceImplicit: true)); // Yes we are going to evaluate it again
                    SpillEvalStack();
                    IOperation controlVariableReferenceForCondition = PopOperand();

                    var notPositive = new BasicBlockBuilder(BasicBlockKind.Block);
                    ConditionalBranch(positiveFlag, jumpIfTrue: false, notPositive);
                    _currentBasicBlock = null;

                    _forToLoopBinaryOperatorLeftOperand = controlVariableReferenceForCondition;
                    _forToLoopBinaryOperatorRightOperand = GetCaptureReference(limitValueId, operation.LimitValue);

                    VisitConditionalBranch(userDefinedInfo.LessThanOrEqual.Value, ref @break, jumpIfTrue: false);
                    UnconditionalBranch(bodyBlock);

                    AppendNewBlock(notPositive);

                    _forToLoopBinaryOperatorLeftOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorLeftOperand);
                    _forToLoopBinaryOperatorRightOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorRightOperand);

                    VisitConditionalBranch(userDefinedInfo.GreaterThanOrEqual.Value, ref @break, jumpIfTrue: false);
                    UnconditionalBranch(bodyBlock);

                    PopStackFrameAndLeaveRegion(frame);

                    _forToLoopBinaryOperatorLeftOperand = null;
                    _forToLoopBinaryOperatorRightOperand = null;
                    return;
                }
                else
                {
                    EvalStackFrame frame = PushStackFrame();

                    PushOperand(visitLoopControlVariableReference(forceImplicit: true)); // Yes we are going to evaluate it again
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
                        PushOperand(negateIfStepNegative(PopOperand()));
                        limitReference = negateIfStepNegative(limitReference);
                    }

                    IOperation condition;

                    if (comparisonKind != BinaryOperatorKind.None)
                    {
                        condition = new BinaryOperation(comparisonKind,
                                                                 PopOperand(),
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

                        ConditionalBranch(condition, jumpIfTrue: false, @break);
                        UnconditionalBranch(bodyBlock);

                        PopStackFrameAndLeaveRegion(frame);
                        return;
                    }

                    if (positiveFlag == null)
                    {
                        // Must be an error case.
                        condition = MakeInvalidOperation(operation.LimitValue.Syntax, booleanType, PopOperand(), limitReference);
                        ConditionalBranch(condition, jumpIfTrue: false, @break);
                        UnconditionalBranch(bodyBlock);

                        PopStackFrameAndLeaveRegion(frame);
                        return;
                    }

                    IOperation eitherLimitOrControlVariableIsNull = null;

                    if (ITypeSymbolHelpers.IsNullableType(operation.LimitValue.Type))
                    {
                        eitherLimitOrControlVariableIsNull = new BinaryOperation(BinaryOperatorKind.Or,
                                                                                          MakeIsNullOperation(limitReference, booleanType),
                                                                                          MakeIsNullOperation(PopOperand(), booleanType),
                                                                                          isLifted: false,
                                                                                          isChecked: false,
                                                                                          isCompareText: false,
                                                                                          operatorMethod: null,
                                                                                          unaryOperatorMethod: null,
                                                                                          semanticModel: null,
                                                                                          operation.StepValue.Syntax,
                                                                                          _compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                                          constantValue: default,
                                                                                          isImplicit: true);

                        // if either limit or control variable is null, we exit the loop
                        var whenBothNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                        ConditionalBranch(eitherLimitOrControlVariableIsNull, jumpIfTrue: false, whenBothNotNull);
                        UnconditionalBranch(@break);

                        PopStackFrameAndLeaveRegion(frame);

                        AppendNewBlock(whenBothNotNull);

                        frame = PushStackFrame();

                        PushOperand(CallNullableMember(visitLoopControlVariableReference(forceImplicit: true), SpecialMember.System_Nullable_T_GetValueOrDefault)); // Yes we are going to evaluate it again
                        limitReference = CallNullableMember(GetCaptureReference(limitValueId, operation.LimitValue), SpecialMember.System_Nullable_T_GetValueOrDefault);
                    }

                    // If (positiveFlag, ctrl <= limit, ctrl >= limit)

                    SpillEvalStack();

                    IOperation controlVariableReferenceforCondition = PopOperand();

                    var notPositive = new BasicBlockBuilder(BasicBlockKind.Block);
                    ConditionalBranch(positiveFlag, jumpIfTrue: false, notPositive);
                    _currentBasicBlock = null;

                    condition = new BinaryOperation(BinaryOperatorKind.LessThanOrEqual,
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

                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);

                    AppendNewBlock(notPositive);

                    condition = new BinaryOperation(BinaryOperatorKind.GreaterThanOrEqual,
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

                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);

                    PopStackFrameAndLeaveRegion(frame);
                    return;
                }

                throw ExceptionUtilities.Unreachable;
            }

            // Produce "(operand Xor (step >> 31))"
            // where 31 is actually the size-1
            IOperation negateIfStepNegative(IOperation operand)
            {
                int bits = stepEnumUnderlyingTypeOrSelf.SpecialType.VBForToShiftBits();

                var shiftConst = new LiteralOperation(semanticModel: null, operand.Syntax, _compilation.GetSpecialType(SpecialType.System_Int32),
                                                       constantValue: bits, isImplicit: true);

                var shiftedStep = new BinaryOperation(BinaryOperatorKind.RightShift,
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

                return new BinaryOperation(BinaryOperatorKind.ExclusiveOr,
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

                    EvalStackFrame frame = PushStackFrame();
                    IOperation controlVariableReferenceForAssignment = visitLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again

                    // We are going to evaluate control variable again and that might require branches
                    PushOperand(controlVariableReferenceForAssignment);

                    // Generate: controlVariable + stepValue
                    _forToLoopBinaryOperatorLeftOperand = visitLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    _forToLoopBinaryOperatorRightOperand = GetCaptureReference(stepValueId, operation.StepValue);

                    IOperation increment = Visit(userDefinedInfo.Addition.Value);

                    _forToLoopBinaryOperatorLeftOperand = null;
                    _forToLoopBinaryOperatorRightOperand = null;

                    controlVariableReferenceForAssignment = PopOperand();
                    AddStatement(new SimpleAssignmentOperation(isRef: false,
                        controlVariableReferenceForAssignment,
                        increment,
                        semanticModel: null,
                        controlVariableReferenceForAssignment.Syntax,
                        type: null,
                        constantValue: default,
                        isImplicit: true));

                    PopStackFrameAndLeaveRegion(frame);
                }
                else
                {
                    BasicBlockBuilder afterIncrement = new BasicBlockBuilder(BasicBlockKind.Block);
                    IOperation controlVariableReferenceForAssignment;
                    bool isNullable = ITypeSymbolHelpers.IsNullableType(operation.StepValue.Type);

                    EvalStackFrame frame = PushStackFrame();
                    PushOperand(visitLoopControlVariableReference(forceImplicit: true)); // Yes we are going to evaluate it again

                    if (isNullable)
                    {
                        // Spill control variable reference, we are going to have branches here.
                        SpillEvalStack();

                        BasicBlockBuilder whenNotNull = new BasicBlockBuilder(BasicBlockKind.Block);

                        EvalStackFrame nullCheckFrame = PushStackFrame();
                        IOperation condition = new BinaryOperation(BinaryOperatorKind.Or,
                                                                            MakeIsNullOperation(GetCaptureReference(stepValueId, operation.StepValue), booleanType),
                                                                            MakeIsNullOperation(visitLoopControlVariableReference(forceImplicit: true), // Yes we are going to evaluate it again
                                                                                                booleanType),
                                                                            isLifted: false,
                                                                            isChecked: false,
                                                                            isCompareText: false,
                                                                            operatorMethod: null,
                                                                            unaryOperatorMethod: null,
                                                                            semanticModel: null,
                                                                            operation.StepValue.Syntax,
                                                                            _compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                            constantValue: default,
                                                                            isImplicit: true);

                        ConditionalBranch(condition, jumpIfTrue: false, whenNotNull);
                        _currentBasicBlock = null;

                        PopStackFrameAndLeaveRegion(nullCheckFrame);

                        controlVariableReferenceForAssignment = OperationCloner.CloneOperation(PeekOperand());
                        Debug.Assert(controlVariableReferenceForAssignment.Kind == OperationKind.FlowCaptureReference);

                        AddStatement(new SimpleAssignmentOperation(isRef: false,
                            controlVariableReferenceForAssignment,
                            new DefaultValueOperation(semanticModel: null,
                                controlVariableReferenceForAssignment.Syntax,
                                controlVariableReferenceForAssignment.Type,
                                constantValue: default,
                                isImplicit: true),
                            semanticModel: null,
                            controlVariableReferenceForAssignment.Syntax,
                            type: null,
                            constantValue: default,
                            isImplicit: true));

                        UnconditionalBranch(afterIncrement);

                        AppendNewBlock(whenNotNull);
                    }

                    IOperation controlVariableReferenceForIncrement = visitLoopControlVariableReference(forceImplicit: true); // Yes we are going to evaluate it again
                    IOperation stepValueForIncrement = GetCaptureReference(stepValueId, operation.StepValue);

                    if (isNullable)
                    {
                        Debug.Assert(ITypeSymbolHelpers.IsNullableType(controlVariableReferenceForIncrement.Type));
                        controlVariableReferenceForIncrement = CallNullableMember(controlVariableReferenceForIncrement, SpecialMember.System_Nullable_T_GetValueOrDefault);
                        stepValueForIncrement = CallNullableMember(stepValueForIncrement, SpecialMember.System_Nullable_T_GetValueOrDefault);
                    }

                    IOperation increment = new BinaryOperation(BinaryOperatorKind.Add,
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

                    controlVariableReferenceForAssignment = PopOperand();

                    if (isNullable)
                    {
                        increment = MakeNullable(increment, controlVariableReferenceForAssignment.Type);
                    }

                    AddStatement(new SimpleAssignmentOperation(isRef: false,
                        controlVariableReferenceForAssignment,
                        increment,
                        semanticModel: null,
                        controlVariableReferenceForAssignment.Syntax,
                        type: null,
                        constantValue: default,
                        isImplicit: true));

                    PopStackFrame(frame, mergeNestedRegions: !isNullable); // We have a branch out in between when nullable is involved
                    LeaveRegionIfAny(frame);
                    AppendNewBlock(afterIncrement);
                }
            }

            IOperation visitLoopControlVariableReference(bool forceImplicit)
            {
                switch (operation.LoopControlVariable.Kind)
                {
                    case OperationKind.VariableDeclarator:
                        var declarator = (IVariableDeclaratorOperation)operation.LoopControlVariable;
                        ILocalSymbol local = declarator.Symbol;

                        return new LocalReferenceOperation(local, isDeclaration: true, semanticModel: null,
                                                            declarator.Syntax, local.Type, constantValue: default, isImplicit: true);

                    default:
                        Debug.Assert(!_forceImplicit);
                        _forceImplicit = forceImplicit;
                        IOperation result = Visit(operation.LoopControlVariable);
                        _forceImplicit = false;
                        return result;
                }
            }
        }

        private static FlowCaptureReferenceOperation GetCaptureReference(int id, IOperation underlying)
        {
            return new FlowCaptureReferenceOperation(id, underlying.Syntax, underlying.Type, underlying.ConstantValue);
        }

        internal override IOperation VisitAggregateQuery(IAggregateQueryOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            IOperation previousAggregationGroup = _currentAggregationGroup;
            _currentAggregationGroup = VisitAndCapture(operation.Group);

            IOperation result = Visit(operation.Aggregation);

            _currentAggregationGroup = previousAggregationGroup;
            return result;
        }

        public override IOperation VisitSwitch(ISwitchOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation switchValue = VisitAndCapture(operation.Value);

            ImmutableArray<ILocalSymbol> locals = getLocals();
            var switchRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(switchRegion);

            BasicBlockBuilder defaultBody = null; // Adjusted in handleSection
            BasicBlockBuilder @break = GetLabeledOrNewBlock(operation.ExitLabel);

            foreach (ISwitchCaseOperation section in operation.Cases)
            {
                handleSection(section);
            }

            Debug.Assert(_currentRegion == switchRegion);
            if (defaultBody != null)
            {
                UnconditionalBranch(defaultBody);
            }

            LeaveRegion();

            AppendNewBlock(@break);

            return FinishVisitingStatement(operation);

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

                IOperation condition = ((BaseSwitchCaseOperation)section).Condition;
                if (condition != null)
                {
                    Debug.Assert(section.Clauses.All(c => c.Label == null));
                    Debug.Assert(_currentSwitchOperationExpression == null);
                    _currentSwitchOperationExpression = switchValue;
                    VisitConditionalBranch(condition, ref nextSection, jumpIfTrue: false);
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

                    UnconditionalBranch(nextSection);
                }

                AppendNewBlock(body);

                VisitStatements(section.Body);

                UnconditionalBranch(@break);

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

                            EvalStackFrame frame = PushStackFrame();
                            PushOperand(OperationCloner.CloneOperation(switchValue));
                            IOperation rightOperand = Visit(compareWith);
                            IOperation leftOperand = PopOperand();

                            if (isLifted)
                            {
                                if (!leftIsNullable)
                                {
                                    if (leftOperand.Type != null)
                                    {
                                        leftOperand = MakeNullable(leftOperand, compareWith.Type);
                                    }
                                }
                                else if (!rightIsNullable && rightOperand.Type != null)
                                {
                                    rightOperand = MakeNullable(rightOperand, operation.Value.Type);
                                }
                            }

                            condition = new BinaryOperation(BinaryOperatorKind.Equals,
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

                            ConditionalBranch(condition, jumpIfTrue: false, nextCase);

                            PopStackFrameAndLeaveRegion(frame);

                            AppendNewBlock(labeled);
                            _currentBasicBlock = null;
                        }

                    case CaseKind.Pattern:
                        {
                            var patternClause = (IPatternCaseClauseOperation)caseClause;

                            EvalStackFrame frame = PushStackFrame();
                            PushOperand(OperationCloner.CloneOperation(switchValue));
                            var pattern = (IPatternOperation)Visit(patternClause.Pattern);
                            condition = new IsPatternOperation(PopOperand(), pattern, semanticModel: null,
                                                                patternClause.Pattern.Syntax, booleanType, constantValue: default, isImplicit: true);
                            ConditionalBranch(condition, jumpIfTrue: false, nextCase);

                            PopStackFrameAndLeaveRegion(frame);

                            if (patternClause.Guard != null)
                            {
                                AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
                                VisitConditionalBranch(patternClause.Guard, ref nextCase, jumpIfTrue: false);
                            }

                            AppendNewBlock(labeled);
                            _currentBasicBlock = null;
                        }
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
                        UnconditionalBranch(nextCase);
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

        private IOperation MakeNullable(IOperation operand, ITypeSymbol type)
        {
            Debug.Assert(ITypeSymbolHelpers.IsNullableType(type));
            Debug.Assert(ITypeSymbolHelpers.GetNullableUnderlyingType(type).Equals(operand.Type));

            return CreateConversion(operand, type);
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
            StartVisitingStatement(operation);
            BasicBlockBuilder current = CurrentBasicBlock;
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block), linkToPrevious: false);
            Debug.Assert(current.BranchValue == null);
            Debug.Assert(!current.HasCondition);
            Debug.Assert(current.FallThrough.Destination == null);
            Debug.Assert(current.FallThrough.Kind == ControlFlowBranchSemantics.None);
            current.FallThrough.Kind = ControlFlowBranchSemantics.ProgramTermination;
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitForLoop(IForLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

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
                VisitConditionalBranch(operation.Condition, ref @break, jumpIfTrue: false);
            }

            VisitStatement(operation.Body);

            var @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            AppendNewBlock(@continue);

            VisitStatements(operation.AtLoopBottom);

            UnconditionalBranch(start);

            LeaveRegion(); // ConditionLocals
            LeaveRegion(); // Locals

            AppendNewBlock(@break);

            return FinishVisitingStatement(operation);
        }

        internal override IOperation VisitFixed(IFixedOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            HandleVariableDeclarations(operation.Variables);

            VisitStatement(operation.Body);

            LeaveRegion();
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, int? captureIdForResult)
        {
            // Anything that has a declaration group (such as for loops) needs to handle them directly itself,
            // this should only be encountered by the visitor for declaration statements.
            StartVisitingStatement(operation);

            HandleVariableDeclarations(operation);
            return FinishVisitingStatement(operation);
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
            if (declarator.Initializer == null && declaration.Initializer == null)
            {
                return;
            }

            ILocalSymbol localSymbol = declarator.Symbol;

            // If the local is a static (possible in VB), then we create a semaphore for conditional execution of the initializer.
            BasicBlockBuilder afterInitialization = null;
            if (localSymbol.IsStatic)
            {
                afterInitialization = new BasicBlockBuilder(BasicBlockKind.Block);

                ITypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
                var initializationSemaphore = new StaticLocalInitializationSemaphoreOperation(localSymbol, declarator.Syntax, booleanType);
                ConditionalBranch(initializationSemaphore, jumpIfTrue: false, afterInitialization);

                _currentBasicBlock = null;
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.StaticLocalInitializer));
            }

            EvalStackFrame frame = PushStackFrame();

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

            Debug.Assert(initializer != null);

            // If we have an afterInitialization, then we must have static local and an initializer to ensure we don't create empty regions that can't be cleaned up.
            Debug.Assert(afterInitialization == null || localSymbol.IsStatic);

            // We can't use the IdentifierToken as the syntax for the local reference, so we use the
            // entire declarator as the node
            var localRef = new LocalReferenceOperation(localSymbol, isDeclaration: true, semanticModel: null, declarator.Syntax, localSymbol.Type, constantValue: default, isImplicit: true);
            var assignment = new SimpleAssignmentOperation(isRef: localSymbol.IsRef, localRef, initializer, semanticModel: null, assignmentSyntax, localRef.Type, constantValue: default, isImplicit: true);
            AddStatement(assignment);

            PopStackFrameAndLeaveRegion(frame);

            if (localSymbol.IsStatic)
            {
                LeaveRegion();
                AppendNewBlock(afterInitialization);
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
            EvalStackFrame frame = PushStackFrame();
            IOperation instance = operation.TargetMethod.IsStatic ? null : operation.Instance;
            (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            PopStackFrame(frame);
            return new InvocationOperation(operation.TargetMethod, visitedInstance, operation.IsVirtual, visitedArguments, semanticModel: null, operation.Syntax,
                                            operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        private (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) VisitInstanceWithArguments(IOperation instance, ImmutableArray<IArgumentOperation> arguments)
        {
            if (instance != null)
            {
                PushOperand(Visit(instance));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(arguments);
            IOperation visitedInstance = instance == null ? null : PopOperand();

            return (visitedInstance, visitedArguments);
        }

        internal override IOperation VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, int? argument)
        {
            EvalStackFrame frame = PushStackFrame();
            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            IOperation initializedInstance = new NoPiaObjectCreationOperation(initializer: null, semanticModel: null, operation.Syntax, operation.Type,
                                                                              operation.ConstantValue, IsImplicit(operation));
            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            EvalStackFrame argumentsFrame = PushStackFrame();
            ImmutableArray<IArgumentOperation> visitedArgs = VisitArguments(operation.Arguments);
            PopStackFrame(argumentsFrame);
            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            IOperation initializedInstance = new ObjectCreationOperation(operation.Constructor, initializer: null, visitedArgs, semanticModel: null,
                                                                          operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            var initializedInstance = new TypeParameterObjectCreationOperation(initializer: null, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            EvalStackFrame argumentsFrame = PushStackFrame();
            ImmutableArray<IOperation> visitedArguments = VisitArray(operation.Arguments);
            PopStackFrame(argumentsFrame);

            var hasDynamicArguments = (HasDynamicArgumentsExpression)operation;
            IOperation initializedInstance = new DynamicObjectCreationOperation(visitedArguments, hasDynamicArguments.ArgumentNames, hasDynamicArguments.ArgumentRefKinds,
                                                                                 initializer: null, semanticModel: null, operation.Syntax, operation.Type,
                                                                                 operation.ConstantValue, IsImplicit(operation));

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        private IOperation HandleObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation initializer, IOperation objectCreation)
        {
            // If the initializer is null, nothing to spill. Just return the original instance.
            if (initializer == null || initializer.Initializers.IsEmpty)
            {
                return objectCreation;
            }

            // Initializer wasn't null, so spill the stack and capture the initialized instance. Returns a reference to the captured instance.
            PushOperand(objectCreation);
            SpillEvalStack();
            objectCreation = PopOperand();

            visitInitializer(initializer, objectCreation);

            return objectCreation;

            void visitInitializer(IObjectOrCollectionInitializerOperation initializerOperation, IOperation initializedInstance)
            {
                ImplicitInstanceInfo previousInitializedInstance = _currentImplicitInstance;
                _currentImplicitInstance = new ImplicitInstanceInfo(initializedInstance);

                foreach (IOperation innerInitializer in initializerOperation.Initializers)
                {
                    handleInitializer(innerInitializer);
                }

                _currentImplicitInstance = previousInitializedInstance;
            }

            void handleInitializer(IOperation innerInitializer)
            {
                switch (innerInitializer.Kind)
                {
                    case OperationKind.MemberInitializer:
                        handleMemberInitializer((IMemberInitializerOperation)innerInitializer);
                        return;

                    case OperationKind.SimpleAssignment:
                        handleSimpleAssignment((ISimpleAssignmentOperation)innerInitializer);
                        return;

                    default:
                        // This assert is to document the list of things we know are possible to go through the default handler. It's possible there
                        // are other nodes that will go through here, and if a new test triggers this assert, it will likely be fine to just add
                        // the node type to the assert. It's here merely to ensure that we think about whether that node type actually does need
                        // special handling in the context of a collection or object initializer before just assuming that it's fine.
#if DEBUG
                        var validKinds = ImmutableArray.Create(OperationKind.Invocation, OperationKind.DynamicInvocation, OperationKind.Increment, OperationKind.Literal,
                                                               OperationKind.LocalReference, OperationKind.Binary, OperationKind.FieldReference, OperationKind.Invalid);
                        Debug.Assert(validKinds.Contains(innerInitializer.Kind));
#endif
                        EvalStackFrame frame = PushStackFrame();
                        AddStatement(Visit(innerInitializer));
                        PopStackFrameAndLeaveRegion(frame);
                        return;
                }
            }

            void handleSimpleAssignment(ISimpleAssignmentOperation assignmentOperation)
            {
                EvalStackFrame frame = PushStackFrame();

                bool pushSuccess = tryPushTarget(assignmentOperation.Target);
                IOperation result;

                if (!pushSuccess)
                {
                    // Error case. We don't try any error recovery here, just return whatever the default visit would.
                    result = Visit(assignmentOperation);
                }
                else
                {
                    // We push the target, which effectively pushes individual components of the target (ie the instance, and arguments if present).
                    // After that has been pushed, we visit the value of the assignment, to ensure that the instance is captured if
                    // needed. Finally, we reassemble the target, which will pull the potentially captured instance from the stack
                    // and reassemble the member reference from the parts.
                    IOperation right = Visit(assignmentOperation.Value);
                    IOperation left = popTarget(assignmentOperation.Target);

                    result = new SimpleAssignmentOperation(assignmentOperation.IsRef, left, right, semanticModel: null, assignmentOperation.Syntax,
                        assignmentOperation.Type, assignmentOperation.ConstantValue, IsImplicit(assignmentOperation));
                }

                AddStatement(result);
                PopStackFrameAndLeaveRegion(frame);
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

                EvalStackFrame frame = PushStackFrame();
                bool pushSuccess = tryPushTarget(memberInitializer.InitializedMember);
                IOperation instance = pushSuccess ? popTarget(memberInitializer.InitializedMember) : Visit(memberInitializer.InitializedMember);
                visitInitializer(memberInitializer.Initializer, instance);
                PopStackFrameAndLeaveRegion(frame);
            }

            bool tryPushTarget(IOperation instance)
            {
                switch (instance.Kind)
                {
                    case OperationKind.FieldReference:
                    case OperationKind.EventReference:
                    case OperationKind.PropertyReference:
                        var memberReference = (IMemberReferenceOperation)instance;

                        if (memberReference.Kind == OperationKind.PropertyReference)
                        {
                            // We assume all arguments have side effects and spill them. We only avoid recapturing things that have already been captured once.
                            VisitAndPushArray(((IPropertyReferenceOperation)memberReference).Arguments, UnwrapArgument);
                            SpillEvalStack();
                        }

                        // If there is control flow in the value being assigned, we want to make sure that
                        // the instance is captured appropriately, but the setter/field load in the reference will only be evaluated after
                        // the value has been evaluated. So we assemble the reference after visiting the value.
                        if (!memberReference.Member.IsStatic && memberReference.Instance != null)
                        {
                            PushOperand(Visit(memberReference.Instance));
                        }
                        return true;

                    case OperationKind.ArrayElementReference:
                        var arrayReference = (IArrayElementReferenceOperation)instance;
                        VisitAndPushArray(arrayReference.Indices);
                        SpillEvalStack();
                        PushOperand(Visit(arrayReference.ArrayReference));
                        return true;

                    case OperationKind.DynamicIndexerAccess:
                        var dynamicIndexer = (IDynamicIndexerAccessOperation)instance;
                        VisitAndPushArray(dynamicIndexer.Arguments);
                        SpillEvalStack();
                        if (dynamicIndexer.Operation != null)
                        {
                            PushOperand(Visit(dynamicIndexer.Operation));
                        }
                        return true;

                    case OperationKind.DynamicMemberReference:
                        var dynamicReference = (IDynamicMemberReferenceOperation)instance;
                        if (dynamicReference.Instance != null)
                        {
                            PushOperand(Visit(dynamicReference.Instance));
                        }
                        return true;

                    default:
                        // As in the assert in handleInitializer, this assert documents the operation kinds that we know go through this path,
                        // and it is possible others go through here as well. If they are encountered, we simply need to ensure
                        // that they don't have any interesting semantics in object or collection initialization contexts and add them to the
                        // assert.
                        Debug.Assert(instance.Kind == OperationKind.Invalid || instance.Kind == OperationKind.None);
                        return false;
                }
            }

            IOperation popTarget(IOperation originalTarget)
            {
                IOperation instance;
                switch (originalTarget.Kind)
                {
                    case OperationKind.FieldReference:
                        var fieldReference = (IFieldReferenceOperation)originalTarget;
                        instance = (!fieldReference.Member.IsStatic && fieldReference.Instance != null) ? PopOperand() : null;
                        return new FieldReferenceOperation(fieldReference.Field, fieldReference.IsDeclaration, instance, semanticModel: null,
                                                            fieldReference.Syntax, fieldReference.Type, fieldReference.ConstantValue, IsImplicit(fieldReference));
                    case OperationKind.EventReference:
                        var eventReference = (IEventReferenceOperation)originalTarget;
                        instance = (!eventReference.Member.IsStatic && eventReference.Instance != null) ? PopOperand() : null;
                        return new EventReferenceOperation(eventReference.Event, instance, semanticModel: null, eventReference.Syntax,
                                                            eventReference.Type, eventReference.ConstantValue, IsImplicit(eventReference));
                    case OperationKind.PropertyReference:
                        var propertyReference = (IPropertyReferenceOperation)originalTarget;
                        instance = (!propertyReference.Member.IsStatic && propertyReference.Instance != null) ? PopOperand() : null;
                        ImmutableArray<IArgumentOperation> propertyArguments = PopArray(propertyReference.Arguments, RewriteArgumentFromArray);
                        return new PropertyReferenceOperation(propertyReference.Property, propertyArguments, instance, semanticModel: null, propertyReference.Syntax,
                                                               propertyReference.Type, propertyReference.ConstantValue, IsImplicit(propertyReference));
                    case OperationKind.ArrayElementReference:
                        var arrayElementReference = (IArrayElementReferenceOperation)originalTarget;
                        instance = PopOperand();
                        ImmutableArray<IOperation> indices = PopArray(arrayElementReference.Indices);
                        return new ArrayElementReferenceOperation(instance, indices, semanticModel: null, originalTarget.Syntax, originalTarget.Type,
                                                                   originalTarget.ConstantValue, IsImplicit(originalTarget));
                    case OperationKind.DynamicIndexerAccess:
                        var dynamicAccess = (BaseDynamicIndexerAccessOperation)originalTarget;
                        instance = dynamicAccess.Operation != null ? PopOperand() : null;
                        ImmutableArray<IOperation> arguments = PopArray(dynamicAccess.Arguments);
                        return new DynamicIndexerAccessOperation(instance, arguments, dynamicAccess.ArgumentNames, dynamicAccess.ArgumentRefKinds, semanticModel: null,
                                                                  dynamicAccess.Syntax, dynamicAccess.Type, dynamicAccess.ConstantValue, IsImplicit(dynamicAccess));
                    case OperationKind.DynamicMemberReference:
                        var dynamicReference = (IDynamicMemberReferenceOperation)originalTarget;
                        instance = dynamicReference.Instance != null ? PopOperand() : null;
                        return new DynamicMemberReferenceOperation(instance, dynamicReference.MemberName, dynamicReference.TypeArguments,
                                                                    dynamicReference.ContainingType, semanticModel: null, dynamicReference.Syntax,
                                                                    dynamicReference.Type, dynamicReference.ConstantValue, IsImplicit(dynamicReference));
                    default:
                        // Unlike in tryPushTarget, we assume that if this method is called, we were successful in pushing, so
                        // this must be one of the explicitly handled kinds
                        throw ExceptionUtilities.UnexpectedValue(originalTarget.Kind);
                }
            }
        }

        public override IOperation VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, int? captureIdForResult)
        {
            Debug.Fail("This code path should not be reachable.");
            return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
        }

        public override IOperation VisitMemberInitializer(IMemberInitializerOperation operation, int? captureIdForResult)
        {
            Debug.Fail("This code path should not be reachable.");
            return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
        }

        public override IOperation VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, int? captureIdForResult)
        {
            if (operation.Initializers.IsEmpty)
            {
                return new AnonymousObjectCreationOperation(initializers: ImmutableArray<IOperation>.Empty, semanticModel: null,
                    operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
            }

            ImplicitInstanceInfo savedCurrentImplicitInstance = _currentImplicitInstance;
            _currentImplicitInstance = new ImplicitInstanceInfo((INamedTypeSymbol)operation.Type);

            SpillEvalStack();

            EvalStackFrame frame = PushStackFrame();

            var initializerBuilder = ArrayBuilder<IOperation>.GetInstance(operation.Initializers.Length);
            for (int i = 0; i < operation.Initializers.Length; i++)
            {
                var simpleAssignment = (ISimpleAssignmentOperation)operation.Initializers[i];
                var propertyReference = (IPropertyReferenceOperation)simpleAssignment.Target;

                Debug.Assert(propertyReference != null);
                Debug.Assert(propertyReference.Arguments.IsEmpty);
                Debug.Assert(propertyReference.Instance != null);
                Debug.Assert(propertyReference.Instance.Kind == OperationKind.InstanceReference);
                Debug.Assert(((IInstanceReferenceOperation)propertyReference.Instance).ReferenceKind == InstanceReferenceKind.ImplicitReceiver);

                var visitedPropertyInstance = new InstanceReferenceOperation(InstanceReferenceKind.ImplicitReceiver, semanticModel: null,
                    propertyReference.Instance.Syntax, propertyReference.Instance.Type, propertyReference.Instance.ConstantValue, IsImplicit(propertyReference.Instance));
                IOperation visitedTarget = new PropertyReferenceOperation(propertyReference.Property, ImmutableArray<IArgumentOperation>.Empty, visitedPropertyInstance,
                    semanticModel: null, propertyReference.Syntax, propertyReference.Type, propertyReference.ConstantValue, IsImplicit(propertyReference));
                IOperation visitedValue = visitAndCaptureInitializer(propertyReference.Property, simpleAssignment.Value);
                var visitedAssignment = new SimpleAssignmentOperation(isRef: simpleAssignment.IsRef, visitedTarget, visitedValue,
                    semanticModel: null, simpleAssignment.Syntax, simpleAssignment.Type, simpleAssignment.ConstantValue, IsImplicit(simpleAssignment));
                initializerBuilder.Add(visitedAssignment);
            }

            _currentImplicitInstance.Free();
            _currentImplicitInstance = savedCurrentImplicitInstance;

            for (int i = 0; i < initializerBuilder.Count; i++)
            {
                PopOperand();
            }

            PopStackFrame(frame);
            return new AnonymousObjectCreationOperation(initializerBuilder.ToImmutableAndFree(), semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));

            IOperation visitAndCaptureInitializer(IPropertySymbol initializedProperty, IOperation initializer)
            {
                PushOperand(Visit(initializer));
                SpillEvalStack();
                IOperation captured = PeekOperand(); // Keep it on the stack so that we know it is still used.

                // For VB, previously initialized properties can be referenced in subsequent initializers.
                // We store the capture Id for the property for such property references.
                // Note that for VB error cases with duplicate property names, all the property symbols are considered equal.
                // We use the last duplicate property's capture id and use it in subsequent property references.
                _currentImplicitInstance.AnonymousTypePropertyValues[initializedProperty] = captured;

                return captured;
            }
        }

        public override IOperation VisitLocalFunction(ILocalFunctionOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            RegionBuilder owner = _currentRegion;

            while (owner.IsStackSpillRegion)
            {
                owner = owner.Enclosing;
            }

            owner.Add(operation.Symbol, operation);
            return FinishVisitingStatement(operation);
        }

        private IOperation VisitLocalFunctionAsRoot(ILocalFunctionOperation operation)
        {
            Debug.Assert(_currentStatement == null);
            VisitMethodBodies(operation.Body, operation.IgnoredBody);
            return null;
        }

        public override IOperation VisitAnonymousFunction(IAnonymousFunctionOperation operation, int? captureIdForResult)
        {
            _haveAnonymousFunction = true;
            return new FlowAnonymousFunctionOperation(GetCurrentContext(), operation, IsImplicit(operation));
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
            EvalStackFrame frame = PushStackFrame();
            VisitAndPushArray(operation.DimensionSizes);
            var visitedInitializer = (IArrayInitializerOperation)Visit(operation.Initializer);
            ImmutableArray<IOperation> visitedDimensions = PopArray(operation.DimensionSizes);
            PopStackFrame(frame);
            return new ArrayCreationOperation(visitedDimensions, visitedInitializer, semanticModel: null,
                                               operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitArrayInitializer(IArrayInitializerOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            visitAndPushArrayInitializerValues(operation);
            return PopStackFrame(frame, popAndAssembleArrayInitializerValues(operation));

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
                        PushOperand(Visit(elementValue));
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
                        visitedElementValue = PopOperand();
                    }

                    builder.Add(visitedElementValue);
                }

                builder.ReverseContents();
                return new ArrayInitializerOperation(builder.ToImmutableAndFree(), semanticModel: null, initializer.Syntax, initializer.ConstantValue, IsImplicit(initializer));
            }
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, int? captureIdForResult)
        {
            if (operation.ReferenceKind == InstanceReferenceKind.ImplicitReceiver)
            {
                // When we're in an object or collection initializer, we need to replace the instance reference with a reference to the object being initialized
                Debug.Assert(operation.IsImplicit);

                if (_currentImplicitInstance.ImplicitInstance != null)
                {
                    return OperationCloner.CloneOperation(_currentImplicitInstance.ImplicitInstance);
                }
                else
                {
                    Debug.Fail("This code path should not be reachable.");
                    return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
                }
            }
            else
            {
                return new InstanceReferenceOperation(operation.ReferenceKind, semanticModel: null, operation.Syntax, operation.Type,
                                                       operation.ConstantValue, IsImplicit(operation));
            }
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();

            if (operation.Operation != null)
            {
                if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
                {
                    var instance = ((IDynamicMemberReferenceOperation)operation.Operation).Instance;
                    if (instance != null)
                    {
                        PushOperand(Visit(instance));
                    }
                }
                else
                {
                    PushOperand(Visit(operation.Operation));
                }
            }

            ImmutableArray<IOperation> rewrittenArguments = VisitArray(operation.Arguments);

            IOperation rewrittenOperation;
            if (operation.Operation == null)
            {
                rewrittenOperation = null;
            }
            else if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
            {
                var dynamicMemberReference = (IDynamicMemberReferenceOperation)operation.Operation;
                IOperation rewrittenInstance = dynamicMemberReference.Instance != null ? PopOperand() : null;
                rewrittenOperation = new DynamicMemberReferenceOperation(rewrittenInstance, dynamicMemberReference.MemberName, dynamicMemberReference.TypeArguments,
                    dynamicMemberReference.ContainingType, semanticModel: null, dynamicMemberReference.Syntax, dynamicMemberReference.Type, dynamicMemberReference.ConstantValue, IsImplicit(dynamicMemberReference));
            }
            else
            {
                rewrittenOperation = PopOperand();
            }

            PopStackFrame(frame);
            return new DynamicInvocationOperation(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, int? captureIdForResult)
        {
            if (operation.Operation != null)
            {
                PushOperand(Visit(operation.Operation));
            }

            ImmutableArray<IOperation> rewrittenArguments = VisitArray(operation.Arguments);
            IOperation rewrittenOperation = operation.Operation != null ? PopOperand() : null;

            return new DynamicIndexerAccessOperation(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, int? captureIdForResult)
        {
            return new DynamicMemberReferenceOperation(Visit(operation.Instance), operation.MemberName, operation.TypeArguments,
                operation.ContainingType, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, int? captureIdForResult)
        {
            (IOperation visitedTarget, IOperation visitedValue) = VisitPreservingTupleOperations(operation.Target, operation.Value);
            return new DeconstructionAssignmentOperation(visitedTarget, visitedValue, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
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
                PushOperand(Visit(value));
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
                return new TupleOperation(elementBuilder.ToImmutableAndFree(), tuple.NaturalType, semanticModel: null, tuple.Syntax, tuple.Type, tuple.ConstantValue, IsImplicit(tuple));
            }
            else
            {
                return PopOperand();
            }
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, int? captureIdForResult)
        {
            return new DeclarationExpressionOperation(VisitPreservingTupleOperations(operation.Expression), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        private IOperation VisitPreservingTupleOperations(IOperation operation)
        {
            EvalStackFrame frame = PushStackFrame();
            PushTargetAndUnwrapTupleIfNecessary(operation);
            return PopStackFrame(frame, PopTargetAndWrapTupleIfNecessary(operation));
        }

        private (IOperation visitedLeft, IOperation visitedRight) VisitPreservingTupleOperations(IOperation left, IOperation right)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // If the left is a tuple, we want to decompose the tuple and push each element back onto the stack, so that if the right
            // has control flow the individual elements are captured. Then we can recompose the tuple after the right has been visited.
            // We do this to keep the graph sane, so that users don't have to track a tuple captured via flow control when it's not really
            // the tuple that's been captured, it's the operands to the tuple.
            EvalStackFrame frame = PushStackFrame();
            PushTargetAndUnwrapTupleIfNecessary(left);
            IOperation visitedRight = Visit(right);
            IOperation visitedLeft = PopTargetAndWrapTupleIfNecessary(left);
            PopStackFrame(frame);
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
                return VisitNoneOperationStatement(operation);
            }
            else
            {
                return VisitNoneOperationExpression(operation);
            }
        }

        private IOperation VisitNoneOperationStatement(IOperation operation)
        {
            Debug.Assert(_currentStatement == operation);
            VisitStatements(operation.Children.ToImmutableArray());
            return new NoneOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, operation.ConstantValue, IsImplicit(operation), operation.Type);
        }

        private IOperation VisitNoneOperationExpression(IOperation operation)
        {
            return PopStackFrame(PushStackFrame(),
                                 new NoneOperation(VisitArray(operation.Children.ToImmutableArray()), semanticModel: null, operation.Syntax, operation.ConstantValue, IsImplicit(operation), operation.Type));
        }

        public override IOperation VisitInterpolatedString(IInterpolatedStringOperation operation, int? captureIdForResult)
        {
            // We visit and rewrite the interpolation parts in two phases:
            //  1. Visit all the non-literal parts of the interpolation and push them onto the eval stack.
            //  2. Traverse the parts in reverse order, popping the non-literal values from the eval stack and visiting the literal values.
            EvalStackFrame frame = PushStackFrame();
            foreach (IInterpolatedStringContentOperation element in operation.Parts)
            {
                if (element.Kind == OperationKind.Interpolation)
                {
                    var interpolation = (IInterpolationOperation)element;
                    PushOperand(Visit(interpolation.Expression));

                    if (interpolation.Alignment != null)
                    {
                        PushOperand(Visit(interpolation.Alignment));
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

                    var rewrittenAlignment = interpolation.Alignment != null ? PopOperand() : null;
                    var rewrittenExpression = PopOperand();
                    rewrittenElement = new InterpolationOperation(rewrittenExpression, rewrittenAlignment, rewrittenFormatString, semanticModel: null, element.Syntax,
                                                         element.Type, element.ConstantValue, IsImplicit(element));
                }
                else
                {
                    var interpolatedStringText = (IInterpolatedStringTextOperation)element;
                    Debug.Assert(interpolatedStringText.Text.Kind == OperationKind.Literal);
                    var rewrittenInterpolationText = VisitLiteral((ILiteralOperation)interpolatedStringText.Text, captureIdForResult: null);
                    rewrittenElement = new InterpolatedStringTextOperation(rewrittenInterpolationText, semanticModel: null, element.Syntax, element.Type, element.ConstantValue, IsImplicit(element));
                }

                partsBuilder.Add(rewrittenElement);
            }

            partsBuilder.ReverseContents();
            PopStackFrame(frame);
            return new InterpolatedStringOperation(partsBuilder.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
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
            return new LiteralOperation(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, int? captureIdForResult)
        {
            return new LiteralOperation(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, int? captureIdForResult)
        {
            return new LocalReferenceOperation(operation.Local, operation.IsDeclaration, semanticModel: null, operation.Syntax,
                                                operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, int? captureIdForResult)
        {
            return new ParameterReferenceOperation(operation.Parameter, semanticModel: null, operation.Syntax,
                                                    operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Field.IsStatic ? null : Visit(operation.Instance);
            return new FieldReferenceOperation(operation.Field, operation.IsDeclaration, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Method.IsStatic ? null : Visit(operation.Instance);
            return new MethodReferenceOperation(operation.Method, operation.IsVirtual, visitedInstance, semanticModel: null,
                                                 operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, int? captureIdForResult)
        {
            // Check if this is an anonymous type property reference with an implicit receiver within an anonymous object initializer.
            if (operation.Instance is IInstanceReferenceOperation instanceReference &&
                instanceReference.ReferenceKind == InstanceReferenceKind.ImplicitReceiver &&
                operation.Property.ContainingType.IsAnonymousType &&
                operation.Property.ContainingType == _currentImplicitInstance.AnonymousType)
            {
                if (_currentImplicitInstance.AnonymousTypePropertyValues.TryGetValue(operation.Property, out IOperation captured))
                {
                    return captured is IFlowCaptureReferenceOperation reference ?
                               GetCaptureReference(reference.Id.Value, operation) :
                               OperationCloner.CloneOperation(captured);
                }
                else
                {
                    return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
                }
            }

            EvalStackFrame frame = PushStackFrame();
            IOperation instance = operation.Property.IsStatic ? null : operation.Instance;
            (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            PopStackFrame(frame);
            return new PropertyReferenceOperation(operation.Property, visitedArguments, visitedInstance, semanticModel: null,
                                                   operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, int? captureIdForResult)
        {
            IOperation visitedInstance = operation.Event.IsStatic ? null : Visit(operation.Instance);
            return new EventReferenceOperation(operation.Event, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, int? captureIdForResult)
        {
            return new TypeOfOperation(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, int? captureIdForResult)
        {
            return new ParenthesizedOperation(Visit(operation.Operand), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitAwait(IAwaitOperation operation, int? captureIdForResult)
        {
            return new AwaitOperation(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, int? captureIdForResult)
        {
            return new SizeOfOperation(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitStop(IStopOperation operation, int? captureIdForResult)
        {
            return new StopOperation(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, int? captureIdForResult)
        {
            return new IsTypeOperation(Visit(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitParameterInitializer(IParameterInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            var parameterRef = new ParameterReferenceOperation(operation.Parameter, semanticModel: null,
                operation.Syntax, operation.Parameter.Type, constantValue: default, isImplicit: true);
            VisitInitializer(rewrittenTarget: parameterRef, initializer: operation);
            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitFieldInitializer(IFieldInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            foreach (IFieldSymbol fieldSymbol in operation.InitializedFields)
            {
                IInstanceReferenceOperation instance = fieldSymbol.IsStatic ?
                    null :
                    new InstanceReferenceOperation(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
                        operation.Syntax, fieldSymbol.ContainingType, constantValue: default, isImplicit: true);
                var fieldRef = new FieldReferenceOperation(fieldSymbol, isDeclaration: false, instance, semanticModel: null,
                    operation.Syntax, fieldSymbol.Type, constantValue: default, isImplicit: true);
                VisitInitializer(rewrittenTarget: fieldRef, initializer: operation);
            }

            return FinishVisitingStatement(operation);
        }

        public override IOperation VisitPropertyInitializer(IPropertyInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            foreach (IPropertySymbol propertySymbol in operation.InitializedProperties)
            {
                var instance = propertySymbol.IsStatic ?
                    null :
                    new InstanceReferenceOperation(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
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

                IOperation propertyRef = new PropertyReferenceOperation(propertySymbol, arguments, instance,
                    semanticModel: null, operation.Syntax, propertySymbol.Type, constantValue: default, isImplicit: true);
                VisitInitializer(rewrittenTarget: propertyRef, initializer: operation);
            }

            return FinishVisitingStatement(operation);
        }

        private void VisitInitializer(IOperation rewrittenTarget, ISymbolInitializerOperation initializer)
        {
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: initializer.Locals));

            EvalStackFrame frame = PushStackFrame();
            var assignment = new SimpleAssignmentOperation(isRef: false, rewrittenTarget, Visit(initializer.Value), semanticModel: null,
                initializer.Syntax, rewrittenTarget.Type, constantValue: default, isImplicit: true);
            AddStatement(assignment);
            PopStackFrameAndLeaveRegion(frame);

            LeaveRegion();
        }

        public override IOperation VisitEventAssignment(IEventAssignmentOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
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
                    PushOperand(Visit(eventReferenceInstance));
                }

                visitedHandler = Visit(operation.HandlerValue);

                IOperation visitedInstance = eventReferenceInstance == null ? null : PopOperand();
                visitedEventReference = new EventReferenceOperation(eventReference.Event, visitedInstance,
                    semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, operation.EventReference.ConstantValue, IsImplicit(operation.EventReference));
            }
            else
            {
                Debug.Assert(operation.EventReference != null);

                PushOperand(Visit(operation.EventReference));
                visitedHandler = Visit(operation.HandlerValue);
                visitedEventReference = PopOperand();
            }

            PopStackFrame(frame);
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
            StartVisitingStatement(operation);

            EvalStackFrame frame = PushStackFrame();
            var instance = operation.EventReference.Event.IsStatic ? null : operation.EventReference.Instance;
            if (instance != null)
            {
                PushOperand(Visit(instance));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(operation.Arguments);
            IOperation visitedInstance = instance == null ? null : PopOperand();
            var visitedEventReference = new EventReferenceOperation(operation.EventReference.Event, visitedInstance,
                semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, operation.EventReference.ConstantValue, IsImplicit(operation.EventReference));

            PopStackFrame(frame);
            return FinishVisitingStatement(operation, new RaiseEventOperation(visitedEventReference, visitedArguments, semanticModel: null,
                                                                              operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation)));
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, int? captureIdForResult)
        {
            return new AddressOfOperation(Visit(operation.Reference), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, int? captureIdForResult)
        {
            return new IncrementOrDecrementOperation(operation.IsPostfix, operation.IsLifted, operation.IsChecked, Visit(operation.Target), operation.OperatorMethod,
                                                     operation.Kind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDiscardOperation(IDiscardOperation operation, int? captureIdForResult)
        {
            return new DiscardOperation(operation.DiscardSymbol, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDiscardPattern(IDiscardPatternOperation pat, int? captureIdForResult)
        {
            return new DiscardPatternOperation(pat.InputType, semanticModel: null, pat.Syntax, IsImplicit(pat));
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, int? captureIdForResult)
        {
            return new OmittedArgumentOperation(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
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

            Debug.Fail("All placeholders should be handled above. Have we introduced a new scenario where placeholders are used?");
            return new PlaceholderOperation(operation.PlaceholderKind, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitConversion(IConversionOperation operation, int? captureIdForResult)
        {
            return new ConversionOperation(Visit(operation.Operand), ((BaseConversionOperation)operation).ConversionConvertible, operation.IsTryCast, operation.IsChecked, semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, int? captureIdForResult)
        {
            return new DefaultValueOperation(semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(Visit(operation.Value));
            var visitedPattern = (IPatternOperation)Visit(operation.Pattern);
            IOperation visitedValue = PopOperand();
            PopStackFrame(frame);
            return new IsPatternOperation(visitedValue, visitedPattern, semanticModel: null,
                                           operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, int? captureIdForResult)
        {
            var children = ArrayBuilder<IOperation>.GetInstance();
            children.AddRange(operation.Children);

            if (children.Count != 0 && children.Last().Kind == OperationKind.ObjectOrCollectionInitializer)
            {
                // We are dealing with erroneous object creation. All children, but the last one are arguments for the constructor,
                // but overload resolution failed.
                SpillEvalStack();

                EvalStackFrame frame = PushStackFrame();
                var initializer = (IObjectOrCollectionInitializerOperation)children.Last();
                children.RemoveLast();

                EvalStackFrame argumentsFrame = PushStackFrame();

                foreach (var argument in children)
                {
                    PushOperand(Visit(argument));
                }

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    children[i] = PopOperand();
                }

                PopStackFrame(argumentsFrame);

                IOperation initializedInstance = new InvalidOperation(children.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type,
                                                                      operation.ConstantValue, IsImplicit(operation));

                initializedInstance = HandleObjectOrCollectionInitializer(initializer, initializedInstance);
                PopStackFrame(frame);
                return initializedInstance;
            }

            IOperation result;
            if (_currentStatement == operation)
            {
                result = visitInvalidOperationStatement(operation);
            }
            else
            {
                result = visitInvalidOperationExpression(operation);
            }

            return result;

            IOperation visitInvalidOperationStatement(IInvalidOperation invalidOperation)
            {
                Debug.Assert(_currentStatement == invalidOperation);
                VisitStatements(children.ToImmutableAndFree());
                return new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, invalidOperation.Syntax, invalidOperation.Type, invalidOperation.ConstantValue, IsImplicit(invalidOperation));
            }

            IOperation visitInvalidOperationExpression(IInvalidOperation invalidOperation)
            {
                return PopStackFrame(PushStackFrame(),
                                     new InvalidOperation(VisitArray(children.ToImmutableAndFree()), semanticModel: null,
                                                                 invalidOperation.Syntax, invalidOperation.Type, invalidOperation.ConstantValue, IsImplicit(operation)));
            }
        }

        public override IOperation VisitReDim(IReDimOperation operation, int? argument)
        {
            StartVisitingStatement(operation);

            // We split the ReDim clauses into separate ReDim operations to ensure that we preserve the evaluation order,
            // i.e. each ReDim clause operand is re-allocated prior to evaluating the next clause.

            // Mark the split ReDim operations as implicit if we have more than one ReDim clause.
            bool isImplicit = operation.Clauses.Length > 1 || IsImplicit(operation);

            foreach (var clause in operation.Clauses)
            {
                EvalStackFrame frame = PushStackFrame();
                var visitedReDimClause = visitReDimClause(clause);
                var visitedReDimOperation = new ReDimOperation(ImmutableArray.Create(visitedReDimClause), operation.Preserve,
                    semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, isImplicit);
                AddStatement(visitedReDimOperation);
                PopStackFrameAndLeaveRegion(frame);
            }

            return FinishVisitingStatement(operation);

            IReDimClauseOperation visitReDimClause(IReDimClauseOperation clause)
            {
                PushOperand(Visit(clause.Operand));
                var visitedDimensionSizes = VisitArray(clause.DimensionSizes);
                var visitedOperand = PopOperand();
                return new ReDimClauseOperation(visitedOperand, visitedDimensionSizes, semanticModel: null,
                    clause.Syntax, clause.Type, clause.ConstantValue, IsImplicit(clause));
            }
        }

        public override IOperation VisitReDimClause(IReDimClauseOperation operation, int? argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, int? captureIdForResult)
        {
            return new TranslatedQueryOperation(Visit(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, int? captureIdForResult)
        {
            return new ConstantPatternOperation(operation.InputType, Visit(operation.Value), semanticModel: null,
                syntax: operation.Syntax, isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitRelationalPattern(IRelationalPatternOperation operation, int? argument)
        {
            return new RelationalPatternOperation(
                operatorKind: operation.OperatorKind,
                value: Visit(operation.Value),
                inputType: operation.InputType,
                semanticModel: null,
                syntax: operation.Syntax,
                type: operation.Type,
                constantValue: default,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitBinaryPattern(IBinaryPatternOperation operation, int? argument)
        {
            return new BinaryPatternOperation(
                operatorKind: operation.OperatorKind,
                leftPattern: (IPatternOperation)Visit(operation.LeftPattern),
                rightPattern: (IPatternOperation)Visit(operation.RightPattern),
                inputType: operation.InputType,
                semanticModel: null,
                syntax: operation.Syntax,
                type: operation.Type,
                constantValue: default,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitNegatedPattern(INegatedPatternOperation operation, int? argument)
        {
            return new NegatedPatternOperation(
                negatedPattern: (IPatternOperation)Visit(operation.NegatedPattern),
                inputType: operation.InputType,
                semanticModel: null,
                syntax: operation.Syntax,
                type: operation.Type,
                constantValue: default,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitTypePattern(ITypePatternOperation operation, int? argument)
        {
            return new TypePatternOperation(
                matchedType: operation.MatchedType,
                inputType: operation.InputType,
                semanticModel: null,
                syntax: operation.Syntax,
                type: operation.Type,
                constantValue: default,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, int? captureIdForResult)
        {
            return new DeclarationPatternOperation(
                inputType: operation.InputType,
                matchedType: operation.MatchedType,
                operation.DeclaredSymbol,
                operation.MatchesNull,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitRecursivePattern(IRecursivePatternOperation operation, int? argument)
        {
            return new RecursivePatternOperation(
                inputType: operation.InputType,
                matchedType: operation.MatchedType,
                operation.DeconstructSymbol,
                operation.DeconstructionSubpatterns.SelectAsArray(p => (IPatternOperation)Visit(p)),
                operation.PropertySubpatterns.SelectAsArray(p => (IPropertySubpatternOperation)Visit(p)),
                operation.DeclaredSymbol,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitPropertySubpattern(IPropertySubpatternOperation operation, int? argument)
        {
            return new PropertySubpatternOperation(
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation),
                Visit(operation.Member),
                (IPatternOperation)Visit(operation.Pattern));
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, int? captureIdForResult)
        {
            return new DelegateCreationOperation(Visit(operation.Target), semanticModel: null,
                operation.Syntax, operation.Type, operation.ConstantValue, IsImplicit(operation));
        }

        public override IOperation VisitRangeOperation(IRangeOperation operation, int? argument)
        {
            if (operation.LeftOperand is object)
            {
                PushOperand(Visit(operation.LeftOperand));
            }

            IOperation visitedRightOperand = null;
            if (operation.RightOperand is object)
            {
                visitedRightOperand = Visit(operation.RightOperand);
            }

            IOperation visitedLeftOperand = operation.LeftOperand is null ? null : PopOperand();

            return new RangeOperation(operation.IsLifted, semanticModel: null, operation.Syntax, operation.Type, visitedLeftOperand, visitedRightOperand, operation.Method, isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitSwitchExpression(ISwitchExpressionOperation operation, int? captureIdForResult)
        {
            // expression switch { pat1 when g1 => e1, pat2 when g2 => e2 }
            //
            // becomes
            //
            // captureInput = expression
            // START scope 1 (arm1 locals)
            // GotoIfFalse (captureInput is pat1 && g1) label1;
            // captureOutput = e1
            // goto afterSwitch
            // label1:
            // END scope 1
            // START scope 2
            // GotoIfFalse (captureInput is pat2 && g2) label2;
            // captureOutput = e2
            // goto afterSwitch
            // label2:
            // END scope 2
            // throw new switch failure
            // afterSwitch:
            // result = captureOutput

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            SpillEvalStack();
            RegionBuilder resultCaptureRegion = _currentRegion;
            int captureOutput = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);
            var capturedInput = VisitAndCapture(operation.Value);
            var afterSwitch = new BasicBlockBuilder(BasicBlockKind.Block);

            foreach (var arm in operation.Arms)
            {
                // START scope (arm locals)
                var armScopeRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: arm.Locals);
                EnterRegion(armScopeRegion);
                var afterArm = new BasicBlockBuilder(BasicBlockKind.Block);

                // GotoIfFalse (captureInput is pat1) label;
                {
                    EvalStackFrame frame = PushStackFrame();
                    var visitedPattern = (IPatternOperation)Visit(arm.Pattern);
                    var patternTest = new IsPatternOperation(
                        OperationCloner.CloneOperation(capturedInput), visitedPattern, semanticModel: null,
                        arm.Syntax, booleanType, default, IsImplicit(arm));
                    ConditionalBranch(patternTest, jumpIfTrue: false, afterArm);
                    _currentBasicBlock = null;
                    PopStackFrameAndLeaveRegion(frame);
                }

                // GotoIfFalse (guard) afterArm;
                if (arm.Guard != null)
                {
                    EvalStackFrame frame = PushStackFrame();
                    VisitConditionalBranch(arm.Guard, ref afterArm, jumpIfTrue: false);
                    _currentBasicBlock = null;
                    PopStackFrameAndLeaveRegion(frame);
                }

                // captureOutput = e
                VisitAndCapture(arm.Value, captureOutput);

                // goto afterSwitch
                UnconditionalBranch(afterSwitch);

                // afterArm:
                AppendNewBlock(afterArm);

                // END scope 1
                LeaveRegion(); // armScopeRegion
            }

            LeaveRegionsUpTo(resultCaptureRegion);

            // throw new SwitchExpressionException
            var matchFailureCtor =
                (IMethodSymbol)(_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor) ??
                                _compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_InvalidOperationException__ctor))?.GetISymbol();
            var makeException = (matchFailureCtor is null)
                ? MakeInvalidOperation(operation.Syntax, type: _compilation.GetSpecialType(SpecialType.System_Object), ImmutableArray<IOperation>.Empty)
                : new ObjectCreationOperation(
                    matchFailureCtor, initializer: null, ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, operation.Syntax,
                    type: matchFailureCtor.ContainingType, constantValue: default, isImplicit: true);
            LinkThrowStatement(makeException);
            _currentBasicBlock = null;

            // afterSwitch:
            AppendNewBlock(afterSwitch, linkToPrevious: false);

            // result = captureOutput
            return GetCaptureReference(captureOutput, operation);
        }

        private void VisitUsingVariableDeclarationOperation(IUsingDeclarationOperation operation, ImmutableArray<IOperation> statements)
        {
            IOperation saveCurrentStatement = _currentStatement;
            _currentStatement = operation;
            StartVisitingStatement(operation);

            // a using statement introduces a 'logical' block after declaration, we synthesize one here in order to analyze it like a regular using 
            BlockOperation logicalBlock = new BlockOperation(
                operations: statements,
                locals: ImmutableArray<ILocalSymbol>.Empty,
                ((Operation)operation).OwningSemanticModel,
                operation.Syntax,
                operation.Type,
                operation.ConstantValue,
                isImplicit: true);

            HandleUsingOperationParts(
                resources: operation.DeclarationGroup,
                body: logicalBlock,
                locals: ImmutableArray<ILocalSymbol>.Empty,
                isAsynchronous: operation.IsAsynchronous);

            FinishVisitingStatement(operation);
            _currentStatement = saveCurrentStatement;
        }

        public IOperation Visit(IOperation operation)
        {
            // We should never be revisiting nodes we've already visited, and we don't set SemanticModel in this builder.
            Debug.Assert(operation == null || ((Operation)operation).OwningSemanticModel.Compilation == _compilation);
            return Visit(operation, argument: null);
        }

        public override IOperation Visit(IOperation operation, int? argument)
        {
            if (operation == null)
            {
                return null;
            }

            return PopStackFrame(PushStackFrame(), base.Visit(operation, argument));
        }

        public override IOperation DefaultVisit(IOperation operation, int? captureIdForResult)
        {
            // this should never reach, otherwise, there is missing override for IOperation type
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitArgument(IArgumentOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitUsingDeclaration(IUsingDeclarationOperation operation, int? argument)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IOperation VisitWith(IWithOperation operation, int? argument)
        {
            EvalStackFrame frame = PushStackFrame();
            // Initializer is removed from the tree and turned into a series of statements that assign to the cloned instance
            IOperation visitedInstance = Visit(operation.Value);

            IOperation cloned = operation.CloneMethod is null
                ? MakeInvalidOperation(visitedInstance.Type, visitedInstance)
                : new InvocationOperation(operation.CloneMethod, visitedInstance,
                    isVirtual: true, arguments: ImmutableArray<IArgumentOperation>.Empty,
                    semanticModel: null, operation.Syntax, operation.Type, operation.ConstantValue, isImplicit: true);

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, cloned));
        }
    }
}
