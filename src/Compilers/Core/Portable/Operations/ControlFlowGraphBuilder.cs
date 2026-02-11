// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Some basic concepts:
    /// - Basic blocks are sequences of statements/operations with no branching. The only branching
    ///   allowed is at the end of the basic block.
    /// - Regions group blocks together and represent the lifetime of locals and captures, loosely similar to scopes in C#.
    ///   There are different kinds of regions, <see cref="ControlFlowRegionKind"/>.
    /// - <see cref="ControlFlowGraphBuilder.SpillEvalStack"/> converts values on the stack into captures.
    /// - Error scenarios from initial binding need to be handled.
    /// </summary>
    internal sealed partial class ControlFlowGraphBuilder : OperationVisitor<int?, IOperation>
    {
        private readonly Compilation _compilation;
        private readonly BasicBlockBuilder _entry = new BasicBlockBuilder(BasicBlockKind.Entry);
        private readonly BasicBlockBuilder _exit = new BasicBlockBuilder(BasicBlockKind.Exit);

        private readonly ArrayBuilder<BasicBlockBuilder> _blocks;
        private readonly PooledDictionary<BasicBlockBuilder, RegionBuilder> _regionMap;
        private BasicBlockBuilder? _currentBasicBlock;
        private RegionBuilder? _currentRegion;
        private PooledDictionary<ILabelSymbol, BasicBlockBuilder>? _labeledBlocks;
        private bool _haveAnonymousFunction;

        private IOperation? _currentStatement;
        private readonly ArrayBuilder<(EvalStackFrame? frameOpt, IOperation? operationOpt)> _evalStack;
        private int _startSpillingAt;
        private ConditionalAccessOperationTracker _currentConditionalAccessTracker;
        private InterpolatedStringHandlerArgumentsContext? _currentInterpolatedStringHandlerArgumentContext;
        private InterpolatedStringHandlerCreationContext? _currentInterpolatedStringHandlerCreationContext;
        private IOperation? _currentSwitchOperationExpression;
        private IOperation? _forToLoopBinaryOperatorLeftOperand;
        private IOperation? _forToLoopBinaryOperatorRightOperand;
        private IOperation? _currentAggregationGroup;
        private bool _forceImplicit; // Force all rewritten nodes to be marked as implicit regardless of their original state.

        private readonly CaptureIdDispenser _captureIdDispenser;

        /// <summary>
        /// Holds the current object being initialized if we're visiting an object initializer.
        /// Or the current anonymous type object being initialized if we're visiting an anonymous type object initializer.
        /// Or the target of a VB With statement.
        /// </summary>
        private ImplicitInstanceInfo _currentImplicitInstance;

        private int _recursionDepth;

        private ControlFlowGraphBuilder(Compilation compilation, CaptureIdDispenser? captureIdDispenser, ArrayBuilder<BasicBlockBuilder> blocks)
        {
            Debug.Assert(compilation != null);
            _compilation = compilation;
            _captureIdDispenser = captureIdDispenser ?? new CaptureIdDispenser();
            _blocks = blocks;
            _regionMap = PooledDictionary<BasicBlockBuilder, RegionBuilder>.GetInstance();
            _evalStack = ArrayBuilder<(EvalStackFrame? frameOpt, IOperation? operationOpt)>.GetInstance();
        }

        private RegionBuilder CurrentRegionRequired
        {
            get
            {
                Debug.Assert(_currentRegion != null);
                return _currentRegion;
            }
        }

        private bool IsImplicit(IOperation operation)
        {
            return _forceImplicit || operation.IsImplicit;
        }

        public static ControlFlowGraph Create(IOperation body, ControlFlowGraph? parent = null, ControlFlowRegion? enclosing = null, CaptureIdDispenser? captureIdDispenser = null, in Context context = default)
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
                    body.Kind == OperationKind.ParameterInitializer ||
                    body.Kind == OperationKind.Attribute,
                    $"Unexpected root operation kind: {body.Kind}");
                Debug.Assert(parent == null);
            }
            else
            {
                Debug.Assert(body.Kind == OperationKind.LocalFunction || body.Kind == OperationKind.AnonymousFunction);
                Debug.Assert(parent != null);
            }
#endif

            var blocks = ArrayBuilder<BasicBlockBuilder>.GetInstance();
            var builder = new ControlFlowGraphBuilder(((Operation)body).OwningSemanticModel!.Compilation, captureIdDispenser, blocks);

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
            ImmutableDictionary<IFlowAnonymousFunctionOperation, (ControlFlowRegion, int)>.Builder? anonymousFunctionsMapOpt = null;

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
                ControlFlowBranch? successor = getFallThroughSuccessor(blockBuilder);
                ControlFlowBranch? conditionalSuccessor = getConditionalSuccessor(blockBuilder);
                builder[blockBuilder.Ordinal].SetSuccessors(successor, conditionalSuccessor);
            }

            // Pass 3: Set the predecessors for the created basic blocks.
            foreach (BasicBlockBuilder blockBuilder in blockBuilders)
            {
                builder[blockBuilder.Ordinal].SetPredecessors(blockBuilder.ConvertPredecessorsToBranches(builder));
            }

            return builder.ToImmutableAndFree();

            ControlFlowBranch? getFallThroughSuccessor(BasicBlockBuilder blockBuilder)
            {
                return blockBuilder.Kind != BasicBlockKind.Exit ?
                           getBranch(in blockBuilder.FallThrough, blockBuilder, isConditionalSuccessor: false) :
                           null;
            }

            ControlFlowBranch? getConditionalSuccessor(BasicBlockBuilder blockBuilder)
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
            ArrayBuilder<BasicBlockBuilder>? outOfRangeBlocksToVisit,
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
                    Debug.Assert(outOfRangeBlocksToVisit != null);
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
                    if (current.BranchValue.GetConstantValue() is { IsBoolean: true, BooleanValue: bool constant })
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
                Debug.Assert(current.Region != null);
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
                        Debug.Assert(current.Region != null);

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
                    Debug.Assert(region.EnclosingRegion != null);
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

            void dispatchException([DisallowNull] ControlFlowRegion? fromRegion)
            {
                do
                {
                    if (!dispatchedExceptionsFromRegions.Add(fromRegion))
                    {
                        return;
                    }

                    ControlFlowRegion? enclosing = fromRegion.Kind == ControlFlowRegionKind.Root ? null : fromRegion.EnclosingRegion;
                    if (fromRegion.Kind == ControlFlowRegionKind.Try)
                    {
                        switch (enclosing!.Kind)
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
                        Debug.Assert(enclosing!.Kind == ControlFlowRegionKind.FilterAndHandler);
                        Debug.Assert(enclosing.EnclosingRegion != null);
                        ControlFlowRegion tryAndCatch = enclosing.EnclosingRegion;
                        Debug.Assert(tryAndCatch.Kind == ControlFlowRegionKind.TryAndCatch);

                        int index = tryAndCatch.NestedRegions.IndexOf(enclosing, startIndex: 1);

                        if (index > 0)
                        {
                            dispatchExceptionThroughCatches(tryAndCatch, startAt: index + 1);
                            fromRegion = tryAndCatch;
                            continue;
                        }

                        throw ExceptionUtilities.Unreachable();
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
                                    Debug.Assert(subRegion.FirstBlock != null);
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
            Debug.Assert(subRegion.Enclosing != null);
            RegionBuilder enclosing = subRegion.Enclosing;

#if DEBUG
            subRegion.AboutToFree();
#endif

            if (subRegion.IsEmpty)
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
                    Debug.Assert(!r.IsEmpty);
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
            ArrayBuilder<RegionBuilder>? fromCurrent = null;
            ArrayBuilder<RegionBuilder>? fromDestination = null;
            ArrayBuilder<RegionBuilder>? fromPredecessor = null;
            ArrayBuilder<BasicBlockBuilder>? predecessorsBuilder = null;

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
                        BasicBlockBuilder? predecessor = block.GetSingletonPredecessorOrDefault();
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
                                Debug.Assert(currentRegion.Enclosing != null);
                                RegionBuilder tryAndFinally = currentRegion.Enclosing;
                                Debug.Assert(tryAndFinally.Kind == ControlFlowRegionKind.TryAndFinally);
                                Debug.Assert(tryAndFinally.Regions!.Count == 2);

                                RegionBuilder @try = tryAndFinally.Regions.First();
                                Debug.Assert(@try.Kind == ControlFlowRegionKind.Try);
                                Debug.Assert(tryAndFinally.Regions.Last() == currentRegion);

                                // If .try region has locals or methods or captures, let's convert it to .locals, otherwise drop it
                                if (@try.Locals.IsEmpty && !@try.HasLocalFunctions && !@try.HasCaptureIds)
                                {
                                    Debug.Assert(@try.FirstBlock != null);
                                    i = @try.FirstBlock.Ordinal - 1; // restart at the first block of removed .try region
                                    MergeSubRegionAndFree(@try, blocks, regionMap);
                                }
                                else
                                {
                                    @try.Kind = ControlFlowRegionKind.LocalLifetime;
                                    i--; // restart at the block that was following the tryAndFinally
                                }

                                MergeSubRegionAndFree(currentRegion, blocks, regionMap);

                                Debug.Assert(tryAndFinally.Enclosing != null);
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
                                BasicBlockBuilder? predecessor = block.GetSingletonPredecessorOrDefault();

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
                            IOperation? value = block.BranchValue;

                            RegionBuilder? implicitEntryRegion = tryGetImplicitEntryRegion(block, currentRegion);

                            if (implicitEntryRegion != null)
                            {
                                // First blocks in filter/catch/finally do not capture all possible predecessors
                                // Do not try to merge them, unless they are simply linked to the next block
                                if (value != null ||
                                    next.Destination != blocks[i + 1])
                                {
                                    continue;
                                }

                                Debug.Assert(implicitEntryRegion.LastBlock!.Ordinal >= next.Destination.Ordinal);
                            }

                            if (value != null)
                            {
                                if (!block.HasPredecessors && next.Kind == ControlFlowBranchSemantics.Return)
                                {
                                    // Let's drop an unreachable compiler generated return that VB optimistically adds at the end of a method body
                                    Debug.Assert(next.Destination != null);
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
                                    BasicBlockBuilder? predecessor = block.GetSingletonPredecessorOrDefault();
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
                            RegionBuilder? destinationRegionOpt = next.Destination == null ? null : regionMap[next.Destination];

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

                        BasicBlockBuilder? predecessor = block.GetSingletonPredecessorOrDefault();

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
                            BasicBlockBuilder? destination = block.Conditional.Destination;
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

            RegionBuilder? tryGetImplicitEntryRegion(BasicBlockBuilder block, [DisallowNull] RegionBuilder? currentRegion)
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
                Debug.Assert(!region.IsEmpty);
                Debug.Assert(region.FirstBlock.Ordinal >= 0);
                Debug.Assert(region.FirstBlock.Ordinal <= region.LastBlock.Ordinal);
                Debug.Assert(region.FirstBlock.Ordinal <= block.Ordinal);
                Debug.Assert(block.Ordinal <= region.LastBlock.Ordinal);

                if (region.FirstBlock == block)
                {
                    BasicBlockBuilder newFirst = blocks[block.Ordinal + 1];
                    region.FirstBlock = newFirst;
                    Debug.Assert(region.Enclosing != null);
                    RegionBuilder enclosing = region.Enclosing;
                    while (enclosing != null && enclosing.FirstBlock == block)
                    {
                        enclosing.FirstBlock = newFirst;
                        Debug.Assert(enclosing.Enclosing != null);
                        enclosing = enclosing.Enclosing;
                    }
                }
                else if (region.LastBlock == block)
                {
                    BasicBlockBuilder newLast = blocks[block.Ordinal - 1];
                    region.LastBlock = newLast;
                    Debug.Assert(region.Enclosing != null);
                    RegionBuilder enclosing = region.Enclosing;
                    while (enclosing != null && enclosing.LastBlock == block)
                    {
                        enclosing.LastBlock = newLast;
                        Debug.Assert(enclosing.Enclosing != null);
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

            bool checkBranchesFromPredecessors(ArrayBuilder<BasicBlockBuilder> predecessors, RegionBuilder currentRegion, RegionBuilder? destinationRegionOpt)
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

            void collectAncestorsAndSelf([DisallowNull] RegionBuilder? from, [NotNull] ref ArrayBuilder<RegionBuilder>? builder)
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
        private static void CheckUnresolvedBranches(ArrayBuilder<BasicBlockBuilder> blocks, PooledDictionary<ILabelSymbol, BasicBlockBuilder>? labeledBlocks)
        {
            if (labeledBlocks == null)
            {
                return;
            }

            PooledHashSet<BasicBlockBuilder>? unresolved = null;
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

        private void VisitStatement(IOperation? operation)
        {
#if DEBUG
            int stackDepth = _evalStack.Count;
            Debug.Assert(stackDepth == 0 || _evalStack.Peek().frameOpt != null);
#endif
            if (operation == null)
            {
                return;
            }

            IOperation? saveCurrentStatement = _currentStatement;
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
            IOperation? statement
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
                    || slot.operationOpt.Kind == OperationKind.OmittedArgument
                    || slot.operationOpt.Kind == OperationKind.CollectionExpressionElementsPlaceholder));
#endif
            if (statement == null)
            {
                return;
            }

            Operation.SetParentOperation(statement, null);
            CurrentBasicBlock.AddStatement(statement);
        }

        [MemberNotNull(nameof(_currentBasicBlock))]
        private void AppendNewBlock(BasicBlockBuilder block, bool linkToPrevious = true)
        {
            Debug.Assert(block != null);
            Debug.Assert(_currentRegion != null);

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
                throw ExceptionUtilities.Unreachable();
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
            Debug.Assert(_currentRegion != null);
            if (_currentRegion.IsEmpty)
            {
                AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));
            }

            RegionBuilder enclosed = _currentRegion;

#if DEBUG
            // We shouldn't be leaving regions that are still associated with stack frames
            foreach ((EvalStackFrame? frameOpt, IOperation? operationOpt) in _evalStack)
            {
                Debug.Assert((frameOpt == null) != (operationOpt == null));

                if (frameOpt != null)
                {
                    Debug.Assert(enclosed != frameOpt.RegionBuilderOpt);
                }
            }
#endif
            _currentRegion = _currentRegion.Enclosing;
            Debug.Assert(enclosed.LastBlock != null);
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

        public override IOperation? VisitBlock(IBlockOperation operation, int? captureIdForResult)
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

        [return: NotNullIfNotNull(nameof(result))]
        private IOperation? FinishVisitingStatement(IOperation originalOperation, IOperation? result = null)
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
        private bool VisitStatementsOneOrAll(IOperation? operation, ImmutableArray<IOperation> statements, int startIndex)
        {
            switch (operation)
            {
                case IUsingDeclarationOperation usingDeclarationOperation:
                    VisitUsingVariableDeclarationOperation(usingDeclarationOperation, statements.AsSpan()[(startIndex + 1)..]);
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

        internal override IOperation? VisitWithStatement(IWithStatementOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            ImplicitInstanceInfo previousInitializedInstance = _currentImplicitInstance;
            _currentImplicitInstance = new ImplicitInstanceInfo(VisitAndCapture(operation.Value));

            VisitStatement(operation.Body);

            _currentImplicitInstance = previousInitializedInstance;
            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitConstructorBodyOperation(IConstructorBodyOperation operation, int? captureIdForResult)
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

        public override IOperation? VisitMethodBodyOperation(IMethodBodyOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            Debug.Assert(captureIdForResult is null);
            VisitMethodBodyBaseOperation(operation);
            return FinishVisitingStatement(operation);
        }

        private void VisitMethodBodyBaseOperation(IMethodBodyBaseOperation operation)
        {
            Debug.Assert(_currentStatement == operation);
            VisitMethodBodies(operation.BlockBody, operation.ExpressionBody);
        }

        private void VisitMethodBodies(IBlockOperation? blockBody, IBlockOperation? expressionBody)
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

        public override IOperation? VisitConditional(IConditionalOperation operation, int? captureIdForResult)
        {
            if (operation == _currentStatement)
            {
                // if (condition)
                //   consequence;
                //
                // becomes
                //
                // GotoIfFalse condition afterif;
                // consequence;
                // afterif:

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

                var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);

                while (true)
                {
                    BasicBlockBuilder? whenFalse = null;
                    VisitConditionalBranch(operation.Condition, ref whenFalse, jumpIfTrue: false);
                    Debug.Assert(whenFalse is { });
                    VisitStatement(operation.WhenTrue);
                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    if (operation.WhenFalse is IConditionalOperation nested)
                    {
                        operation = nested;
                    }
                    else
                    {
                        break;
                    }
                }

                if (operation.WhenFalse is not null)
                {
                    VisitStatement(operation.WhenFalse);
                }

                AppendNewBlock(afterIf);

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

                Debug.Assert(operation is { WhenTrue: not null, WhenFalse: not null });

                SpillEvalStack();

                BasicBlockBuilder? whenFalse = null;
                VisitConditionalBranch(operation.Condition, ref whenFalse, jumpIfTrue: false);

                var afterIf = new BasicBlockBuilder(BasicBlockKind.Block);
                IOperation result;

                // Specially handle cases with "throw" as operation.WhenTrue or operation.WhenFalse. We don't need to create an additional
                // capture for the result because there won't be any result from the throwing branches.
                if (operation.WhenTrue is IConversionOperation whenTrueConversion && whenTrueConversion.Operand.Kind == OperationKind.Throw)
                {
                    IOperation? rewrittenThrow = BaseVisitRequired(whenTrueConversion.Operand, null);
                    Debug.Assert(rewrittenThrow!.Kind == OperationKind.None);
                    Debug.Assert(rewrittenThrow.ChildOperations.IsEmpty());

                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    result = VisitRequired(operation.WhenFalse);
                }
                else if (operation.WhenFalse is IConversionOperation whenFalseConversion && whenFalseConversion.Operand.Kind == OperationKind.Throw)
                {
                    result = VisitRequired(operation.WhenTrue);

                    UnconditionalBranch(afterIf);

                    AppendNewBlock(whenFalse);

                    IOperation rewrittenThrow = BaseVisitRequired(whenFalseConversion.Operand, null);
                    Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                    Debug.Assert(rewrittenThrow.ChildOperations.IsEmpty());
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
            IOperation result = BaseVisitRequired(operation, captureId);
            PopStackFrame(frame);
            CaptureResultIfNotAlready(operation.Syntax, captureId, result);
            LeaveRegionIfAny(frame);
        }

        private IOperation VisitAndCapture(IOperation operation)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(BaseVisitRequired(operation, null));
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
            private RegionBuilder? _lazyRegionBuilder;

            public RegionBuilder? RegionBuilderOpt
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

            (EvalStackFrame? frameOpt, IOperation? operationOpt) = _evalStack.Pop();
            Debug.Assert(frame == frameOpt);
            Debug.Assert(operationOpt == null);

            if (frame.RegionBuilderOpt != null && mergeNestedRegions)
            {
                while (_currentRegion != frame.RegionBuilderOpt)
                {
                    Debug.Assert(_currentRegion != null);
                    RegionBuilder toMerge = _currentRegion;
                    Debug.Assert(toMerge.Enclosing != null);
                    _currentRegion = toMerge.Enclosing;

                    Debug.Assert(toMerge.IsStackSpillRegion);
                    Debug.Assert(!toMerge.HasLocalFunctions);
                    Debug.Assert(toMerge.Locals.IsEmpty);

                    _currentRegion.AddCaptureIds(toMerge.CaptureIds);
                    // This region can be empty in certain error scenarios, such as `new T {}`, where T does not
                    // have a class constraint. There are no arguments or initializers, so nothing will have
                    // been put into the region at this point
                    if (!toMerge.IsEmpty)
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
            RegionBuilder? toLeave = frame.RegionBuilderOpt;
            if (toLeave != null)
            {
                while (_currentRegion != toLeave)
                {
                    Debug.Assert(_currentRegion!.IsStackSpillRegion);
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
                (EvalStackFrame? frameOpt, _) = _evalStack[i];
                if (frameOpt != null)
                {
                    currentFrameIndex = i;
                    Debug.Assert(frameOpt.RegionBuilderOpt != null);
                    break;
                }
            }

            for (int i = _startSpillingAt; i < _evalStack.Count; i++)
            {
                (EvalStackFrame? frameOpt, IOperation? operationOpt) = _evalStack[i];
                Debug.Assert((frameOpt == null) != (operationOpt == null));

                if (frameOpt != null)
                {
                    currentFrameIndex = i;
                    Debug.Assert(frameOpt.RegionBuilderOpt == null);
                    frameOpt.RegionBuilderOpt = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, isStackSpillRegion: true);
                    EnterRegion(frameOpt.RegionBuilderOpt, spillingStack: true);
                    continue;
                }

                Debug.Assert(operationOpt != null);

                // Declarations cannot have control flow, so we don't need to spill them.
                if (operationOpt.Kind != OperationKind.FlowCaptureReference
                    && operationOpt.Kind != OperationKind.DeclarationExpression
                    && operationOpt.Kind != OperationKind.Discard
                    && operationOpt.Kind != OperationKind.OmittedArgument
                    && operationOpt.Kind != OperationKind.CollectionExpressionElementsPlaceholder)
                {
                    // Here we need to decide what region should own the new capture. Due to the spilling operations occurred before,
                    // we currently might be in a region that is not associated with the stack frame we are in, but it is one of its
                    // directly or indirectly nested regions. The operation that we are about to spill is likely to remove references
                    // to some captures from the stack. That means that, after the spilling, we should be able to leave the spill
                    // regions that no longer own captures referenced on the stack. The new capture that we create, should belong to
                    // the region that will become current after that. Here we are trying to compute what will be that region.
                    // Obviously, we shouldn’t be leaving the region associated with the frame.
                    EvalStackFrame? currentFrame = _evalStack[currentFrameIndex].frameOpt;
                    Debug.Assert(currentFrame != null);
                    RegionBuilder? currentSpillRegion = currentFrame.RegionBuilderOpt;
                    Debug.Assert(currentSpillRegion != null);

                    if (_currentRegion != currentSpillRegion)
                    {
                        var idsStillOnTheStack = PooledHashSet<CaptureId>.GetInstance();

                        for (int j = currentFrameIndex + 1; j < _evalStack.Count; j++)
                        {
                            IOperation? operation = _evalStack[j].operationOpt;
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

                        RegionBuilder candidate = CurrentRegionRequired;
                        do
                        {
                            Debug.Assert(candidate.IsStackSpillRegion);
                            if (candidate.HasCaptureIds && candidate.CaptureIds.Any((id, set) => set.Contains(id), idsStillOnTheStack))
                            {
                                currentSpillRegion = candidate;
                                break;
                            }

                            Debug.Assert(candidate.Enclosing != null);
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
                        Debug.Assert(CurrentRegionRequired.IsStackSpillRegion);
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
                (EvalStackFrame? frameOpt, IOperation? operationOpt) = _evalStack[i];
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

            (EvalStackFrame? frameOpt, IOperation? operationOpt) = _evalStack.Pop();
            Debug.Assert(frameOpt == null);
            Debug.Assert(operationOpt != null);

            return operationOpt;
        }

        private IOperation PeekOperand()
        {
            Debug.Assert(_startSpillingAt <= _evalStack.Count);

            (EvalStackFrame? frameOpt, IOperation? operationOpt) = _evalStack.Peek();
            Debug.Assert(frameOpt == null);
            Debug.Assert(operationOpt != null);

            return operationOpt;
        }

        private void VisitAndPushArray<T>(ImmutableArray<T> array, Func<T, IOperation>? unwrapper = null) where T : IOperation
        {
            Debug.Assert(unwrapper != null || typeof(T) == typeof(IOperation));
            foreach (var element in array)
            {
                PushOperand(VisitRequired(unwrapper == null ? element : unwrapper(element)));
            }
        }

        private ImmutableArray<T> PopArray<T>(ImmutableArray<T> originalArray, Func<IOperation, int, ImmutableArray<T>, T>? wrapper = null) where T : IOperation
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

        private ImmutableArray<T> VisitArray<T>(ImmutableArray<T> originalArray, Func<T, IOperation>? unwrapper = null, Func<IOperation, int, ImmutableArray<T>, T>? wrapper = null) where T : IOperation
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

        private ImmutableArray<IArgumentOperation> VisitArguments(ImmutableArray<IArgumentOperation> arguments, bool instancePushed)
        {
            VisitAndPushArguments(arguments, instancePushed);

            var visitedArguments = PopArray(arguments, RewriteArgumentFromArray);
            return visitedArguments;
        }

        private void VisitAndPushArguments(ImmutableArray<IArgumentOperation> arguments, bool instancePushed)
        {
            var previousInterpolatedStringHandlerContext = _currentInterpolatedStringHandlerArgumentContext;

            ArrayBuilder<IInterpolatedStringHandlerCreationOperation>? interpolatedStringBuilder = null;
            int lastIndexForSpilling = -1;

            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].Value is IInterpolatedStringHandlerCreationOperation creation)
                {
                    lastIndexForSpilling = i;
                    interpolatedStringBuilder ??= ArrayBuilder<IInterpolatedStringHandlerCreationOperation>.GetInstance();
                    interpolatedStringBuilder.Add(creation);
                }
            }

            if (lastIndexForSpilling > -1)
            {
                Debug.Assert(interpolatedStringBuilder != null);
                _currentInterpolatedStringHandlerArgumentContext = new InterpolatedStringHandlerArgumentsContext(
                    interpolatedStringBuilder.ToImmutableAndFree(),
                    startingStackDepth: _evalStack.Count - (instancePushed ? 1 : 0),
                    hasReceiver: instancePushed);
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                // If there are declaration expressions in the arguments before an interpolated string handler, and that declaration
                // expression is referenced by the handler constructor, we need to spill it to ensure the declaration doesn't end
                // up in the tree twice. However, we don't want to generally introduce spilling for these declarations: that could
                // have unexpected affects on consumers. So we limit the spilling to those indexes before the last interpolated string
                // handler. We _could_ limit this further by only spilling declaration expressions if the handler in question actually
                // referenced a specific declaration expression in the argument list, but we think that the difficulty in implementing
                // this check is more complexity than this scenario needs.
                var argument = arguments[i].Value switch
                {
                    IDeclarationExpressionOperation declaration when i < lastIndexForSpilling => declaration.Expression,
                    var value => value
                };

                PushOperand(VisitRequired(argument));
            }

            _currentInterpolatedStringHandlerArgumentContext = previousInterpolatedStringHandlerContext;
        }

        private IArgumentOperation RewriteArgumentFromArray(IOperation visitedArgument, int index, ImmutableArray<IArgumentOperation> args)
        {
            Debug.Assert(index >= 0 && index < args.Length);
            var originalArgument = (ArgumentOperation)args[index];
            return new ArgumentOperation(originalArgument.ArgumentKind, originalArgument.Parameter, visitedArgument,
                                         originalArgument.InConversionConvertible, originalArgument.OutConversionConvertible,
                                         semanticModel: null, originalArgument.Syntax, IsImplicit(originalArgument));
        }

        public override IOperation VisitSimpleAssignment(ISimpleAssignmentOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(VisitRequired(operation.Target));
            IOperation value = VisitRequired(operation.Value);
            return PopStackFrame(frame, new SimpleAssignmentOperation(operation.IsRef, PopOperand(), value, null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation)));
        }

        public override IOperation VisitCompoundAssignment(ICompoundAssignmentOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            var compoundAssignment = (CompoundAssignmentOperation)operation;
            PushOperand(VisitRequired(compoundAssignment.Target));
            IOperation value = VisitRequired(compoundAssignment.Value);

            return PopStackFrame(frame, new CompoundAssignmentOperation(compoundAssignment.InConversionConvertible, compoundAssignment.OutConversionConvertible, operation.OperatorKind, operation.IsLifted,
                operation.IsChecked, operation.OperatorMethod, operation.ConstrainedToType, PopOperand(), value, semanticModel: null,
                syntax: operation.Syntax, type: operation.Type, isImplicit: IsImplicit(operation)));
        }

        public override IOperation VisitArrayElementReference(IArrayElementReferenceOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(VisitRequired(operation.ArrayReference));
            ImmutableArray<IOperation> visitedIndices = VisitArray(operation.Indices);
            IOperation visitedArrayReference = PopOperand();
            PopStackFrame(frame);
            return new ArrayElementReferenceOperation(visitedArrayReference, visitedIndices, semanticModel: null,
                operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitImplicitIndexerReference(IImplicitIndexerReferenceOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(VisitRequired(operation.Instance));
            IOperation argument = VisitRequired(operation.Argument);
            IOperation instance = PopOperand();
            PopStackFrame(frame);
            return new ImplicitIndexerReferenceOperation(instance, argument, operation.LengthSymbol, operation.IndexerSymbol, semanticModel: null,
                operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation? VisitInlineArrayAccess(IInlineArrayAccessOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(VisitRequired(operation.Instance));
            IOperation argument = VisitRequired(operation.Argument);
            IOperation instance = PopOperand();
            PopStackFrame(frame);
            return new InlineArrayAccessOperation(instance, argument, semanticModel: null,
                operation.Syntax, operation.Type, IsImplicit(operation));
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
                        return VisitObjectBinaryConditionalOperator(operation);
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

            var stack = ArrayBuilder<(IBinaryOperation, EvalStackFrame)>.GetInstance();
            IOperation leftOperand;

            while (true)
            {
                stack.Push((operation, PushStackFrame()));
                leftOperand = operation.LeftOperand;

                if (leftOperand is not IBinaryOperation binary || IsConditional(binary))
                {
                    break;
                }

                operation = binary;
            }

            leftOperand = VisitRequired(leftOperand);

            do
            {
                EvalStackFrame frame;
                (operation, frame) = stack.Pop();

                PushOperand(leftOperand);
                IOperation rightOperand = VisitRequired(operation.RightOperand);

                leftOperand = PopStackFrame(frame, new BinaryOperation(operation.OperatorKind, PopOperand(), rightOperand, operation.IsLifted, operation.IsChecked, operation.IsCompareText,
                                                                       operation.OperatorMethod, operation.ConstrainedToType, ((BinaryOperation)operation).UnaryOperatorMethod,
                                                                       semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation)));

            }
            while (stack.Count != 0);

            stack.Free();

            return leftOperand;
        }

        public override IOperation VisitTupleBinaryOperator(ITupleBinaryOperation operation, int? captureIdForResult)
        {
            (IOperation visitedLeft, IOperation visitedRight) = VisitPreservingTupleOperations(operation.LeftOperand, operation.RightOperand);
            return new TupleBinaryOperation(operation.OperatorKind, visitedLeft, visitedRight,
                semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitUnaryOperator(IUnaryOperation operation, int? captureIdForResult)
        {
            if (IsBooleanLogicalNot(operation))
            {
                return VisitConditionalExpression(operation, sense: true, captureIdForResult, fallToTrueOpt: null, fallToFalseOpt: null);
            }

            return new UnaryOperation(operation.OperatorKind, VisitRequired(operation.Operand), operation.IsLifted, operation.IsChecked,
                                      operation.OperatorMethod, operation.ConstrainedToType,
                                      semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
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
                                                          BasicBlockBuilder? fallToTrueOpt, BasicBlockBuilder? fallToFalseOpt)
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

            var resultCaptureRegion = CurrentRegionRequired;

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
                return new UnaryOperation(UnaryOperatorKind.Not, operand, isLifted: true, isChecked: false,
                                          operatorMethod: null, constrainedToType: null,
                                          semanticModel: null, operand.Syntax, operand.Type, constantValue: null, isImplicit: true);

            }
        }

        private IOperation VisitObjectBinaryConditionalOperator(IBinaryOperation binOp)
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

            condition = CreateConversion(VisitRequired(left), booleanType);

            ConditionalBranch(condition, jumpIfTrue: isAndAlso, checkRight);
            _currentBasicBlock = null;

            PopStackFrameAndLeaveRegion(frame);

            var resultCaptureRegion = CurrentRegionRequired;

            int resultId = GetNextCaptureId(resultCaptureRegion);
            ConstantValue constantValue = ConstantValue.Create(!isAndAlso);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, new LiteralOperation(semanticModel: null, left.Syntax, booleanType, constantValue, isImplicit: true)));
            UnconditionalBranch(done);

            AppendNewBlock(checkRight);

            frame = PushStackFrame();

            condition = CreateConversion(VisitRequired(right), booleanType);

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax, condition));

            PopStackFrame(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            condition = new FlowCaptureReferenceOperation(resultId, binOp.Syntax, booleanType, constantValue: null);
            Debug.Assert(binOp.Type is not null);
            return new ConversionOperation(condition, _compilation.ClassifyConvertibleConversion(condition, binOp.Type, out _), isTryCast: false, isChecked: false,
                                           semanticModel: null, binOp.Syntax, binOp.Type, binOp.GetConstantValue(), isImplicit: true);
        }

        private IOperation CreateConversion(IOperation operand, ITypeSymbol type)
        {
            return new ConversionOperation(operand, _compilation.ClassifyConvertibleConversion(operand, type, out ConstantValue? constantValue), isTryCast: false, isChecked: false,
                                           semanticModel: null, operand.Syntax, type, constantValue, isImplicit: true);
        }

        private IOperation VisitDynamicBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            SpillEvalStack();
            Debug.Assert(binOp.Type is not null);

            var resultCaptureRegion = CurrentRegionRequired;

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol? unaryOperatorMethod = ((BinaryOperation)binOp).UnaryOperatorMethod;
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
                                                   condition, isLifted: false, isChecked: false,
                                                   operatorMethod: unaryOperatorMethod,
                                                   constrainedToType: unaryOperatorMethod is not null && (unaryOperatorMethod.IsAbstract || unaryOperatorMethod.IsVirtual) ? binOp.ConstrainedToType : null,
                                                   semanticModel: null, condition.Syntax, booleanType, constantValue: null, isImplicit: true);
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
            IOperation visitedRight = VisitRequired(right);
            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax,
                                         new BinaryOperation(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                             PopOperand(),
                                                             visitedRight,
                                                             isLifted: false,
                                                             binOp.IsChecked,
                                                             binOp.IsCompareText,
                                                             binOp.OperatorMethod,
                                                             binOp.OperatorMethod is not null && (binOp.OperatorMethod.IsAbstract || binOp.OperatorMethod.IsVirtual) ? binOp.ConstrainedToType : null,
                                                             unaryOperatorMethod: null,
                                                             semanticModel: null,
                                                             binOp.Syntax,
                                                             binOp.Type,
                                                             binOp.GetConstantValue(),
                                                             IsImplicit(binOp))));

            PopStackFrameAndLeaveRegion(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);
        }

        private IOperation VisitUserDefinedBinaryConditionalOperator(IBinaryOperation binOp, int? captureIdForResult)
        {
            Debug.Assert(binOp.OperatorMethod is not null);

            SpillEvalStack();

            var resultCaptureRegion = CurrentRegionRequired;

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            bool isLifted = binOp.IsLifted;
            IOperation left = binOp.LeftOperand;
            IOperation right = binOp.RightOperand;
            IMethodSymbol? unaryOperatorMethod = ((BinaryOperation)binOp).UnaryOperatorMethod;
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
                                               condition, isLifted: false, isChecked: false,
                                               operatorMethod: unaryOperatorMethod,
                                               constrainedToType: unaryOperatorMethod.IsAbstract || unaryOperatorMethod.IsVirtual ? binOp.ConstrainedToType : null,
                                               semanticModel: null, condition.Syntax, unaryOperatorMethod.ReturnType, constantValue: null, isImplicit: true);
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
            IOperation visitedRight = VisitRequired(right);

            AddStatement(new FlowCaptureOperation(resultId, binOp.Syntax,
                                         new BinaryOperation(isAndAlso ? BinaryOperatorKind.And : BinaryOperatorKind.Or,
                                                             PopOperand(),
                                                             visitedRight,
                                                             isLifted,
                                                             binOp.IsChecked,
                                                             binOp.IsCompareText,
                                                             binOp.OperatorMethod,
                                                             binOp.OperatorMethod.IsAbstract || binOp.OperatorMethod.IsVirtual ? binOp.ConstrainedToType : null,
                                                             unaryOperatorMethod: null,
                                                             semanticModel: null,
                                                             binOp.Syntax,
                                                             binOp.Type,
                                                             binOp.GetConstantValue(), IsImplicit(binOp))));

            PopStackFrameAndLeaveRegion(frame);
            LeaveRegionsUpTo(resultCaptureRegion);

            AppendNewBlock(done);

            return GetCaptureReference(resultId, binOp);
        }

        private IOperation VisitShortCircuitingOperator(IBinaryOperation condition, bool sense, bool stopSense, bool stopValue,
                                                        int? captureIdForResult, BasicBlockBuilder? fallToTrueOpt, BasicBlockBuilder? fallToFalseOpt)
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

            ref BasicBlockBuilder? lazyFallThrough = ref stopValue ? ref fallToTrueOpt : ref fallToFalseOpt;
            bool newFallThroughBlock = (lazyFallThrough == null);

            VisitConditionalBranch(condition.LeftOperand, ref lazyFallThrough, stopSense);

            var resultCaptureRegion = CurrentRegionRequired;
            int captureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

            IOperation resultFromRight = VisitConditionalExpression(condition.RightOperand, sense, captureId, fallToTrueOpt, fallToFalseOpt);

            CaptureResultIfNotAlready(condition.RightOperand.Syntax, captureId, resultFromRight);

            LeaveRegionsUpTo(resultCaptureRegion);

            if (newFallThroughBlock)
            {
                var labEnd = new BasicBlockBuilder(BasicBlockKind.Block);
                UnconditionalBranch(labEnd);

                AppendNewBlock(lazyFallThrough);

                var constantValue = ConstantValue.Create(stopValue);
                SyntaxNode leftSyntax = (lazyFallThrough.GetSingletonPredecessorOrDefault() != null ? condition.LeftOperand : condition).Syntax;
                AddStatement(new FlowCaptureOperation(captureId, leftSyntax, new LiteralOperation(semanticModel: null, leftSyntax, condition.Type, constantValue, isImplicit: true)));

                AppendNewBlock(labEnd);
            }

            return GetCaptureReference(captureId, condition);
        }

        private IOperation VisitConditionalExpression(IOperation condition, bool sense, int? captureIdForResult, BasicBlockBuilder? fallToTrueOpt, BasicBlockBuilder? fallToFalseOpt)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(condition.Type));

            IUnaryOperation? lastUnary = null;

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

            condition = VisitRequired(condition);
            if (!sense)
            {
                return lastUnary != null
                    ? new UnaryOperation(lastUnary.OperatorKind, condition, lastUnary.IsLifted, lastUnary.IsChecked,
                                         lastUnary.OperatorMethod, lastUnary.ConstrainedToType, semanticModel: null, lastUnary.Syntax,
                                         lastUnary.Type, lastUnary.GetConstantValue(), IsImplicit(lastUnary))
                    : new UnaryOperation(UnaryOperatorKind.Not, condition, isLifted: false, isChecked: false,
                                         operatorMethod: null, constrainedToType: null, semanticModel: null, condition.Syntax,
                                         condition.Type, constantValue: null, isImplicit: true);
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

        private void VisitConditionalBranch(IOperation condition, [NotNull] ref BasicBlockBuilder? dest, bool jumpIfTrue)
        {
            SpillEvalStack();
#if DEBUG
            RegionBuilder? current = _currentRegion;
#endif
            VisitConditionalBranchCore(condition, ref dest, jumpIfTrue);
#if DEBUG
            Debug.Assert(current == _currentRegion);
#endif
        }

        /// <summary>
        /// This function does not change the current region. The stack should be spilled before calling it.
        /// </summary>
        private void VisitConditionalBranchCore(IOperation condition, [NotNull] ref BasicBlockBuilder? dest, bool jumpIfTrue)
        {
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            _recursionDepth++;
            visitConditionalBranchCore(condition, ref dest, jumpIfTrue);
            _recursionDepth--;

            void visitConditionalBranchCore(IOperation condition, [NotNull] ref BasicBlockBuilder? dest, bool jumpIfTrue)
            {
oneMoreTime:
                Debug.Assert(_startSpillingAt == _evalStack.Count);

                condition = skipParenthesized(condition);

                switch (condition.Kind)
                {
                    case OperationKind.Binary:

                        if (IsBooleanConditionalOperator((IBinaryOperation)condition))
                        {
                            dest ??= new BasicBlockBuilder(BasicBlockKind.Block);
                            var stack = ArrayBuilder<(IOperation? condition, BasicBlockBuilder dest, bool jumpIfTrue)>.GetInstance();
                            stack.Push((condition, dest, jumpIfTrue));

                            do
                            {
                                (IOperation? condition, BasicBlockBuilder dest, bool jumpIfTrue) top = stack.Pop();

                                if (top.condition is null)
                                {
                                    // This is a special entry to indicate that it is time to append the block
                                    AppendNewBlock(top.dest);
                                }
                                else if (top.condition is IBinaryOperation binOp && IsBooleanConditionalOperator(binOp))
                                {
                                    if (CalculateAndOrSense(binOp, top.jumpIfTrue))
                                    {
                                        // gotoif(LeftOperand != sense) fallThrough
                                        // gotoif(RightOperand == sense) dest
                                        // fallThrough:

                                        BasicBlockBuilder? fallThrough = new BasicBlockBuilder(BasicBlockKind.Block);

                                        // Note, operations are pushed to the stack in opposite order
                                        stack.Push((null, fallThrough, true)); // This is a special entry to indicate that it is time to append the fallThrough block
                                        stack.Push((skipParenthesized(binOp.RightOperand), top.dest, top.jumpIfTrue));
                                        stack.Push((skipParenthesized(binOp.LeftOperand), fallThrough, !top.jumpIfTrue));
                                    }
                                    else
                                    {
                                        // gotoif(LeftOperand == sense) dest
                                        // gotoif(RightOperand == sense) dest

                                        // Note, operations are pushed to the stack in opposite order
                                        stack.Push((skipParenthesized(binOp.RightOperand), top.dest, top.jumpIfTrue));
                                        stack.Push((skipParenthesized(binOp.LeftOperand), top.dest, top.jumpIfTrue));
                                    }
                                }
                                else if (stack.Count == 0 && ReferenceEquals(dest, top.dest))
                                {
                                    // Instead of recursion we can restart from the top with new condition
                                    condition = top.condition;
                                    jumpIfTrue = top.jumpIfTrue;
                                    stack.Free();
                                    goto oneMoreTime;
                                }
                                else
                                {
                                    VisitConditionalBranchCore(top.condition, ref top.dest, top.jumpIfTrue);
                                }
                            }
                            while (stack.Count != 0);

                            stack.Free();
                            return;
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

                            Debug.Assert(conditional.WhenFalse is not null);
                            if (ITypeSymbolHelpers.IsBooleanType(conditional.WhenTrue.Type) &&
                                ITypeSymbolHelpers.IsBooleanType(conditional.WhenFalse.Type))
                            {
                                BasicBlockBuilder? whenFalse = null;
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

                                dest ??= new BasicBlockBuilder(BasicBlockKind.Block);
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
                            IOperation? rewrittenThrow = BaseVisitRequired(conversion.Operand, null);
                            Debug.Assert(rewrittenThrow != null);
                            Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                            Debug.Assert(rewrittenThrow.ChildOperations.IsEmpty());
                            dest ??= new BasicBlockBuilder(BasicBlockKind.Block);
                            return;
                        }
                        goto default;

                    default:
                        {
                            EvalStackFrame frame = PushStackFrame();

                            condition = VisitRequired(condition);
                            dest ??= new BasicBlockBuilder(BasicBlockKind.Block);
                            ConditionalBranch(condition, jumpIfTrue, dest);
                            _currentBasicBlock = null;

                            PopStackFrameAndLeaveRegion(frame);
                            return;
                        }
                }

                static IOperation skipParenthesized(IOperation condition)
                {
                    while (condition.Kind == OperationKind.Parenthesized)
                    {
                        condition = ((IParenthesizedOperation)condition).Operand;
                    }

                    return condition;
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
            Debug.Assert(branch.Destination != null);
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
            ITypeSymbol? valueTypeOpt = operationValue.Type;

            PushOperand(VisitRequired(operationValue));
            SpillEvalStack();
            IOperation testExpression = PopOperand();

            ConditionalBranch(MakeIsNullOperation(testExpression),
                jumpIfTrue: true,
                whenNull);
            _currentBasicBlock = null;

            CommonConversion testConversion = operation.ValueConversion;
            IOperation capturedValue = OperationCloner.CloneOperation(testExpression);
            IOperation? convertedTestExpression = null;

            if (testConversion.Exists)
            {
                IOperation? possiblyUnwrappedValue;

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
                        convertedTestExpression = new ConversionOperation(possiblyUnwrappedValue, ((CoalesceOperation)operation).ValueConversionConvertible,
                                                                          isTryCast: false, isChecked: false, semanticModel: null, valueSyntax, operation.Type,
                                                                          constantValue: null, isImplicit: true);
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

            RegionBuilder resultCaptureRegion = CurrentRegionRequired;

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

                Debug.Assert(conversion is not null);
                IOperation? rewrittenThrow = BaseVisitRequired(conversion.Operand, null);
                Debug.Assert(rewrittenThrow != null);
                Debug.Assert(rewrittenThrow.Kind == OperationKind.None);
                Debug.Assert(rewrittenThrow.ChildOperations.IsEmpty());
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

        public override IOperation? VisitCoalesceAssignment(ICoalesceAssignmentOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            // If we're in a statement context, we elide the capture of the result of the assignment, as it will
            // just be wrapped in an expression statement that isn't used anywhere and isn't observed by anything.
            Debug.Assert(operation.Parent != null);
            bool isStatement = _currentStatement == operation || operation.Parent.Kind == OperationKind.ExpressionStatement;
            Debug.Assert(captureIdForResult == null || !isStatement);

            RegionBuilder resultCaptureRegion = CurrentRegionRequired;

            EvalStackFrame frame = PushStackFrame();

            PushOperand(VisitRequired(operation.Target));
            SpillEvalStack();
            IOperation locationCapture = PopOperand();

            // Capture the value, as it will only be evaluated once. The location will be used separately later for
            // the null case
            EvalStackFrame valueFrame = PushStackFrame();
            SpillEvalStack();
            Debug.Assert(valueFrame.RegionBuilderOpt != null);
            int valueCaptureId = GetNextCaptureId(valueFrame.RegionBuilderOpt);
            AddStatement(new FlowCaptureOperation(valueCaptureId, locationCapture.Syntax, locationCapture));
            IOperation valueCapture = GetCaptureReference(valueCaptureId, locationCapture);

            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
            var afterCoalesce = new BasicBlockBuilder(BasicBlockKind.Block);

            int resultCaptureId = isStatement ? -1 : captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

            if (operation.Target?.Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                ((INamedTypeSymbol)operation.Target.Type!).TypeArguments[0].Equals(operation.Type))
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
                EvalStackFrame? intermediateFrame = null;

                if (!isStatement)
                {
                    intermediateFrame = PushStackFrame();
                    SpillEvalStack();
                    Debug.Assert(intermediateFrame.RegionBuilderOpt != null);
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
                    Debug.Assert(intermediateFrame != null);
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

                IOperation whenNullValue = VisitRequired(operation.Value);
                if (!isStatement)
                {
                    Debug.Assert(whenNullFrame.RegionBuilderOpt != null);
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
                        constantValue: operation.GetConstantValue(),
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

                IOperation whenNullValue = VisitRequired(operation.Value);
                IOperation whenNullAssignment = new SimpleAssignmentOperation(isRef: false, OperationCloner.CloneOperation(locationCapture), whenNullValue, semanticModel: null,
                    operation.Syntax, operation.Type, constantValue: operation.GetConstantValue(), isImplicit: true);

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

        private static IOperation MakeInvalidOperation(ITypeSymbol? type, IOperation child)
        {
            return new InvalidOperation(ImmutableArray.Create<IOperation>(child),
                                        semanticModel: null, child.Syntax, type,
                                        constantValue: null, isImplicit: true);
        }

        private static IOperation MakeInvalidOperation(SyntaxNode syntax, ITypeSymbol? type, IOperation child1, IOperation child2)
        {
            return MakeInvalidOperation(syntax, type, ImmutableArray.Create<IOperation>(child1, child2));
        }

        private static IOperation MakeInvalidOperation(SyntaxNode syntax, ITypeSymbol? type, ImmutableArray<IOperation> children)
        {
            return new InvalidOperation(children,
                                        semanticModel: null, syntax, type,
                                        constantValue: null, isImplicit: true);
        }

        private IsNullOperation MakeIsNullOperation(IOperation operand)
        {
            return MakeIsNullOperation(operand, _compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private static IsNullOperation MakeIsNullOperation(IOperation operand, ITypeSymbol booleanType)
        {
            Debug.Assert(ITypeSymbolHelpers.IsBooleanType(booleanType));
            ConstantValue? constantValue = operand.GetConstantValue() is { IsNull: var isNull }
                ? ConstantValue.Create(isNull)
                : null;
            return new IsNullOperation(operand.Syntax, operand,
                                       booleanType,
                                       constantValue);
        }

        private IOperation? TryCallNullableMember(IOperation value, SpecialMember nullableMember)
        {
            Debug.Assert(nullableMember == SpecialMember.System_Nullable_T_GetValueOrDefault ||
                         nullableMember == SpecialMember.System_Nullable_T_get_HasValue ||
                         nullableMember == SpecialMember.System_Nullable_T_get_Value ||
                         nullableMember == SpecialMember.System_Nullable_T__op_Explicit_ToT ||
                         nullableMember == SpecialMember.System_Nullable_T__op_Implicit_FromT);
            ITypeSymbol? valueType = value.Type;

            Debug.Assert(ITypeSymbolHelpers.IsNullableType(valueType));

            var method = (IMethodSymbol?)_compilation.CommonGetSpecialTypeMember(nullableMember)?.GetISymbol();

            if (method != null)
            {
                foreach (ISymbol candidate in valueType.GetMembers(method.Name))
                {
                    if (candidate.OriginalDefinition.Equals(method))
                    {
                        method = (IMethodSymbol)candidate;
                        return new InvocationOperation(method, constrainedToType: null, value, isVirtual: false,
                                                       ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, value.Syntax,
                                                       method.ReturnType, isImplicit: true);
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

        public override IOperation? VisitConditionalAccess(IConditionalAccessOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            RegionBuilder resultCaptureRegion = CurrentRegionRequired;

            // Avoid creation of default values and FlowCapture for conditional access on a statement level.
            bool isOnStatementLevel = _currentStatement == operation || (_currentStatement == operation.Parent && _currentStatement?.Kind == OperationKind.ExpressionStatement);
            EvalStackFrame? expressionFrame = null;
            var operations = ArrayBuilder<IOperation>.GetInstance();

            if (!isOnStatementLevel)
            {
                expressionFrame = PushStackFrame();
            }

            IConditionalAccessOperation currentConditionalAccess = operation;
            IOperation testExpression;
            var whenNull = new BasicBlockBuilder(BasicBlockKind.Block);
            var previousTracker = _currentConditionalAccessTracker;
            _currentConditionalAccessTracker = new ConditionalAccessOperationTracker(operations, whenNull);

            while (true)
            {
                testExpression = currentConditionalAccess.Operation;
                if (!isConditionalAccessInstancePresentInChildren(currentConditionalAccess.WhenNotNull))
                {
                    // https://github.com/dotnet/roslyn/issues/27564: It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
                    //                      a None operation is created and all children are dropped.
                    //                      See Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests.ExpressionCompilerTests.ConditionalAccessExpressionType
                    //                      Because of this, the recursion to visit the child operations will never occur if we visit the WhenNull of the current
                    //                      conditional access, so we need to manually visit the Operation of the conditional access now.
                    _ = VisitConditionalAccessTestExpression(testExpression);
                    break;
                }

                operations.Push(testExpression);

                if (currentConditionalAccess.WhenNotNull is not IConditionalAccessOperation nested)
                {
                    break;
                }

                currentConditionalAccess = nested;
            }

            if (isOnStatementLevel)
            {
                Debug.Assert(captureIdForResult == null);

                IOperation result = VisitRequired(currentConditionalAccess.WhenNotNull);
                resetConditionalAccessTracker();

                if (_currentStatement != operation)
                {
                    Debug.Assert(_currentStatement is not null);
                    var expressionStatement = (IExpressionStatementOperation)_currentStatement;
                    result = new ExpressionStatementOperation(result, semanticModel: null, expressionStatement.Syntax,
                                                              IsImplicit(expressionStatement));
                }

                AddStatement(result);
                AppendNewBlock(whenNull);
                return null;
            }
            else
            {
                Debug.Assert(expressionFrame != null);
                int resultCaptureId = captureIdForResult ?? GetNextCaptureId(resultCaptureRegion);

                if (ITypeSymbolHelpers.IsNullableType(operation.Type) && !ITypeSymbolHelpers.IsNullableType(currentConditionalAccess.WhenNotNull.Type))
                {
                    IOperation access = VisitRequired(currentConditionalAccess.WhenNotNull);
                    AddStatement(new FlowCaptureOperation(resultCaptureId, currentConditionalAccess.WhenNotNull.Syntax,
                        MakeNullable(access, operation.Type)));
                }
                else
                {
                    CaptureResultIfNotAlready(currentConditionalAccess.WhenNotNull.Syntax, resultCaptureId,
                                              VisitRequired(currentConditionalAccess.WhenNotNull, resultCaptureId));
                }

                PopStackFrame(expressionFrame);
                LeaveRegionsUpTo(resultCaptureRegion);

                resetConditionalAccessTracker();

                var afterAccess = new BasicBlockBuilder(BasicBlockKind.Block);
                UnconditionalBranch(afterAccess);

                AppendNewBlock(whenNull);

                SyntaxNode defaultValueSyntax = (operation.Operation == testExpression ? testExpression : operation).Syntax;

                Debug.Assert(operation.Type is not null);
                AddStatement(new FlowCaptureOperation(resultCaptureId,
                                             defaultValueSyntax,
                                             new DefaultValueOperation(semanticModel: null, defaultValueSyntax, operation.Type,
                                                                        (operation.Type.IsReferenceType && !ITypeSymbolHelpers.IsNullableType(operation.Type))
                                                                            ? ConstantValue.Null
                                                                            : null,
                                                                        isImplicit: true)));

                AppendNewBlock(afterAccess);

                return GetCaptureReference(resultCaptureId, operation);
            }

            void resetConditionalAccessTracker()
            {
                Debug.Assert(!_currentConditionalAccessTracker.IsDefault);
                Debug.Assert(_currentConditionalAccessTracker.Operations.Count == 0);
                _currentConditionalAccessTracker.Free();
                _currentConditionalAccessTracker = previousTracker;
            }

            static bool isConditionalAccessInstancePresentInChildren(IOperation operation)
            {
                if (operation is InvalidOperation invalidOperation)
                {
                    return checkInvalidChildren(invalidOperation);
                }

                // The conditional access should always be first leaf node in the subtree when performing a depth-first search. Visit the first child recursively
                // until we either reach the bottom, or find the conditional access.
                Operation currentOperation = (Operation)operation;
                while (currentOperation.ChildOperations.GetEnumerator() is var enumerator && enumerator.MoveNext())
                {
                    if (enumerator.Current is IConditionalAccessInstanceOperation)
                    {
                        return true;
                    }
                    else if (enumerator.Current is InvalidOperation invalidChild)
                    {
                        return checkInvalidChildren(invalidChild);
                    }

                    currentOperation = (Operation)enumerator.Current;
                }

                return false;
            }

            static bool checkInvalidChildren(InvalidOperation operation)
            {
                // Invalid operations can have children ordering that doesn't put the conditional access instance first. For these cases,
                // use a recursive check
                foreach (var child in operation.ChildOperations)
                {
                    if (child is IConditionalAccessInstanceOperation || isConditionalAccessInstancePresentInChildren(child))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override IOperation VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, int? captureIdForResult)
        {
            Debug.Assert(!_currentConditionalAccessTracker.IsDefault && _currentConditionalAccessTracker.Operations.Count > 0);

            IOperation testExpression = _currentConditionalAccessTracker.Operations.Pop();
            return VisitConditionalAccessTestExpression(testExpression);
        }

        private IOperation VisitConditionalAccessTestExpression(IOperation testExpression)
        {
            Debug.Assert(!_currentConditionalAccessTracker.IsDefault);
            SyntaxNode testExpressionSyntax = testExpression.Syntax;
            ITypeSymbol? testExpressionType = testExpression.Type;

            var frame = PushStackFrame();
            PushOperand(VisitRequired(testExpression));
            SpillEvalStack();
            IOperation spilledTestExpression = PopOperand();
            PopStackFrame(frame);

            ConditionalBranch(MakeIsNullOperation(spilledTestExpression),
                jumpIfTrue: true,
                _currentConditionalAccessTracker.WhenNull);
            _currentBasicBlock = null;

            IOperation receiver = OperationCloner.CloneOperation(spilledTestExpression);

            if (ITypeSymbolHelpers.IsNullableType(testExpressionType))
            {
                receiver = CallNullableMember(receiver, SpecialMember.System_Nullable_T_GetValueOrDefault);
            }

            return receiver;
        }

        public override IOperation? VisitExpressionStatement(IExpressionStatementOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            IOperation? underlying = Visit(operation.Operation);

            if (underlying == null)
            {
                Debug.Assert(operation.Operation.Kind == OperationKind.ConditionalAccess || operation.Operation.Kind == OperationKind.CoalesceAssignment);
                return FinishVisitingStatement(operation);
            }
            else if (operation.Operation.Kind == OperationKind.Throw)
            {
                return FinishVisitingStatement(operation);
            }

            return FinishVisitingStatement(operation, new ExpressionStatementOperation(underlying, semanticModel: null, operation.Syntax, IsImplicit(operation)));
        }

        public override IOperation? VisitWhileLoop(IWhileLoopOperation operation, int? captureIdForResult)
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

                Debug.Assert(operation.Condition is not null);
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

        public override IOperation? VisitTry(ITryOperation operation, int? captureIdForResult)
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

            RegionBuilder? tryAndFinallyRegion = null;
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

            Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.Try);
            VisitStatement(operation.Body);
            UnconditionalBranch(afterTryCatchFinally);

            if (haveCatches)
            {
                LeaveRegion();

                foreach (ICatchClauseOperation catchClause in operation.Catches)
                {
                    RegionBuilder? filterAndHandlerRegion = null;

                    IOperation? exceptionDeclarationOrExpression = catchClause.ExceptionDeclarationOrExpression;
                    IOperation? filter = catchClause.Filter;
                    bool haveFilter = filter != null;
                    var catchBlock = new BasicBlockBuilder(BasicBlockKind.Block);

                    if (haveFilter)
                    {
                        filterAndHandlerRegion = new RegionBuilder(ControlFlowRegionKind.FilterAndHandler, catchClause.ExceptionType, catchClause.Locals);
                        EnterRegion(filterAndHandlerRegion);

                        var filterRegion = new RegionBuilder(ControlFlowRegionKind.Filter, catchClause.ExceptionType);
                        EnterRegion(filterRegion);

                        AddExceptionStore(catchClause.ExceptionType, exceptionDeclarationOrExpression);

                        VisitConditionalBranch(filter!, ref catchBlock, jumpIfTrue: true);
                        var continueDispatchBlock = new BasicBlockBuilder(BasicBlockKind.Block);
                        AppendNewBlock(continueDispatchBlock);
                        continueDispatchBlock.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;
                        LeaveRegion();

                        Debug.Assert(!filterRegion.IsEmpty);
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

                    Debug.Assert(!handlerRegion.IsEmpty);
                    if (haveFilter)
                    {
                        Debug.Assert(CurrentRegionRequired == filterAndHandlerRegion);
                        LeaveRegion();
#if DEBUG
                        Debug.Assert(filterAndHandlerRegion.Regions![0].LastBlock!.FallThrough.Destination == null);
                        if (handlerRegion.FirstBlock.HasPredecessors)
                        {
                            var predecessors = ArrayBuilder<BasicBlockBuilder>.GetInstance();
                            handlerRegion.FirstBlock.GetPredecessors(predecessors);
                            Debug.Assert(predecessors.All(p => filterAndHandlerRegion.Regions[0].FirstBlock!.Ordinal <= p.Ordinal &&
                                                          filterAndHandlerRegion.Regions[0].LastBlock!.Ordinal >= p.Ordinal));
                            predecessors.Free();
                        }
#endif
                    }
                    else
                    {
                        Debug.Assert(!handlerRegion.FirstBlock.HasPredecessors);
                    }
                }

                Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.TryAndCatch);
                LeaveRegion();
            }

            if (haveFinally)
            {
                Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.Try);
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
                Debug.Assert(!finallyRegion.IsEmpty);
                Debug.Assert(finallyRegion.LastBlock.FallThrough.Destination == null);
                Debug.Assert(!finallyRegion.FirstBlock.HasPredecessors);
            }

            AppendNewBlock(afterTryCatchFinally, linkToPrevious: false);
            Debug.Assert(tryAndFinallyRegion?.Regions![1].LastBlock!.FallThrough.Destination == null);

            return FinishVisitingStatement(operation);
        }

        private void AddExceptionStore(ITypeSymbol exceptionType, IOperation? exceptionDeclarationOrExpression)
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
                                                                  constantValue: null,
                                                                  isImplicit: true);
                }
                else
                {
                    exceptionTarget = VisitRequired(exceptionDeclarationOrExpression);
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
                        constantValue: null,
                        isImplicit: true));
                }
            }
        }

        public override IOperation VisitCatchClause(ICatchClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation? VisitReturn(IReturnOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            IOperation? returnedValue = Visit(operation.ReturnedValue);

            switch (operation.Kind)
            {
                case OperationKind.YieldReturn:
                    AddStatement(new ReturnOperation(returnedValue, OperationKind.YieldReturn, semanticModel: null, operation.Syntax, IsImplicit(operation)));
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

        public override IOperation? VisitLabeled(ILabeledOperation operation, int? captureIdForResult)
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

        private BasicBlockBuilder GetLabeledOrNewBlock(ILabelSymbol? labelOpt)
        {
            if (labelOpt == null)
            {
                return new BasicBlockBuilder(BasicBlockKind.Block);
            }

            BasicBlockBuilder? labeledBlock;

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

        public override IOperation? VisitBranch(IBranchOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            UnconditionalBranch(GetLabeledOrNewBlock(operation.Target));
            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitEmpty(IEmptyOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitThrow(IThrowOperation operation, int? captureIdForResult)
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
                return new NoneOperation(children: ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, constantValue: null, isImplicit: true, type: null);
            }
        }

        private void LinkThrowStatement(IOperation? exception)
        {
            BasicBlockBuilder current = CurrentBasicBlock;
            Debug.Assert(current.BranchValue == null);
            Debug.Assert(!current.HasCondition);
            Debug.Assert(current.FallThrough.Destination == null);
            Debug.Assert(current.FallThrough.Kind == ControlFlowBranchSemantics.None);
            current.BranchValue = Operation.SetParentOperation(exception, null);
            current.FallThrough.Kind = exception == null ? ControlFlowBranchSemantics.Rethrow : ControlFlowBranchSemantics.Throw;
        }

        public override IOperation? VisitUsing(IUsingOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            DisposeOperationInfo disposeInfo = ((UsingOperation)operation).DisposeInfo;
            HandleUsingOperationParts(operation.Resources, operation.Body, disposeInfo.DisposeMethod, disposeInfo.DisposeArguments, operation.Locals, operation.IsAsynchronous);
            return FinishVisitingStatement(operation);
        }

        private void HandleUsingOperationParts(IOperation resources, IOperation body, IMethodSymbol? disposeMethod, ImmutableArray<IArgumentOperation> disposeArguments, ImmutableArray<ILocalSymbol> locals, bool isAsynchronous,
            Func<IOperation, IOperation>? visitResource = null)
        {
            var usingRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(usingRegion);

            ITypeSymbol iDisposable = isAsynchronous
                ? _compilation.CommonGetWellKnownType(WellKnownType.System_IAsyncDisposable).GetITypeSymbol()
                : _compilation.GetSpecialType(SpecialType.System_IDisposable);

            if (resources is IVariableDeclarationGroupOperation declarationGroup)
            {
                Debug.Assert(visitResource is null);

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
                IOperation resource = visitResource != null ? visitResource(resources) : VisitRequired(resources);

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

            void processQueue(ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)>? resourceQueueOpt)
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
                                                                 constantValue: null, isImplicit: true),
                                    resourceQueueOpt);
                }
            }

            bool shouldConvertToIDisposableBeforeTry(IOperation resource)
            {
                return resource.Type == null || resource.Type.Kind == SymbolKind.DynamicType;
            }

            void processResource(IOperation resource, ArrayBuilder<(IVariableDeclarationOperation, IVariableDeclaratorOperation)>? resourceQueueOpt)
            {
                // When ResourceType is a non-nullable value type that implements IDisposable, the expansion is:
                //
                // {
                //   ResourceType resource = expr;
                //   try { statement; }
                //   finally { ((IDisposable)resource).Dispose(); }
                // }
                //
                // Otherwise, when ResourceType is a non-nullable value type that implements
                // disposal via pattern, the expansion is:
                //
                // {
                //   try { statement; }
                //   finally { resource.Dispose(); }
                // }
                // Otherwise, when Resource type is a nullable value type or
                // a reference type other than dynamic that implements IDisposable, the expansion is:
                //
                // {
                //   ResourceType resource = expr;
                //   try { statement; }
                //   finally { if (resource != null) ((IDisposable)resource).Dispose(); }
                // }
                //
                // Otherwise, when Resource type is a reference type other than dynamic
                // that implements disposal via pattern, the expansion is:
                //
                // {
                //   ResourceType resource = expr;
                //   try { statement; }
                //   finally { if (resource != null) resource.Dispose(); }
                // }
                //
                // Otherwise, when ResourceType is dynamic, the expansion is:
                // {
                //   dynamic resource = expr;
                //   IDisposable d = (IDisposable)resource;
                //   try { statement; }
                //   finally { if (d != null) d.Dispose(); }
                // }

                RegionBuilder? resourceRegion = null;

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

                Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.Try);
                UnconditionalBranch(afterTryFinally);

                LeaveRegion();

                AddDisposingFinally(resource, requiresRuntimeConversion: false, iDisposable, disposeMethod, disposeArguments, isAsynchronous);

                Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.TryAndFinally);
                LeaveRegion();

                if (resourceRegion != null)
                {
                    Debug.Assert(_currentRegion == resourceRegion);
                    LeaveRegion();
                }

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }
        }

        private void AddDisposingFinally(IOperation resource, bool requiresRuntimeConversion, ITypeSymbol iDisposable, IMethodSymbol? disposeMethod, ImmutableArray<IArgumentOperation> disposeArguments, bool isAsynchronous)
        {
            Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.TryAndFinally);
            Debug.Assert(resource.Type is not null);

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            var finallyRegion = new RegionBuilder(ControlFlowRegionKind.Finally);
            EnterRegion(finallyRegion);
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (requiresRuntimeConversion)
            {
                Debug.Assert(!isNotNullableValueType(resource.Type));
                resource = ConvertToIDisposable(resource, iDisposable, isTryCast: true);
                int captureId = GetNextCaptureId(finallyRegion);
                AddStatement(new FlowCaptureOperation(captureId, resource.Syntax, resource));
                resource = GetCaptureReference(captureId, resource);
                Debug.Assert(resource.Type is not null);
            }

            if (requiresRuntimeConversion || !isNotNullableValueType(resource.Type))
            {
                IOperation condition = MakeIsNullOperation(OperationCloner.CloneOperation(resource));
                ConditionalBranch(condition, jumpIfTrue: true, endOfFinally);
                _currentBasicBlock = null;
            }

            if (!iDisposable.Equals(resource.Type) && disposeMethod is null)
            {
                if (resource.Type.IsReferenceType)
                {
                    resource = ConvertToIDisposable(resource, iDisposable);
                }
                else if (ITypeSymbolHelpers.IsNullableType(resource.Type))
                {
                    resource = CallNullableMember(resource, SpecialMember.System_Nullable_T_GetValueOrDefault);
                }
            }

            EvalStackFrame disposeFrame = PushStackFrame();

            AddStatement(tryDispose(resource) ??
                         MakeInvalidOperation(type: null, resource));

            PopStackFrameAndLeaveRegion(disposeFrame);

            AppendNewBlock(endOfFinally);

            Debug.Assert(_currentRegion == finallyRegion);
            LeaveRegion();
            return;

            IOperation? tryDispose(IOperation value)
            {
                Debug.Assert((disposeMethod is object && !disposeArguments.IsDefault) ||
                             ((value.Type!.Equals(iDisposable) || (!value.Type.IsReferenceType && !ITypeSymbolHelpers.IsNullableType(value.Type))) && disposeArguments.IsDefaultOrEmpty));

                var method = disposeMethod ?? (isAsynchronous
                    ? (IMethodSymbol?)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync)?.GetISymbol()
                    : (IMethodSymbol?)_compilation.CommonGetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose)?.GetISymbol());

                if (method != null)
                {
                    ImmutableArray<IArgumentOperation> args;
                    if (disposeMethod is not null)
                    {
                        PushOperand(value);
                        args = VisitArguments(disposeArguments, instancePushed: true);
                        value = PopOperand();
                    }
                    else
                    {
                        args = ImmutableArray<IArgumentOperation>.Empty;
                    }

                    var invocation = new InvocationOperation(method, constrainedToType: null, value, isVirtual: disposeMethod is (null or { IsVirtual: true } or { IsAbstract: true }),
                                                             args, semanticModel: null, value.Syntax,
                                                             method.ReturnType, isImplicit: true);

                    if (isAsynchronous)
                    {
                        return new AwaitOperation(invocation, semanticModel: null, value.Syntax, _compilation.GetSpecialType(SpecialType.System_Void), isImplicit: true);
                    }

                    return invocation;
                }

                return null;
            }

            bool isNotNullableValueType([NotNullWhen(true)] ITypeSymbol? type)
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

        public override IOperation? VisitLock(ILockOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            // `lock (l) { }` on value of type `System.Threading.Lock` is lowered to `using (l.EnterScope()) { }`.
            if (operation.LockedValue.Type?.IsWellKnownTypeLock() == true)
            {
                if (operation.LockedValue.Type.TryFindLockTypeInfo() is { } lockTypeInfo)
                {
                    HandleUsingOperationParts(
                        resources: operation.LockedValue,
                        body: operation.Body,
                        disposeMethod: lockTypeInfo.ScopeDisposeMethod,
                        disposeArguments: ImmutableArray<IArgumentOperation>.Empty,
                        locals: ImmutableArray<ILocalSymbol>.Empty,
                        isAsynchronous: false,
                        visitResource: (resource) =>
                        {
                            var lockObject = VisitRequired(resource);

                            return new InvocationOperation(
                                targetMethod: lockTypeInfo.EnterScopeMethod,
                                constrainedToType: null,
                                instance: lockObject,
                                isVirtual: lockTypeInfo.EnterScopeMethod.IsVirtual ||
                                    lockTypeInfo.EnterScopeMethod.IsAbstract ||
                                    lockTypeInfo.EnterScopeMethod.IsOverride,
                                arguments: ImmutableArray<IArgumentOperation>.Empty,
                                semanticModel: null,
                                syntax: lockObject.Syntax,
                                type: lockTypeInfo.EnterScopeMethod.ReturnType,
                                isImplicit: true);
                        });

                    return FinishVisitingStatement(operation);
                }
                else
                {
                    IOperation? underlying = Visit(operation.LockedValue);

                    if (underlying is not null)
                    {
                        AddStatement(new ExpressionStatementOperation(
                            MakeInvalidOperation(type: null, underlying),
                            semanticModel: null,
                            operation.Syntax,
                            IsImplicit(operation)));
                    }

                    VisitStatement(operation.Body);

                    return FinishVisitingStatement(operation);
                }
            }

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
            var lockStatement = (LockOperation)operation;

            var lockRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime,
                                               locals: lockStatement.LockTakenSymbol != null ?
                                                   ImmutableArray.Create(lockStatement.LockTakenSymbol) :
                                                   ImmutableArray<ILocalSymbol>.Empty);
            EnterRegion(lockRegion);

            EvalStackFrame frame = PushStackFrame();
            IOperation lockedValue = VisitRequired(operation.LockedValue);

            if (!objectType.Equals(lockedValue.Type))
            {
                lockedValue = CreateConversion(lockedValue, objectType);
            }

            PushOperand(lockedValue);
            SpillEvalStack();
            lockedValue = PopOperand();
            PopStackFrame(frame);

            var enterMethod = (IMethodSymbol?)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter2)?.GetISymbol();
            bool legacyMode = (enterMethod == null);

            if (legacyMode)
            {
                Debug.Assert(lockStatement.LockTakenSymbol == null);
                enterMethod = (IMethodSymbol?)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Enter)?.GetISymbol();

                // Monitor.Enter($lock);
                if (enterMethod == null)
                {
                    AddStatement(MakeInvalidOperation(type: null, lockedValue));
                }
                else
                {
                    AddStatement(new InvocationOperation(enterMethod, constrainedToType: null, instance: null, isVirtual: false,
                                                          ImmutableArray.Create<IArgumentOperation>(
                                                                    new ArgumentOperation(ArgumentKind.Explicit,
                                                                                          enterMethod.Parameters[0],
                                                                                          lockedValue,
                                                                                          inConversion: OperationFactory.IdentityConversion,
                                                                                          outConversion: OperationFactory.IdentityConversion,
                                                                                          semanticModel: null,
                                                                                          lockedValue.Syntax,
                                                                                          isImplicit: true)),
                                                          semanticModel: null, lockedValue.Syntax,
                                                          enterMethod.ReturnType, isImplicit: true));
                }
            }

            var afterTryFinally = new BasicBlockBuilder(BasicBlockKind.Block);

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.TryAndFinally));
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Try));

            IOperation? lockTaken = null;
            if (!legacyMode)
            {
                // Monitor.Enter($lock, ref $lockTaken);
                Debug.Assert(lockStatement.LockTakenSymbol is not null);
                Debug.Assert(enterMethod is not null);
                lockTaken = new LocalReferenceOperation(lockStatement.LockTakenSymbol, isDeclaration: true, semanticModel: null, lockedValue.Syntax,
                                                         lockStatement.LockTakenSymbol.Type, constantValue: null, isImplicit: true);
                AddStatement(new InvocationOperation(enterMethod, constrainedToType: null, instance: null, isVirtual: false,
                                                      ImmutableArray.Create<IArgumentOperation>(
                                                                new ArgumentOperation(ArgumentKind.Explicit,
                                                                                      enterMethod.Parameters[0],
                                                                                      lockedValue,
                                                                                      inConversion: OperationFactory.IdentityConversion,
                                                                                      outConversion: OperationFactory.IdentityConversion,
                                                                                      semanticModel: null,
                                                                                      lockedValue.Syntax,
                                                                                      isImplicit: true),
                                                                new ArgumentOperation(ArgumentKind.Explicit,
                                                                                      enterMethod.Parameters[1],
                                                                                      lockTaken,
                                                                                      inConversion: OperationFactory.IdentityConversion,
                                                                                      outConversion: OperationFactory.IdentityConversion,
                                                                                      semanticModel: null,
                                                                                      lockedValue.Syntax,
                                                                                      isImplicit: true)),
                                                      semanticModel: null, lockedValue.Syntax,
                                                      enterMethod.ReturnType, isImplicit: true));
            }

            VisitStatement(operation.Body);

            Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.Try);
            UnconditionalBranch(afterTryFinally);

            LeaveRegion();

            var endOfFinally = new BasicBlockBuilder(BasicBlockKind.Block);
            endOfFinally.FallThrough.Kind = ControlFlowBranchSemantics.StructuredExceptionHandling;

            EnterRegion(new RegionBuilder(ControlFlowRegionKind.Finally));
            AppendNewBlock(new BasicBlockBuilder(BasicBlockKind.Block));

            if (!legacyMode)
            {
                // if ($lockTaken)
                Debug.Assert(lockStatement.LockTakenSymbol is not null);
                IOperation condition = new LocalReferenceOperation(lockStatement.LockTakenSymbol, isDeclaration: false, semanticModel: null, lockedValue.Syntax,
                                                                    lockStatement.LockTakenSymbol.Type, constantValue: null, isImplicit: true);
                ConditionalBranch(condition, jumpIfTrue: false, endOfFinally);
                _currentBasicBlock = null;
            }

            // Monitor.Exit($lock);
            var exitMethod = (IMethodSymbol?)_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Threading_Monitor__Exit)?.GetISymbol();
            lockedValue = OperationCloner.CloneOperation(lockedValue);

            if (exitMethod == null)
            {
                AddStatement(MakeInvalidOperation(type: null, lockedValue));
            }
            else
            {
                AddStatement(new InvocationOperation(exitMethod, constrainedToType: null, instance: null, isVirtual: false,
                                                     ImmutableArray.Create<IArgumentOperation>(
                                                               new ArgumentOperation(ArgumentKind.Explicit,
                                                                                     exitMethod.Parameters[0],
                                                                                     lockedValue,
                                                                                     inConversion: OperationFactory.IdentityConversion,
                                                                                     outConversion: OperationFactory.IdentityConversion,
                                                                                     semanticModel: null,
                                                                                     lockedValue.Syntax,
                                                                                     isImplicit: true)),
                                                     semanticModel: null, lockedValue.Syntax,
                                                     exitMethod.ReturnType, isImplicit: true));
            }

            AppendNewBlock(endOfFinally);

            LeaveRegion();
            Debug.Assert(CurrentRegionRequired.Kind == ControlFlowRegionKind.TryAndFinally);
            LeaveRegion();

            LeaveRegionsUpTo(lockRegion);
            LeaveRegion();

            AppendNewBlock(afterTryFinally, linkToPrevious: false);

            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitForEachLoop(IForEachLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            var enumeratorCaptureRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime);
            EnterRegion(enumeratorCaptureRegion);

            ForEachLoopOperationInfo? info = ((ForEachLoopOperation)operation).Info;

            RegionBuilder? regionForCollection = null;

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
                    ? _compilation.CommonGetWellKnownType(WellKnownType.System_IAsyncDisposable).GetITypeSymbol()
                    : _compilation.GetSpecialType(SpecialType.System_IDisposable);

                AddDisposingFinally(OperationCloner.CloneOperation(enumerator),
                                    requiresRuntimeConversion: !info.KnownToImplementIDisposable && info.PatternDisposeMethod == null,
                                    iDisposable,
                                    info.PatternDisposeMethod,
                                    info.DisposeArguments,
                                    isAsynchronous);

                Debug.Assert(_currentRegion.Kind == ControlFlowRegionKind.TryAndFinally);
                LeaveRegion();

                AppendNewBlock(afterTryFinally, linkToPrevious: false);
            }

            Debug.Assert(_currentRegion == enumeratorCaptureRegion);
            LeaveRegion();

            return FinishVisitingStatement(operation);

            IOperation applyConversion(IConvertibleConversion? conversionOpt, IOperation operand, ITypeSymbol? targetType)
            {
                if (conversionOpt?.ToCommonConversion().IsIdentity == false)
                {
                    operand = new ConversionOperation(operand, conversionOpt, isTryCast: false, isChecked: false, semanticModel: null,
                                                      operand.Syntax, targetType, constantValue: null, isImplicit: true);
                }

                return operand;
            }

            IOperation getEnumerator()
            {
                IOperation result;
                EvalStackFrame getEnumeratorFrame = PushStackFrame();

                if (info?.GetEnumeratorMethod != null)
                {
                    IOperation? collection = info.GetEnumeratorMethod.IsStatic ? null : Visit(operation.Collection);

                    if (collection is not null && info.InlineArrayConversion is { } inlineArrayConversion)
                    {
                        if (info.CollectionIsInlineArrayValue)
                        {
                            // We cannot convert a value to a span, need to make a local copy and convert that.
                            int localCopyCaptureId = GetNextCaptureId(enumeratorCaptureRegion);
                            AddStatement(new FlowCaptureOperation(localCopyCaptureId, operation.Collection.Syntax, collection));

                            collection = new FlowCaptureReferenceOperation(localCopyCaptureId, operation.Collection.Syntax, collection.Type, constantValue: null);
                        }

                        collection = applyConversion(inlineArrayConversion, collection, info.GetEnumeratorMethod.ContainingType);
                    }

                    IOperation invocation = makeInvocation(operation.Collection.Syntax,
                                                           info.GetEnumeratorMethod,
                                                           collection,
                                                           info.GetEnumeratorArguments);

                    int enumeratorCaptureId = GetNextCaptureId(enumeratorCaptureRegion);
                    AddStatement(new FlowCaptureOperation(enumeratorCaptureId, operation.Collection.Syntax, invocation));

                    result = new FlowCaptureReferenceOperation(enumeratorCaptureId, operation.Collection.Syntax, info.GetEnumeratorMethod.ReturnType, constantValue: null);
                }
                else
                {
                    // This must be an error case
                    AddStatement(MakeInvalidOperation(type: null, VisitRequired(operation.Collection)));
                    result = new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Collection.Syntax,
                                                  type: null, constantValue: null, isImplicit: true);
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
                        return new AwaitOperation(moveNext, semanticModel: null, operation.Syntax, _compilation.GetSpecialType(SpecialType.System_Boolean), isImplicit: true);
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
                    var instance = info.CurrentProperty.IsStatic ? null : enumeratorRef;
                    var visitedArguments = makeArguments(info.CurrentArguments, ref instance);
                    return new PropertyReferenceOperation(info.CurrentProperty,
                                                          constrainedToType: null,
                                                          visitedArguments,
                                                          instance,
                                                          semanticModel: null,
                                                          operation.LoopControlVariable.Syntax,
                                                          info.CurrentProperty.Type, isImplicit: true);
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
                                                                                         constantValue: null,
                                                                                         isImplicit: true),
                                                             current,
                                                             semanticModel: null,
                                                             declarator.Syntax,
                                                             type: null,
                                                             constantValue: null,
                                                             isImplicit: true);

                    case OperationKind.Tuple:
                    case OperationKind.DeclarationExpression:
                        Debug.Assert(info?.ElementConversion?.ToCommonConversion().IsIdentity != false);

                        return new DeconstructionAssignmentOperation(VisitPreservingTupleOperations(operation.LoopControlVariable),
                                                                     current, semanticModel: null,
                                                                     operation.LoopControlVariable.Syntax, operation.LoopControlVariable.Type,
                                                                     isImplicit: true);
                    default:
                        return new SimpleAssignmentOperation(isRef: false, // In C# this is an error case and VB doesn't support ref locals
                            VisitRequired(operation.LoopControlVariable),
                            current, semanticModel: null, operation.LoopControlVariable.Syntax,
                            operation.LoopControlVariable.Type,
                            constantValue: null, isImplicit: true);
                }
            }

            InvocationOperation makeInvocationDroppingInstanceForStaticMethods(IMethodSymbol method, IOperation instance, ImmutableArray<IArgumentOperation> arguments)
            {
                return makeInvocation(instance.Syntax, method, method.IsStatic ? null : instance, arguments);
            }

            InvocationOperation makeInvocation(SyntaxNode syntax, IMethodSymbol method, IOperation? instanceOpt, ImmutableArray<IArgumentOperation> arguments)
            {
                Debug.Assert(method.IsStatic == (instanceOpt == null));
                var visitedArguments = makeArguments(arguments, ref instanceOpt);
                return new InvocationOperation(method, constrainedToType: null, instanceOpt,
                                               isVirtual: method.IsVirtual || method.IsAbstract || method.IsOverride,
                                               visitedArguments, semanticModel: null, syntax,
                                               method.ReturnType, isImplicit: true);
            }

            ImmutableArray<IArgumentOperation> makeArguments(ImmutableArray<IArgumentOperation> arguments, ref IOperation? instance)
            {
                if (!arguments.IsDefaultOrEmpty)
                {
                    bool hasInstance = instance != null;
                    if (hasInstance)
                    {
                        PushOperand(instance!);
                    }
                    arguments = VisitArguments(arguments, hasInstance);
                    instance = hasInstance ? PopOperand() : null;

                    return arguments;
                }

                return ImmutableArray<IArgumentOperation>.Empty;
            }
        }

        public override IOperation? VisitForToLoop(IForToLoopOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            (ILocalSymbol loopObject, ForToLoopOperationUserDefinedInfo userDefinedInfo) = ((ForToLoopOperation)operation).Info;
            bool isObjectLoop = (loopObject != null);
            ImmutableArray<ILocalSymbol> locals = operation.Locals;

            if (isObjectLoop)
            {
                locals = locals.Insert(0, loopObject!);
            }

            ITypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            BasicBlockBuilder @continue = GetLabeledOrNewBlock(operation.ContinueLabel);
            BasicBlockBuilder? @break = GetLabeledOrNewBlock(operation.ExitLabel);
            BasicBlockBuilder checkConditionBlock = new BasicBlockBuilder(BasicBlockKind.Block);
            BasicBlockBuilder bodyBlock = new BasicBlockBuilder(BasicBlockKind.Block);

            var loopRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(loopRegion);

            // Handle loop initialization
            int limitValueId = -1;
            int stepValueId = -1;
            IFlowCaptureReferenceOperation? positiveFlag = null;
            ITypeSymbol? stepEnumUnderlyingTypeOrSelf = ITypeSymbolHelpers.GetEnumUnderlyingTypeOrSelf(operation.StepValue.Type);

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
                Debug.Assert(isObjectLoop && loopObject is not null);
                bool isInitialization = (helper == WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);
                var loopObjectReference = new LocalReferenceOperation(loopObject,
                                                                       isDeclaration: isInitialization,
                                                                       semanticModel: null,
                                                                       operation.LoopControlVariable.Syntax, loopObject.Type,
                                                                       constantValue: null, isImplicit: true);

                var method = (IMethodSymbol?)_compilation.CommonGetWellKnownTypeMember(helper)?.GetISymbol();
                int parametersCount = WellKnownMembers.GetDescriptor(helper).ParametersCount;

                if (method is null)
                {
                    var builder = ArrayBuilder<IOperation>.GetInstance(--parametersCount, fillWithValue: null!);
                    builder[--parametersCount] = loopObjectReference;
                    do
                    {
                        builder[--parametersCount] = PopOperand();
                    }
                    while (parametersCount != 0);

                    Debug.Assert(builder.All(o => o != null));
                    return MakeInvalidOperation(operation.LimitValue.Syntax, booleanType, builder.ToImmutableAndFree());
                }
                else
                {
                    var builder = ArrayBuilder<IArgumentOperation>.GetInstance(parametersCount, fillWithValue: null!);

                    builder[--parametersCount] = new ArgumentOperation(ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       visitLoopControlVariableReference(forceImplicit: true), // Yes we are going to evaluate it again
                                                                       inConversion: OperationFactory.IdentityConversion,
                                                                       outConversion: OperationFactory.IdentityConversion,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    builder[--parametersCount] = new ArgumentOperation(ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                       loopObjectReference,
                                                                       inConversion: OperationFactory.IdentityConversion,
                                                                       outConversion: OperationFactory.IdentityConversion,
                                                                       semanticModel: null, syntax, isImplicit: true);

                    do
                    {
                        IOperation value = PopOperand();
                        builder[--parametersCount] = new ArgumentOperation(ArgumentKind.Explicit, method.Parameters[parametersCount],
                                                                           value,
                                                                           inConversion: OperationFactory.IdentityConversion,
                                                                           outConversion: OperationFactory.IdentityConversion,
                                                                           semanticModel: null, isInitialization ? value.Syntax : syntax, isImplicit: true);
                    }
                    while (parametersCount != 0);

                    Debug.Assert(builder.All(op => op is not null));

                    return new InvocationOperation(method, constrainedToType: null, instance: null, isVirtual: false, builder.ToImmutableAndFree(),
                                                   semanticModel: null, operation.LimitValue.Syntax, method.ReturnType,
                                                   isImplicit: true);
                }
            }

            void initializeLoop()
            {
                EvalStackFrame frame = PushStackFrame();

                PushOperand(visitLoopControlVariableReference(forceImplicit: false));
                PushOperand(VisitRequired(operation.InitialValue));

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

                    PushOperand(VisitRequired(operation.LimitValue));
                    PushOperand(VisitRequired(operation.StepValue));

                    IOperation condition = tryCallObjectForLoopControlHelper(operation.LoopControlVariable.Syntax,
                                                                             WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl__ForLoopInitObj);

                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);
                }
                else
                {
                    SpillEvalStack();
                    RegionBuilder currentRegion = CurrentRegionRequired;

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

                        IOperation subtraction = VisitRequired(userDefinedInfo.Subtraction);

                        _forToLoopBinaryOperatorLeftOperand = stepValue;
                        _forToLoopBinaryOperatorRightOperand = subtraction;

                        int positiveFlagId = GetNextCaptureId(loopRegion);
                        VisitAndCapture(userDefinedInfo.GreaterThanOrEqual, positiveFlagId);

                        positiveFlag = GetCaptureReference(positiveFlagId, userDefinedInfo.GreaterThanOrEqual);

                        _forToLoopBinaryOperatorLeftOperand = null;
                        _forToLoopBinaryOperatorRightOperand = null;
                    }
                    else if (!(operation.StepValue.GetConstantValue() is { IsBad: false }) &&
                             !ITypeSymbolHelpers.IsSignedIntegralType(stepEnumUnderlyingTypeOrSelf) &&
                             !ITypeSymbolHelpers.IsUnsignedIntegralType(stepEnumUnderlyingTypeOrSelf))
                    {
                        IOperation? stepValueIsNull = null;

                        if (ITypeSymbolHelpers.IsNullableType(stepValue.Type))
                        {
                            stepValueIsNull = MakeIsNullOperation(GetCaptureReference(stepValueId, operation.StepValue), booleanType);
                            stepValue = CallNullableMember(stepValue, SpecialMember.System_Nullable_T_GetValueOrDefault);
                        }

                        ITypeSymbol? stepValueEnumUnderlyingTypeOrSelf = ITypeSymbolHelpers.GetEnumUnderlyingTypeOrSelf(stepValue.Type);

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
                                isUp = new LiteralOperation(semanticModel: null, stepValue.Syntax, booleanType, constantValue: ConstantValue.Create(false), isImplicit: true);

                                AddStatement(new FlowCaptureOperation(positiveFlagId, isUp.Syntax, isUp));

                                UnconditionalBranch(afterPositiveCheck);
                                AppendNewBlock(whenNotNull);
                            }

                            IOperation literal = new LiteralOperation(semanticModel: null, stepValue.Syntax, stepValue.Type,
                                                                       constantValue: ConstantValue.Default(stepValueEnumUnderlyingTypeOrSelf.SpecialType),
                                                                       isImplicit: true);

                            isUp = new BinaryOperation(BinaryOperatorKind.GreaterThanOrEqual,
                                                       stepValue,
                                                       literal,
                                                       isLifted: false,
                                                       isChecked: false,
                                                       isCompareText: false,
                                                       operatorMethod: null,
                                                       constrainedToType: null,
                                                       unaryOperatorMethod: null,
                                                       semanticModel: null,
                                                       stepValue.Syntax,
                                                       booleanType,
                                                       constantValue: null,
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
                        constantValue: null, isImplicit: true));
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
                    Debug.Assert(positiveFlag is not null);
                    ConditionalBranch(positiveFlag, jumpIfTrue: false, notPositive);
                    _currentBasicBlock = null;

                    _forToLoopBinaryOperatorLeftOperand = controlVariableReferenceForCondition;
                    _forToLoopBinaryOperatorRightOperand = GetCaptureReference(limitValueId, operation.LimitValue);

                    VisitConditionalBranch(userDefinedInfo.LessThanOrEqual, ref @break, jumpIfTrue: false);
                    UnconditionalBranch(bodyBlock);

                    AppendNewBlock(notPositive);

                    _forToLoopBinaryOperatorLeftOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorLeftOperand);
                    _forToLoopBinaryOperatorRightOperand = OperationCloner.CloneOperation(_forToLoopBinaryOperatorRightOperand);

                    VisitConditionalBranch(userDefinedInfo.GreaterThanOrEqual, ref @break, jumpIfTrue: false);
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
                    else if (operation.StepValue.GetConstantValue() is { IsBad: false } value)
                    {
                        Debug.Assert(value.Discriminator != ConstantValueTypeDiscriminator.Bad);
                        if (value.IsNegativeNumeric)
                        {
                            comparisonKind = BinaryOperatorKind.GreaterThanOrEqual;
                        }
                        else if (value.IsNumeric)
                        {
                            comparisonKind = BinaryOperatorKind.LessThanOrEqual;
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
                                                        constrainedToType: null,
                                                        unaryOperatorMethod: null,
                                                        semanticModel: null,
                                                        operation.LimitValue.Syntax,
                                                        booleanType,
                                                        constantValue: null,
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

                    IOperation? eitherLimitOrControlVariableIsNull = null;

                    if (ITypeSymbolHelpers.IsNullableType(operation.LimitValue.Type))
                    {
                        eitherLimitOrControlVariableIsNull = new BinaryOperation(BinaryOperatorKind.Or,
                                                                                 MakeIsNullOperation(limitReference, booleanType),
                                                                                 MakeIsNullOperation(PopOperand(), booleanType),
                                                                                 isLifted: false,
                                                                                 isChecked: false,
                                                                                 isCompareText: false,
                                                                                 operatorMethod: null,
                                                                                 constrainedToType: null,
                                                                                 unaryOperatorMethod: null,
                                                                                 semanticModel: null,
                                                                                 operation.StepValue.Syntax,
                                                                                 _compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                                 constantValue: null,
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
                                                    constrainedToType: null,
                                                    unaryOperatorMethod: null,
                                                    semanticModel: null,
                                                    operation.LimitValue.Syntax,
                                                    booleanType,
                                                    constantValue: null,
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
                                                    constrainedToType: null,
                                                    unaryOperatorMethod: null,
                                                    semanticModel: null,
                                                    operation.LimitValue.Syntax,
                                                    booleanType,
                                                    constantValue: null,
                                                    isImplicit: true);

                    ConditionalBranch(condition, jumpIfTrue: false, @break);
                    UnconditionalBranch(bodyBlock);

                    PopStackFrameAndLeaveRegion(frame);
                    return;
                }

                throw ExceptionUtilities.Unreachable();
            }

            // Produce "(operand Xor (step >> 31))"
            // where 31 is actually the size-1
            IOperation negateIfStepNegative(IOperation operand)
            {
                int bits = stepEnumUnderlyingTypeOrSelf.SpecialType.VBForToShiftBits();

                var shiftConst = new LiteralOperation(semanticModel: null, operand.Syntax, _compilation.GetSpecialType(SpecialType.System_Int32),
                                                       constantValue: ConstantValue.Create(bits), isImplicit: true);

                var shiftedStep = new BinaryOperation(BinaryOperatorKind.RightShift,
                                                      GetCaptureReference(stepValueId, operation.StepValue),
                                                      shiftConst,
                                                      isLifted: false,
                                                      isChecked: false,
                                                      isCompareText: false,
                                                      operatorMethod: null,
                                                      constrainedToType: null,
                                                      unaryOperatorMethod: null,
                                                      semanticModel: null,
                                                      operand.Syntax,
                                                      operation.StepValue.Type,
                                                      constantValue: null,
                                                      isImplicit: true);

                return new BinaryOperation(BinaryOperatorKind.ExclusiveOr,
                                           shiftedStep,
                                           operand,
                                           isLifted: false,
                                           isChecked: false,
                                           isCompareText: false,
                                           operatorMethod: null,
                                           constrainedToType: null,
                                           unaryOperatorMethod: null,
                                           semanticModel: null,
                                           operand.Syntax,
                                           operand.Type,
                                           constantValue: null,
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

                    IOperation increment = VisitRequired(userDefinedInfo.Addition);

                    _forToLoopBinaryOperatorLeftOperand = null;
                    _forToLoopBinaryOperatorRightOperand = null;

                    controlVariableReferenceForAssignment = PopOperand();
                    AddStatement(new SimpleAssignmentOperation(isRef: false,
                        controlVariableReferenceForAssignment,
                        increment,
                        semanticModel: null,
                        controlVariableReferenceForAssignment.Syntax,
                        type: null,
                        constantValue: null,
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
                                                                   constrainedToType: null,
                                                                   unaryOperatorMethod: null,
                                                                   semanticModel: null,
                                                                   operation.StepValue.Syntax,
                                                                   _compilation.GetSpecialType(SpecialType.System_Boolean),
                                                                   constantValue: null,
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
                                constantValue: null,
                                isImplicit: true),
                            semanticModel: null,
                            controlVariableReferenceForAssignment.Syntax,
                            type: null,
                            constantValue: null,
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
                                                               constrainedToType: null,
                                                               unaryOperatorMethod: null,
                                                               semanticModel: null,
                                                               operation.StepValue.Syntax,
                                                               controlVariableReferenceForIncrement.Type,
                                                               constantValue: null,
                                                               isImplicit: true);

                    controlVariableReferenceForAssignment = PopOperand();

                    if (isNullable)
                    {
                        Debug.Assert(controlVariableReferenceForAssignment.Type != null);
                        increment = MakeNullable(increment, controlVariableReferenceForAssignment.Type);
                    }

                    AddStatement(new SimpleAssignmentOperation(isRef: false,
                        controlVariableReferenceForAssignment,
                        increment,
                        semanticModel: null,
                        controlVariableReferenceForAssignment.Syntax,
                        type: null,
                        constantValue: null,
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
                                                            declarator.Syntax, local.Type, constantValue: null, isImplicit: true);

                    default:
                        Debug.Assert(!_forceImplicit);
                        _forceImplicit = forceImplicit;
                        IOperation result = VisitRequired(operation.LoopControlVariable);
                        _forceImplicit = false;
                        return result;
                }
            }
        }

        private static FlowCaptureReferenceOperation GetCaptureReference(int id, IOperation underlying)
        {
            return new FlowCaptureReferenceOperation(id, underlying.Syntax, underlying.Type, underlying.GetConstantValue());
        }

        internal override IOperation VisitAggregateQuery(IAggregateQueryOperation operation, int? captureIdForResult)
        {
            SpillEvalStack();

            IOperation? previousAggregationGroup = _currentAggregationGroup;
            _currentAggregationGroup = VisitAndCapture(operation.Group);

            IOperation result = VisitRequired(operation.Aggregation);

            _currentAggregationGroup = previousAggregationGroup;
            return result;
        }

        public override IOperation? VisitSwitch(ISwitchOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            INamedTypeSymbol booleanType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            IOperation switchValue = VisitAndCapture(operation.Value);

            ImmutableArray<ILocalSymbol> locals = getLocals();
            var switchRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: locals);
            EnterRegion(switchRegion);

            BasicBlockBuilder? defaultBody = null; // Adjusted in handleSection
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

                IOperation? condition = ((SwitchCaseOperation)section).Condition;
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

            void handleCase(ICaseClauseOperation caseClause, BasicBlockBuilder body, [DisallowNull] BasicBlockBuilder? nextCase)
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
                            IOperation rightOperand = VisitRequired(compareWith);
                            IOperation leftOperand = PopOperand();

                            if (isLifted)
                            {
                                if (!leftIsNullable)
                                {
                                    if (leftOperand.Type != null)
                                    {
                                        Debug.Assert(compareWith.Type != null);
                                        leftOperand = MakeNullable(leftOperand, compareWith.Type);
                                    }
                                }
                                else if (!rightIsNullable && rightOperand.Type != null)
                                {
                                    Debug.Assert(operation.Value.Type != null);
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
                                                            constrainedToType: null,
                                                            unaryOperatorMethod: null,
                                                            semanticModel: null,
                                                            compareWith.Syntax,
                                                            booleanType,
                                                            constantValue: null,
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
                            var pattern = (IPatternOperation)VisitRequired(patternClause.Pattern);
                            condition = new IsPatternOperation(PopOperand(), pattern, semanticModel: null,
                                                               patternClause.Pattern.Syntax, booleanType, isImplicit: true);
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
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitRangeCaseClause(IRangeCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitPatternCaseClause(IPatternCaseClauseOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation? VisitEnd(IEndOperation operation, int? captureIdForResult)
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

        public override IOperation? VisitForLoop(IForLoopOperation operation, int? captureIdForResult)
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

        internal override IOperation? VisitFixed(IFixedOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: operation.Locals));

            HandleVariableDeclarations(operation.Variables);

            VisitStatement(operation.Body);

            LeaveRegion();
            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, int? captureIdForResult)
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
            BasicBlockBuilder? afterInitialization = null;
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

            IOperation? initializer = null;
            SyntaxNode? assignmentSyntax = null;
            if (declarator.Initializer != null)
            {
                initializer = Visit(declarator.Initializer.Value);
                assignmentSyntax = declarator.Syntax;
            }

            if (declaration.Initializer != null)
            {
                IOperation operationInitializer = VisitRequired(declaration.Initializer.Value);
                assignmentSyntax = declaration.Syntax;
                if (initializer != null)
                {
                    initializer = new InvalidOperation(ImmutableArray.Create(initializer, operationInitializer),
                                                        semanticModel: null,
                                                        declaration.Syntax,
                                                        type: localSymbol.Type,
                                                        constantValue: null,
                                                        isImplicit: true);
                }
                else
                {
                    initializer = operationInitializer;
                }
            }

            Debug.Assert(initializer != null && assignmentSyntax != null);

            // If we have an afterInitialization, then we must have static local and an initializer to ensure we don't create empty regions that can't be cleaned up.
            Debug.Assert((afterInitialization, localSymbol.IsStatic) is (null, false) or (not null, true));

            // We can't use the IdentifierToken as the syntax for the local reference, so we use the
            // entire declarator as the node
            var localRef = new LocalReferenceOperation(localSymbol, isDeclaration: true, semanticModel: null, declarator.Syntax, localSymbol.Type, constantValue: null, isImplicit: true);
            var assignment = new SimpleAssignmentOperation(isRef: localSymbol.IsRef, localRef, initializer, semanticModel: null, assignmentSyntax, localRef.Type, constantValue: null, isImplicit: true);
            AddStatement(assignment);

            PopStackFrameAndLeaveRegion(frame);

            if (localSymbol.IsStatic)
            {
                LeaveRegion();
                AppendNewBlock(afterInitialization!);
            }
        }

        public override IOperation VisitVariableDeclaration(IVariableDeclarationOperation operation, int? captureIdForResult)
        {
            // All variable declarators should be handled by VisitVariableDeclarationGroup.
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitVariableDeclarator(IVariableDeclaratorOperation operation, int? captureIdForResult)
        {
            // All variable declarators should be handled by VisitVariableDeclaration.
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitVariableInitializer(IVariableInitializerOperation operation, int? captureIdForResult)
        {
            // All variable initializers should be removed from the tree by VisitVariableDeclaration.
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitFlowCapture(IFlowCaptureOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitIsNull(IIsNullOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitCaughtException(ICaughtExceptionOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitInvocation(IInvocationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            IOperation? instance = operation.TargetMethod.IsStatic ? null : operation.Instance;
            (IOperation? visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            PopStackFrame(frame);
            return new InvocationOperation(operation.TargetMethod, operation.ConstrainedToType, visitedInstance, operation.IsVirtual, visitedArguments, semanticModel: null, operation.Syntax,
                                           operation.Type, IsImplicit(operation));
        }

        public override IOperation? VisitFunctionPointerInvocation(IFunctionPointerInvocationOperation operation, int? argument)
        {
            EvalStackFrame frame = PushStackFrame();
            var target = operation.Target;
            var (visitedPointer, visitedArguments) = handlePointerAndArguments(target, operation.Arguments);
            PopStackFrame(frame);
            return new FunctionPointerInvocationOperation(visitedPointer, visitedArguments, semanticModel: null, operation.Syntax,
                                           operation.Type, IsImplicit(operation));

            (IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) handlePointerAndArguments(
                IOperation targetPointer, ImmutableArray<IArgumentOperation> arguments)
            {
                PushOperand(VisitRequired(targetPointer));

                ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(arguments, instancePushed: false);
                IOperation visitedInstance = PopOperand();

                return (visitedInstance, visitedArguments);
            }
        }

        private (IOperation? visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) VisitInstanceWithArguments(IOperation? instance, ImmutableArray<IArgumentOperation> arguments)
        {
            bool hasInstance = instance != null;
            if (hasInstance)
            {
                PushOperand(VisitRequired(instance!));
            }

            ImmutableArray<IArgumentOperation> visitedArguments = VisitArguments(arguments, instancePushed: hasInstance);
            IOperation? visitedInstance = hasInstance ? PopOperand() : null;

            return (visitedInstance, visitedArguments);
        }

        internal override IOperation VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, int? argument)
        {
            EvalStackFrame frame = PushStackFrame();
            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            IOperation initializedInstance = new NoPiaObjectCreationOperation(initializer: null, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitObjectCreation(IObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            EvalStackFrame argumentsFrame = PushStackFrame();
            ImmutableArray<IArgumentOperation> visitedArgs = VisitArguments(operation.Arguments, instancePushed: false);
            PopStackFrame(argumentsFrame);
            // Initializer is removed from the tree and turned into a series of statements that assign to the created instance
            IOperation initializedInstance = new ObjectCreationOperation(operation.Constructor, initializer: null, visitedArgs, semanticModel: null,
                                                                          operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            var initializedInstance = new TypeParameterObjectCreationOperation(initializer: null, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        public override IOperation VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            EvalStackFrame argumentsFrame = PushStackFrame();
            ImmutableArray<IOperation> visitedArguments = VisitArray(operation.Arguments);
            PopStackFrame(argumentsFrame);

            var hasDynamicArguments = (HasDynamicArgumentsExpression)operation;
            IOperation initializedInstance = new DynamicObjectCreationOperation(initializer: null, visitedArguments, hasDynamicArguments.ArgumentNames,
                                                                                hasDynamicArguments.ArgumentRefKinds, semanticModel: null, operation.Syntax,
                                                                                operation.Type, IsImplicit(operation));

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, initializedInstance));
        }

        private IOperation HandleObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation? initializer, IOperation objectCreation)
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
                                                               OperationKind.LocalReference, OperationKind.Binary, OperationKind.FieldReference, OperationKind.Invalid,
                                                               OperationKind.InterpolatedString);
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
                    result = VisitRequired(assignmentOperation);
                }
                else
                {
                    // We push the target, which effectively pushes individual components of the target (ie the instance, and arguments if present).
                    // After that has been pushed, we visit the value of the assignment, to ensure that the instance is captured if
                    // needed. Finally, we reassemble the target, which will pull the potentially captured instance from the stack
                    // and reassemble the member reference from the parts.
                    IOperation right = VisitRequired(assignmentOperation.Value);
                    IOperation left = popTarget(assignmentOperation.Target);

                    result = new SimpleAssignmentOperation(assignmentOperation.IsRef, left, right, semanticModel: null, assignmentOperation.Syntax,
                        assignmentOperation.Type, assignmentOperation.GetConstantValue(), IsImplicit(assignmentOperation));
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

                if (onlyContainsEmptyLeafNestedInitializers(memberInitializer))
                {
                    // However, when the leaf nested initializers are empty, we won't access the chain of initialized members
                    // and we only evaluate the arguments/indexes they contain.
                    addIndexes(memberInitializer);
                    return;
                }

                EvalStackFrame frame = PushStackFrame();
                bool pushSuccess = tryPushTarget(memberInitializer.InitializedMember);
                IOperation instance = pushSuccess ? popTarget(memberInitializer.InitializedMember) : VisitRequired(memberInitializer.InitializedMember);
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
                            // We do not pass an instance here, as the instance is not yet available. For arguments that need the instance (such as interpolated
                            // string handlers), they will handle the missing instance by substituting an IInvalidOperation

                            VisitAndPushArguments(((IPropertyReferenceOperation)memberReference).Arguments, instancePushed: false);
                            SpillEvalStack();
                        }

                        // If there is control flow in the value being assigned, we want to make sure that
                        // the instance is captured appropriately, but the setter/field load in the reference will only be evaluated after
                        // the value has been evaluated. So we assemble the reference after visiting the value.
                        if (!memberReference.Member.IsStatic && memberReference.Instance != null)
                        {
                            PushOperand(VisitRequired(memberReference.Instance));
                        }
                        return true;

                    case OperationKind.ArrayElementReference:
                        var arrayReference = (IArrayElementReferenceOperation)instance;
                        VisitAndPushArray(arrayReference.Indices);
                        SpillEvalStack();
                        PushOperand(VisitRequired(arrayReference.ArrayReference));
                        return true;

                    case OperationKind.ImplicitIndexerReference:
                        var implicitIndexerReference = (IImplicitIndexerReferenceOperation)instance;
                        PushOperand(VisitRequired(implicitIndexerReference.Argument));
                        SpillEvalStack();
                        PushOperand(VisitRequired(implicitIndexerReference.Instance));
                        return true;

                    case OperationKind.DynamicIndexerAccess:
                        var dynamicIndexer = (IDynamicIndexerAccessOperation)instance;
                        VisitAndPushArray(dynamicIndexer.Arguments);
                        SpillEvalStack();
                        PushOperand(VisitRequired(dynamicIndexer.Operation));
                        return true;

                    case OperationKind.DynamicMemberReference:
                        var dynamicReference = (IDynamicMemberReferenceOperation)instance;
                        if (dynamicReference.Instance != null)
                        {
                            PushOperand(VisitRequired(dynamicReference.Instance));
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
                IOperation? instance;
                switch (originalTarget.Kind)
                {
                    case OperationKind.FieldReference:
                        var fieldReference = (IFieldReferenceOperation)originalTarget;
                        instance = (!fieldReference.Member.IsStatic && fieldReference.Instance != null) ? PopOperand() : null;
                        return new FieldReferenceOperation(fieldReference.Field, fieldReference.IsDeclaration, instance, semanticModel: null,
                                                            fieldReference.Syntax, fieldReference.Type, fieldReference.GetConstantValue(), IsImplicit(fieldReference));
                    case OperationKind.EventReference:
                        var eventReference = (IEventReferenceOperation)originalTarget;
                        instance = (!eventReference.Member.IsStatic && eventReference.Instance != null) ? PopOperand() : null;
                        return new EventReferenceOperation(eventReference.Event, eventReference.ConstrainedToType, instance, semanticModel: null, eventReference.Syntax,
                                                            eventReference.Type, IsImplicit(eventReference));
                    case OperationKind.PropertyReference:
                        var propertyReference = (IPropertyReferenceOperation)originalTarget;
                        instance = (!propertyReference.Member.IsStatic && propertyReference.Instance != null) ? PopOperand() : null;
                        ImmutableArray<IArgumentOperation> propertyArguments = PopArray(propertyReference.Arguments, RewriteArgumentFromArray);
                        return new PropertyReferenceOperation(propertyReference.Property, propertyReference.ConstrainedToType, propertyArguments, instance, semanticModel: null, propertyReference.Syntax,
                                                               propertyReference.Type, IsImplicit(propertyReference));
                    case OperationKind.ArrayElementReference:
                        var arrayElementReference = (IArrayElementReferenceOperation)originalTarget;
                        instance = PopOperand();
                        ImmutableArray<IOperation> indices = PopArray(arrayElementReference.Indices);
                        return new ArrayElementReferenceOperation(instance, indices, semanticModel: null, originalTarget.Syntax, originalTarget.Type, IsImplicit(originalTarget));

                    case OperationKind.ImplicitIndexerReference:
                        var indexerReference = (IImplicitIndexerReferenceOperation)originalTarget;
                        instance = PopOperand();
                        IOperation index = PopOperand();
                        return new ImplicitIndexerReferenceOperation(instance, index, indexerReference.LengthSymbol, indexerReference.IndexerSymbol,
                                                                      semanticModel: null, originalTarget.Syntax, originalTarget.Type, IsImplicit(originalTarget));

                    case OperationKind.DynamicIndexerAccess:
                        var dynamicAccess = (DynamicIndexerAccessOperation)originalTarget;
                        instance = PopOperand();
                        ImmutableArray<IOperation> arguments = PopArray(dynamicAccess.Arguments);
                        return new DynamicIndexerAccessOperation(instance, arguments, dynamicAccess.ArgumentNames, dynamicAccess.ArgumentRefKinds, semanticModel: null,
                                                                  dynamicAccess.Syntax, dynamicAccess.Type, IsImplicit(dynamicAccess));
                    case OperationKind.DynamicMemberReference:
                        var dynamicReference = (IDynamicMemberReferenceOperation)originalTarget;
                        instance = dynamicReference.Instance != null ? PopOperand() : null;
                        return new DynamicMemberReferenceOperation(instance, dynamicReference.MemberName, dynamicReference.TypeArguments,
                                                                    dynamicReference.ContainingType, semanticModel: null, dynamicReference.Syntax,
                                                                    dynamicReference.Type, IsImplicit(dynamicReference));
                    default:
                        // Unlike in tryPushTarget, we assume that if this method is called, we were successful in pushing, so
                        // this must be one of the explicitly handled kinds
                        throw ExceptionUtilities.UnexpectedValue(originalTarget.Kind);
                }
            }

            static bool onlyContainsEmptyLeafNestedInitializers(IMemberInitializerOperation memberInitializer)
            {
                // Guard on the cases understood by addIndexes below
                if (memberInitializer.InitializedMember is IPropertyReferenceOperation
                    or IImplicitIndexerReferenceOperation
                    or IArrayElementReferenceOperation
                    or IDynamicIndexerAccessOperation
                    or IFieldReferenceOperation
                    or IEventReferenceOperation
                    || memberInitializer.InitializedMember is NoneOperation { ChildOperations: var children } && children.ToImmutableArray() is [IInstanceReferenceOperation, _])
                {
                    // Since there are no empty collection initializers, we don't need to differentiate object vs. collection initializers
                    return memberInitializer.Initializer is IObjectOrCollectionInitializerOperation initializer
                        && initializer.Initializers.All(e => e is IMemberInitializerOperation assignment && onlyContainsEmptyLeafNestedInitializers(assignment));
                }

                return false;
            }

            void addIndexes(IMemberInitializerOperation memberInitializer)
            {
                var lhs = memberInitializer.InitializedMember;
                // If we have an element access of the form `[arguments] = { ... }`, we'll evaluate `arguments` only
                if (lhs is IPropertyReferenceOperation propertyReference)
                {
                    foreach (var argument in propertyReference.Arguments)
                    {
                        if (argument is { ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation array })
                        {
                            Debug.Assert(array.Initializer is not null);

                            foreach (var element in array.Initializer.ElementValues)
                            {
                                AddStatement(Visit(element));
                            }
                        }
                        else
                        {
                            AddStatement(Visit(argument.Value));
                        }
                    }
                }
                else if (lhs is IImplicitIndexerReferenceOperation implicitIndexer)
                {
                    AddStatement(Visit(implicitIndexer.Argument));
                }
                else if (lhs is IArrayElementReferenceOperation arrayAccess)
                {
                    foreach (var index in arrayAccess.Indices)
                    {
                        AddStatement(Visit(index));
                    }
                }
                else if (lhs is IDynamicIndexerAccessOperation dynamicIndexerAccess)
                {
                    foreach (var argument in dynamicIndexerAccess.Arguments)
                    {
                        AddStatement(Visit(argument));
                    }
                }
                else if (lhs is NoneOperation { ChildOperations: var children } &&
                    children.ToImmutableArray() is [IInstanceReferenceOperation, var index])
                {
                    // Proper pointer element access support tracked by https://github.com/dotnet/roslyn/issues/21295
                    AddStatement(Visit(index));
                }
                else if (lhs is not (FieldReferenceOperation or EventReferenceOperation))
                {
                    throw ExceptionUtilities.UnexpectedValue(lhs.Kind);
                }

                // And any nested indexes
                foreach (var initializer in memberInitializer.Initializer.Initializers)
                {
                    addIndexes((IMemberInitializerOperation)initializer);
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
                    operation.Syntax, operation.Type, IsImplicit(operation));
            }

            ImplicitInstanceInfo savedCurrentImplicitInstance = _currentImplicitInstance;
            Debug.Assert(operation.Type is not null);
            _currentImplicitInstance = new ImplicitInstanceInfo((INamedTypeSymbol)operation.Type);
            Debug.Assert(_currentImplicitInstance.AnonymousTypePropertyValues is not null);

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
                    propertyReference.Instance.Syntax, propertyReference.Instance.Type, IsImplicit(propertyReference.Instance));
                IOperation visitedTarget = new PropertyReferenceOperation(propertyReference.Property, propertyReference.ConstrainedToType, ImmutableArray<IArgumentOperation>.Empty, visitedPropertyInstance,
                    semanticModel: null, propertyReference.Syntax, propertyReference.Type, IsImplicit(propertyReference));
                IOperation visitedValue = visitAndCaptureInitializer(propertyReference.Property, simpleAssignment.Value);
                var visitedAssignment = new SimpleAssignmentOperation(isRef: simpleAssignment.IsRef, visitedTarget, visitedValue,
                    semanticModel: null, simpleAssignment.Syntax, simpleAssignment.Type, simpleAssignment.GetConstantValue(), IsImplicit(simpleAssignment));
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
                operation.Syntax, operation.Type, IsImplicit(operation));

            IOperation visitAndCaptureInitializer(IPropertySymbol initializedProperty, IOperation initializer)
            {
                PushOperand(VisitRequired(initializer));
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

        public override IOperation? VisitLocalFunction(ILocalFunctionOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            RegionBuilder owner = CurrentRegionRequired;

            while (owner.IsStackSpillRegion)
            {
                Debug.Assert(owner.Enclosing != null);
                owner = owner.Enclosing;
            }

            owner.Add(operation.Symbol, operation);
            return FinishVisitingStatement(operation);
        }

        private IOperation? VisitLocalFunctionAsRoot(ILocalFunctionOperation operation)
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
            throw ExceptionUtilities.Unreachable();
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
            var visitedInitializer = (IArrayInitializerOperation?)Visit(operation.Initializer);
            ImmutableArray<IOperation> visitedDimensions = PopArray(operation.DimensionSizes);
            PopStackFrame(frame);
            return new ArrayCreationOperation(visitedDimensions, visitedInitializer, semanticModel: null,
                                               operation.Syntax, operation.Type, IsImplicit(operation));
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
                        PushOperand(VisitRequired(elementValue));
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
                return new ArrayInitializerOperation(builder.ToImmutableAndFree(), semanticModel: null, initializer.Syntax, IsImplicit(initializer));
            }
        }

        public override IOperation? VisitCollectionExpression(ICollectionExpressionOperation operation, int? argument)
        {
            EvalStackFrame frame = PushStackFrame();

            if (operation.ConstructArguments.Any(a => a is IArgumentOperation) && !operation.ConstructArguments.All(a => a is IArgumentOperation))
                throw ExceptionUtilities.UnexpectedValue("Mixed argument operations and non-argument operations in ConstructArguments");

            // If we bound successfully, we'll have an array of IArgumentOperation.  We want to call through to
            // VisitArguments to handle it properly.  So attempt to cast to that type first, but fallback to just
            // visiting the array of expressions if we didn't bind successfully.
            var arguments = operation.ConstructArguments.As<IArgumentOperation>();
            if (arguments.IsDefault)
            {
                VisitAndPushArray(operation.ConstructArguments);
            }
            else
            {
                VisitAndPushArguments(arguments, instancePushed: false);
            }

            var elements = VisitArray(
                operation.Elements,
                unwrapper: static (IOperation element) =>
                {
                    return element is ISpreadOperation spread ?
                        spread.Operand :
                        element;
                },
                wrapper: (IOperation operation, int index, ImmutableArray<IOperation> elements) =>
                {
                    return elements[index] is ISpreadOperation spread ?
                        new SpreadOperation(
                            operation,
                            elementType: spread.ElementType,
                            elementConversion: ((SpreadOperation)spread).ElementConversionConvertible,
                            semanticModel: null,
                            spread.Syntax,
                            IsImplicit(spread)) :
                        operation;
                });

            var creationArguments = arguments.IsDefault
                ? PopArray(operation.ConstructArguments)
                : ImmutableArray<IOperation>.CastUp(PopArray(arguments, RewriteArgumentFromArray));

            return PopStackFrame(frame, new CollectionExpressionOperation(
                operation.ConstructMethod,
                creationArguments,
                elements,
                semanticModel: null,
                operation.Syntax,
                operation.Type,
                IsImplicit(operation)));
        }

        public override IOperation? VisitSpread(ISpreadOperation operation, int? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitInstanceReference(IInstanceReferenceOperation operation, int? captureIdForResult)
        {
            switch (operation.ReferenceKind)
            {
                case InstanceReferenceKind.ImplicitReceiver:
                    // When we're in an object or collection initializer, we need to replace the instance reference with a reference to the object being initialized
                    Debug.Assert(operation.IsImplicit);

                    if (_currentImplicitInstance.ImplicitInstance != null)
                    {
                        return OperationCloner.CloneOperation(_currentImplicitInstance.ImplicitInstance);
                    }
                    else
                    {
                        Debug.Assert(operation.Parent is InvocationOperation { Parent: CollectionExpressionOperation ce } && ce.HasErrors(_compilation),
                            "Expected to reach this only in collection expression infinite chain cases.");
                        return MakeInvalidOperation(operation.Syntax, operation.Type, ImmutableArray<IOperation>.Empty);
                    }

                case InstanceReferenceKind.InterpolatedStringHandler:
                    AssertContainingContextIsForThisCreation(operation, assertArgumentContext: false);
                    return new FlowCaptureReferenceOperation(_currentInterpolatedStringHandlerCreationContext.HandlerPlaceholder, operation.Syntax, operation.Type, operation.GetConstantValue());

                default:
                    return new InstanceReferenceOperation(operation.ReferenceKind, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
            }
        }

        public override IOperation VisitDynamicInvocation(IDynamicInvocationOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();

            if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
            {
                var instance = ((IDynamicMemberReferenceOperation)operation.Operation).Instance;
                if (instance != null)
                {
                    PushOperand(VisitRequired(instance));
                }
            }
            else
            {
                PushOperand(VisitRequired(operation.Operation));
            }

            ImmutableArray<IOperation> rewrittenArguments = VisitArray(operation.Arguments);

            IOperation rewrittenOperation;
            if (operation.Operation.Kind == OperationKind.DynamicMemberReference)
            {
                var dynamicMemberReference = (IDynamicMemberReferenceOperation)operation.Operation;
                IOperation? rewrittenInstance = dynamicMemberReference.Instance != null ? PopOperand() : null;
                rewrittenOperation = new DynamicMemberReferenceOperation(rewrittenInstance, dynamicMemberReference.MemberName, dynamicMemberReference.TypeArguments,
                    dynamicMemberReference.ContainingType, semanticModel: null, dynamicMemberReference.Syntax, dynamicMemberReference.Type, IsImplicit(dynamicMemberReference));
            }
            else
            {
                rewrittenOperation = PopOperand();
            }

            PopStackFrame(frame);
            return new DynamicInvocationOperation(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, int? captureIdForResult)
        {
            PushOperand(VisitRequired(operation.Operation));

            ImmutableArray<IOperation> rewrittenArguments = VisitArray(operation.Arguments);
            IOperation rewrittenOperation = PopOperand();

            return new DynamicIndexerAccessOperation(rewrittenOperation, rewrittenArguments, ((HasDynamicArgumentsExpression)operation).ArgumentNames,
                ((HasDynamicArgumentsExpression)operation).ArgumentRefKinds, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, int? captureIdForResult)
        {
            return new DynamicMemberReferenceOperation(Visit(operation.Instance), operation.MemberName, operation.TypeArguments,
                operation.ContainingType, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, int? captureIdForResult)
        {
            (IOperation visitedTarget, IOperation visitedValue) = VisitPreservingTupleOperations(operation.Target, operation.Value);
            return new DeconstructionAssignmentOperation(visitedTarget, visitedValue, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
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
                PushOperand(VisitRequired(value));
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
                return new TupleOperation(elementBuilder.ToImmutableAndFree(), tuple.NaturalType, semanticModel: null, tuple.Syntax, tuple.Type, IsImplicit(tuple));
            }
            else
            {
                return PopOperand();
            }
        }

        public override IOperation VisitDeclarationExpression(IDeclarationExpressionOperation operation, int? captureIdForResult)
        {
            return new DeclarationExpressionOperation(VisitPreservingTupleOperations(operation.Expression), semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
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
            IOperation visitedRight = VisitRequired(right);
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
            VisitStatements(((Operation)operation).ChildOperations.ToImmutableArray());
            return new NoneOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        private IOperation VisitNoneOperationExpression(IOperation operation)
        {
            return PopStackFrame(PushStackFrame(),
                                 new NoneOperation(VisitArray(((Operation)operation).ChildOperations.ToImmutableArray()), semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation)));
        }

        public override IOperation? VisitInterpolatedStringHandlerCreation(IInterpolatedStringHandlerCreationOperation operation, int? captureIdForResult)
        {
            // We turn the interpolated string into a call to create the handler type, a series of append calls (potentially with branches, depending on the
            // handler semantics), and then evaluate to the handler flow capture temp.

            SpillEvalStack();
            int maxStackDepth = _evalStack.Count - 2;

#if DEBUG
            Debug.Assert(_evalStack[maxStackDepth + 1].frameOpt != null);
            if (_currentInterpolatedStringHandlerArgumentContext?.ApplicableCreationOperations.Contains(operation) == true)
            {
                for (int i = _currentInterpolatedStringHandlerArgumentContext.StartingStackDepth;
                     i < maxStackDepth;
                     i++)
                {
                    Debug.Assert(_evalStack[i].frameOpt == null);
                    Debug.Assert(_evalStack[i].operationOpt != null);
                }
            }
#endif

            RegionBuilder resultRegion = CurrentRegionRequired;
            var handlerCaptureId = captureIdForResult ?? GetNextCaptureId(resultRegion);

            var constructorRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime);
            EnterRegion(constructorRegion);

            BasicBlockBuilder? resultBlock = null;
            if (operation.HandlerCreationHasSuccessParameter || operation.HandlerAppendCallsReturnBool)
            {
                resultBlock = new BasicBlockBuilder(BasicBlockKind.Block);
            }

            // Any placeholders for arguments should have already been created, except for the out parameter if it exists.
            int outParameterFlowCapture = -1;
            IInterpolatedStringHandlerArgumentPlaceholderOperation? outParameterPlaceholder = null;

            if (operation.HandlerCreationHasSuccessParameter)
            {
                // Only successful constructor binds will have a trailing parameter
                Debug.Assert(operation.HandlerCreation is IObjectCreationOperation);
                outParameterFlowCapture = GetNextCaptureId(constructorRegion);
                var arguments = ((IObjectCreationOperation)operation.HandlerCreation).Arguments;
                IArgumentOperation? outParameterArgument = null;

                for (int i = arguments.Length - 1; i > 1; i--)
                {
                    if (arguments[i] is { Value: IInterpolatedStringHandlerArgumentPlaceholderOperation { PlaceholderKind: InterpolatedStringArgumentPlaceholderKind.TrailingValidityArgument } } arg)
                    {
                        outParameterArgument = arg;
                        break;
                    }
                }

                Debug.Assert(outParameterArgument is { Parameter: { RefKind: RefKind.Out, Type.SpecialType: SpecialType.System_Boolean } });
                outParameterPlaceholder = (IInterpolatedStringHandlerArgumentPlaceholderOperation)outParameterArgument.Value;
            }

            var previousHandlerContext = _currentInterpolatedStringHandlerCreationContext;
            _currentInterpolatedStringHandlerCreationContext = new InterpolatedStringHandlerCreationContext(operation, maxStackDepth, handlerCaptureId, outParameterFlowCapture);

            VisitAndCapture(operation.HandlerCreation, handlerCaptureId);

            if (operation.HandlerCreationHasSuccessParameter)
            {
                // Branch on the success parameter to the next block
                Debug.Assert(resultBlock != null);
                Debug.Assert(outParameterPlaceholder != null);
                Debug.Assert(outParameterFlowCapture != -1);

                // if (!outParameterFlowCapture) goto resultBlock;
                // else goto next block;
                ConditionalBranch(new FlowCaptureReferenceOperation(outParameterFlowCapture, outParameterPlaceholder.Syntax, outParameterPlaceholder.Type, constantValue: null), jumpIfTrue: false, resultBlock);
                _currentBasicBlock = null;
            }

            LeaveRegionsUpTo(resultRegion);

            var appendCalls = ArrayBuilder<IInterpolatedStringAppendOperation>.GetInstance();
            collectAppendCalls(operation, appendCalls);

            int appendCallsLength = appendCalls.Count;
            for (var i = 0; i < appendCallsLength; i++)
            {
                EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime));
                var appendCall = appendCalls[i];
                IOperation visitedAppendCall = VisitRequired(appendCall.AppendCall);
                if (operation.HandlerAppendCallsReturnBool)
                {
                    Debug.Assert(resultBlock != null);

                    if (i == appendCallsLength - 1)
                    {
                        // No matter the result, we're going to the result block next. So just visit the statement, and if the current block can be
                        // combined with the result block, the compaction machinery will take care of it
                        AddStatement(visitedAppendCall);
                    }
                    else
                    {
                        // if (!appendCall()) goto result else goto next block
                        ConditionalBranch(visitedAppendCall, jumpIfTrue: false, resultBlock);
                        _currentBasicBlock = null;
                    }
                }
                else
                {
                    AddStatement(visitedAppendCall);
                }

                LeaveRegionsUpTo(resultRegion);
            }

            if (resultBlock != null)
            {
                AppendNewBlock(resultBlock, linkToPrevious: true);
            }

            _currentInterpolatedStringHandlerCreationContext = previousHandlerContext;
            appendCalls.Free();
            return new FlowCaptureReferenceOperation(handlerCaptureId, operation.Syntax, operation.Type, operation.GetConstantValue());

            static void collectAppendCalls(IInterpolatedStringHandlerCreationOperation creation, ArrayBuilder<IInterpolatedStringAppendOperation> appendCalls)
            {
                if (creation.Content is IInterpolatedStringOperation interpolatedString)
                {
                    // Simple case
                    appendStringCalls(interpolatedString, appendCalls);
                    return;
                }

                var stack = ArrayBuilder<IInterpolatedStringAdditionOperation>.GetInstance();
                pushLeftNodes((IInterpolatedStringAdditionOperation)creation.Content, stack);

                while (stack.TryPop(out IInterpolatedStringAdditionOperation? currentAddition))
                {
                    switch (currentAddition.Left)
                    {
                        case IInterpolatedStringOperation interpolatedString1:
                            appendStringCalls(interpolatedString1, appendCalls);
                            break;
                        case IInterpolatedStringAdditionOperation:
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(currentAddition.Left.Kind);
                    }

                    switch (currentAddition.Right)
                    {
                        case IInterpolatedStringOperation interpolatedString1:
                            appendStringCalls(interpolatedString1, appendCalls);
                            break;
                        case IInterpolatedStringAdditionOperation additionOperation:
                            pushLeftNodes(additionOperation, stack);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(currentAddition.Left.Kind);
                    }
                }

                stack.Free();
                return;

                static void appendStringCalls(IInterpolatedStringOperation interpolatedString, ArrayBuilder<IInterpolatedStringAppendOperation> appendCalls)
                {
                    foreach (var part in interpolatedString.Parts)
                    {
                        appendCalls.Add((IInterpolatedStringAppendOperation)part);
                    }
                }

                static void pushLeftNodes(IInterpolatedStringAdditionOperation addition, ArrayBuilder<IInterpolatedStringAdditionOperation> stack)
                {
                    IInterpolatedStringAdditionOperation? current = addition;
                    do
                    {
                        stack.Push(current);
                        current = current.Left as IInterpolatedStringAdditionOperation;
                    }
                    while (current != null);
                }
            }
        }

        public override IOperation? VisitInterpolatedStringAddition(IInterpolatedStringAdditionOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation? VisitInterpolatedStringAppend(IInterpolatedStringAppendOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation? VisitInterpolatedStringHandlerArgumentPlaceholder(IInterpolatedStringHandlerArgumentPlaceholderOperation operation, int? captureIdForResult)
        {
            switch (operation.PlaceholderKind)
            {
                case InterpolatedStringArgumentPlaceholderKind.TrailingValidityArgument:
                    AssertContainingContextIsForThisCreation(operation, assertArgumentContext: false);

                    return new FlowCaptureReferenceOperation(_currentInterpolatedStringHandlerCreationContext.OutPlaceholder, operation.Syntax, operation.Type, operation.GetConstantValue(), isInitialization: true);

                case InterpolatedStringArgumentPlaceholderKind.CallsiteReceiver:
                    AssertContainingContextIsForThisCreation(operation, assertArgumentContext: true);
                    Debug.Assert(_currentInterpolatedStringHandlerArgumentContext != null);
                    if (_currentInterpolatedStringHandlerArgumentContext.HasReceiver && tryGetArgumentOrReceiver(-1) is IOperation receiverCapture)
                    {
                        Debug.Assert(receiverCapture is IFlowCaptureReferenceOperation);
                        return OperationCloner.CloneOperation(receiverCapture);
                    }
                    else
                    {
                        return new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), isImplicit: true);
                    }

                case InterpolatedStringArgumentPlaceholderKind.CallsiteArgument:
                    AssertContainingContextIsForThisCreation(operation, assertArgumentContext: true);
                    Debug.Assert(_currentInterpolatedStringHandlerArgumentContext != null);
                    if (tryGetArgumentOrReceiver(operation.ArgumentIndex) is IOperation argumentCapture)
                    {
                        Debug.Assert(argumentCapture is IFlowCaptureReferenceOperation or IDiscardOperation);
                        return OperationCloner.CloneOperation(argumentCapture);
                    }
                    else
                    {
                        return new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), isImplicit: true);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(operation.PlaceholderKind);
            }

            IOperation? tryGetArgumentOrReceiver(int argumentIndex)
            {

                if (_currentInterpolatedStringHandlerArgumentContext.HasReceiver)
                {
                    argumentIndex++;
                }

                int targetStackDepth = _currentInterpolatedStringHandlerArgumentContext.StartingStackDepth + argumentIndex;

                Debug.Assert(_evalStack.Count > _currentInterpolatedStringHandlerCreationContext.MaximumStackDepth);

                if (targetStackDepth > _currentInterpolatedStringHandlerCreationContext.MaximumStackDepth
                    || targetStackDepth >= _evalStack.Count)
                {
                    return null;
                }

                return _evalStack[targetStackDepth].operationOpt;
            }
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
                    PushOperand(VisitRequired(interpolation.Expression));

                    if (interpolation.Alignment != null)
                    {
                        PushOperand(VisitRequired(interpolation.Alignment));
                    }
                }
            }

            var partsBuilder = ArrayBuilder<IInterpolatedStringContentOperation>.GetInstance(operation.Parts.Length);
            for (int i = operation.Parts.Length - 1; i >= 0; i--)
            {
                IInterpolatedStringContentOperation element = operation.Parts[i];
                IInterpolatedStringContentOperation rewrittenElement;
                switch (element)
                {
                    case IInterpolationOperation interpolation:
                        IOperation? rewrittenFormatString;
                        if (interpolation.FormatString != null)
                        {
                            Debug.Assert(interpolation.FormatString is ILiteralOperation or IConversionOperation { Operand: ILiteralOperation });
                            rewrittenFormatString = VisitRequired(interpolation.FormatString, argument: null);
                        }
                        else
                        {
                            rewrittenFormatString = null;
                        }

                        var rewrittenAlignment = interpolation.Alignment != null ? PopOperand() : null;
                        var rewrittenExpression = PopOperand();
                        rewrittenElement = new InterpolationOperation(rewrittenExpression, rewrittenAlignment, rewrittenFormatString, semanticModel: null, element.Syntax, IsImplicit(element));
                        break;
                    case IInterpolatedStringTextOperation interpolatedStringText:
                        Debug.Assert(interpolatedStringText.Text is ILiteralOperation or IConversionOperation { Operand: ILiteralOperation });
                        var rewrittenInterpolationText = VisitRequired(interpolatedStringText.Text, argument: null);
                        rewrittenElement = new InterpolatedStringTextOperation(rewrittenInterpolationText, semanticModel: null, element.Syntax, IsImplicit(element));
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(element.Kind);

                }

                partsBuilder.Add(rewrittenElement);
            }

            partsBuilder.ReverseContents();
            PopStackFrame(frame);
            return new InterpolatedStringOperation(partsBuilder.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitInterpolation(IInterpolationOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitNameOf(INameOfOperation operation, int? captureIdForResult)
        {
            Debug.Assert(operation.GetConstantValue() != null);
            return new LiteralOperation(semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitLiteral(ILiteralOperation operation, int? captureIdForResult)
        {
            return new LiteralOperation(semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation? VisitUtf8String(IUtf8StringOperation operation, int? captureIdForResult)
        {
            return new Utf8StringOperation(operation.Value, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitLocalReference(ILocalReferenceOperation operation, int? captureIdForResult)
        {
            return new LocalReferenceOperation(operation.Local, operation.IsDeclaration, semanticModel: null, operation.Syntax,
                                                operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitParameterReference(IParameterReferenceOperation operation, int? captureIdForResult)
        {
            return new ParameterReferenceOperation(operation.Parameter, semanticModel: null, operation.Syntax,
                                                   operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitFieldReference(IFieldReferenceOperation operation, int? captureIdForResult)
        {
            IOperation? visitedInstance = operation.Field.IsStatic ? null : Visit(operation.Instance);
            return new FieldReferenceOperation(operation.Field, operation.IsDeclaration, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitMethodReference(IMethodReferenceOperation operation, int? captureIdForResult)
        {
            IOperation? visitedInstance = operation.Method.IsStatic ? null : Visit(operation.Instance);
            return new MethodReferenceOperation(operation.Method, operation.ConstrainedToType, operation.IsVirtual, visitedInstance, semanticModel: null,
                                                operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitPropertyReference(IPropertyReferenceOperation operation, int? captureIdForResult)
        {
            // Check if this is an anonymous type property reference with an implicit receiver within an anonymous object initializer.
            if (operation.Instance is IInstanceReferenceOperation instanceReference &&
                instanceReference.ReferenceKind == InstanceReferenceKind.ImplicitReceiver &&
                operation.Property.ContainingType is { } containingType &&
                containingType.IsAnonymousType &&
                containingType == _currentImplicitInstance.AnonymousType)
            {
                Debug.Assert(_currentImplicitInstance.AnonymousTypePropertyValues is not null);
                if (_currentImplicitInstance.AnonymousTypePropertyValues.TryGetValue(operation.Property, out IOperation? captured))
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
            IOperation? instance = operation.Property.IsStatic ? null : operation.Instance;
            (IOperation? visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) = VisitInstanceWithArguments(instance, operation.Arguments);
            PopStackFrame(frame);
            return new PropertyReferenceOperation(operation.Property, operation.ConstrainedToType, visitedArguments, visitedInstance, semanticModel: null,
                                                  operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitEventReference(IEventReferenceOperation operation, int? captureIdForResult)
        {
            IOperation? visitedInstance = operation.Event.IsStatic ? null : Visit(operation.Instance);
            return new EventReferenceOperation(operation.Event, operation.ConstrainedToType, visitedInstance, semanticModel: null,
                                               operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitTypeOf(ITypeOfOperation operation, int? captureIdForResult)
        {
            return new TypeOfOperation(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitParenthesized(IParenthesizedOperation operation, int? captureIdForResult)
        {
            return new ParenthesizedOperation(VisitRequired(operation.Operand), semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitAwait(IAwaitOperation operation, int? captureIdForResult)
        {
            return new AwaitOperation(VisitRequired(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitSizeOf(ISizeOfOperation operation, int? captureIdForResult)
        {
            return new SizeOfOperation(operation.TypeOperand, semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitStop(IStopOperation operation, int? captureIdForResult)
        {
            return new StopOperation(semanticModel: null, operation.Syntax, IsImplicit(operation));
        }

        public override IOperation VisitIsType(IIsTypeOperation operation, int? captureIdForResult)
        {
            return new IsTypeOperation(VisitRequired(operation.ValueOperand), operation.TypeOperand, operation.IsNegated, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation? VisitParameterInitializer(IParameterInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            var parameterRef = new ParameterReferenceOperation(operation.Parameter, semanticModel: null,
                operation.Syntax, operation.Parameter.Type, isImplicit: true);
            VisitInitializer(rewrittenTarget: parameterRef, initializer: operation);
            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitFieldInitializer(IFieldInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            foreach (IFieldSymbol fieldSymbol in operation.InitializedFields)
            {
                IInstanceReferenceOperation? instance = fieldSymbol.IsStatic ?
                    null :
                    new InstanceReferenceOperation(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
                        operation.Syntax, fieldSymbol.ContainingType, isImplicit: true);
                var fieldRef = new FieldReferenceOperation(fieldSymbol, isDeclaration: false, instance, semanticModel: null,
                    operation.Syntax, fieldSymbol.Type, constantValue: null, isImplicit: true);
                VisitInitializer(rewrittenTarget: fieldRef, initializer: operation);
            }

            return FinishVisitingStatement(operation);
        }

        public override IOperation? VisitPropertyInitializer(IPropertyInitializerOperation operation, int? captureIdForResult)
        {
            StartVisitingStatement(operation);

            foreach (IPropertySymbol propertySymbol in operation.InitializedProperties)
            {
                var instance = propertySymbol.IsStatic ?
                    null :
                    new InstanceReferenceOperation(InstanceReferenceKind.ContainingTypeInstance, semanticModel: null,
                        operation.Syntax, propertySymbol.ContainingType, isImplicit: true);

                ImmutableArray<IArgumentOperation> arguments;
                if (!propertySymbol.Parameters.IsEmpty)
                {
                    // Must be an error case of initializing a property with parameters.
                    var builder = ArrayBuilder<IArgumentOperation>.GetInstance(propertySymbol.Parameters.Length);
                    foreach (var parameter in propertySymbol.Parameters)
                    {
                        var value = new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null,
                            operation.Syntax, parameter.Type, constantValue: null, isImplicit: true);
                        var argument = new ArgumentOperation(ArgumentKind.Explicit, parameter, value,
                                                             inConversion: OperationFactory.IdentityConversion,
                                                             outConversion: OperationFactory.IdentityConversion,
                                                             semanticModel: null, operation.Syntax, isImplicit: true);
                        builder.Add(argument);
                    }

                    arguments = builder.ToImmutableAndFree();
                }
                else
                {
                    arguments = ImmutableArray<IArgumentOperation>.Empty;
                }

                IOperation propertyRef = new PropertyReferenceOperation(propertySymbol, constrainedToType: null, arguments, instance,
                    semanticModel: null, operation.Syntax, propertySymbol.Type, isImplicit: true);
                VisitInitializer(rewrittenTarget: propertyRef, initializer: operation);
            }

            return FinishVisitingStatement(operation);
        }

        private void VisitInitializer(IOperation rewrittenTarget, ISymbolInitializerOperation initializer)
        {
            EnterRegion(new RegionBuilder(ControlFlowRegionKind.LocalLifetime, locals: initializer.Locals));

            EvalStackFrame frame = PushStackFrame();
            var assignment = new SimpleAssignmentOperation(isRef: false, rewrittenTarget, VisitRequired(initializer.Value), semanticModel: null,
                initializer.Syntax, rewrittenTarget.Type, constantValue: null, isImplicit: true);
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
            IEventReferenceOperation? eventReference = getEventReference();
            if (eventReference != null)
            {
                // Preserve the IEventReferenceOperation.
                var eventReferenceInstance = eventReference.Event.IsStatic ? null : eventReference.Instance;
                if (eventReferenceInstance != null)
                {
                    PushOperand(VisitRequired(eventReferenceInstance));
                }

                visitedHandler = VisitRequired(operation.HandlerValue);

                IOperation? visitedInstance = eventReferenceInstance == null ? null : PopOperand();
                visitedEventReference = new EventReferenceOperation(eventReference.Event, eventReference.ConstrainedToType, visitedInstance,
                    semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, IsImplicit(operation.EventReference));
            }
            else
            {
                Debug.Assert(operation.EventReference != null);

                PushOperand(VisitRequired(operation.EventReference));
                visitedHandler = VisitRequired(operation.HandlerValue);
                visitedEventReference = PopOperand();
            }

            PopStackFrame(frame);
            return new EventAssignmentOperation(visitedEventReference, visitedHandler, operation.Adds, semanticModel: null,
                operation.Syntax, operation.Type, IsImplicit(operation));

            IEventReferenceOperation? getEventReference()
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

            (IOperation? visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments) =
                VisitInstanceWithArguments(operation.EventReference.Event.IsStatic ? null : operation.EventReference.Instance, operation.Arguments);
            var visitedEventReference = new EventReferenceOperation(operation.EventReference.Event, operation.EventReference.ConstrainedToType, visitedInstance,
                semanticModel: null, operation.EventReference.Syntax, operation.EventReference.Type, IsImplicit(operation.EventReference));

            PopStackFrame(frame);
            return FinishVisitingStatement(operation, new RaiseEventOperation(visitedEventReference, visitedArguments, semanticModel: null,
                                                                              operation.Syntax, IsImplicit(operation)));
        }

        public override IOperation VisitAddressOf(IAddressOfOperation operation, int? captureIdForResult)
        {
            return new AddressOfOperation(VisitRequired(operation.Reference), semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, int? captureIdForResult)
        {
            return new IncrementOrDecrementOperation(operation.IsPostfix, operation.IsLifted, operation.IsChecked, VisitRequired(operation.Target),
                                                     operation.OperatorMethod, operation.ConstrainedToType,
                                                     operation.Kind, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitDiscardOperation(IDiscardOperation operation, int? captureIdForResult)
        {
            return new DiscardOperation(operation.DiscardSymbol, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitDiscardPattern(IDiscardPatternOperation pat, int? captureIdForResult)
        {
            return new DiscardPatternOperation(pat.InputType, pat.NarrowedType, semanticModel: null, pat.Syntax, IsImplicit(pat));
        }

        public override IOperation VisitOmittedArgument(IOmittedArgumentOperation operation, int? captureIdForResult)
        {
            return new OmittedArgumentOperation(semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
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
            return new PlaceholderOperation(operation.PlaceholderKind, semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation? VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation operation, int? argument)
        {
            // Leave collection builder element placeholder alone. It itself doesn't affect flow control.
            return new CollectionExpressionElementsPlaceholderOperation(semanticModel: null, operation.Syntax, operation.Type, operation.IsImplicit);
        }

        public override IOperation VisitConversion(IConversionOperation operation, int? captureIdForResult)
        {
            return new ConversionOperation(VisitRequired(operation.Operand), ((ConversionOperation)operation).ConversionConvertible, operation.IsTryCast, operation.IsChecked, semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitDefaultValue(IDefaultValueOperation operation, int? captureIdForResult)
        {
            return new DefaultValueOperation(semanticModel: null, operation.Syntax, operation.Type, operation.GetConstantValue(), IsImplicit(operation));
        }

        public override IOperation VisitIsPattern(IIsPatternOperation operation, int? captureIdForResult)
        {
            EvalStackFrame frame = PushStackFrame();
            PushOperand(VisitRequired(operation.Value));
            var visitedPattern = (IPatternOperation)VisitRequired(operation.Pattern);
            IOperation visitedValue = PopOperand();
            PopStackFrame(frame);
            return new IsPatternOperation(visitedValue, visitedPattern, semanticModel: null,
                                          operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitInvalid(IInvalidOperation operation, int? captureIdForResult)
        {
            var children = ArrayBuilder<IOperation>.GetInstance();
            children.AddRange(((InvalidOperation)operation).Children);

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
                    PushOperand(VisitRequired(argument));
                }

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    children[i] = PopOperand();
                }

                PopStackFrame(argumentsFrame);

                IOperation initializedInstance = new InvalidOperation(children.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type,
                                                                      operation.GetConstantValue(), IsImplicit(operation));

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
                return new InvalidOperation(ImmutableArray<IOperation>.Empty, semanticModel: null, invalidOperation.Syntax, invalidOperation.Type, invalidOperation.GetConstantValue(), IsImplicit(invalidOperation));
            }

            IOperation visitInvalidOperationExpression(IInvalidOperation invalidOperation)
            {
                return PopStackFrame(PushStackFrame(),
                                     new InvalidOperation(VisitArray(children.ToImmutableAndFree()), semanticModel: null,
                                                                 invalidOperation.Syntax, invalidOperation.Type, invalidOperation.GetConstantValue(), IsImplicit(operation)));
            }
        }

        public override IOperation? VisitReDim(IReDimOperation operation, int? argument)
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
                    semanticModel: null, operation.Syntax, isImplicit);
                AddStatement(visitedReDimOperation);
                PopStackFrameAndLeaveRegion(frame);
            }

            return FinishVisitingStatement(operation);

            IReDimClauseOperation visitReDimClause(IReDimClauseOperation clause)
            {
                PushOperand(VisitRequired(clause.Operand));
                var visitedDimensionSizes = VisitArray(clause.DimensionSizes);
                var visitedOperand = PopOperand();
                return new ReDimClauseOperation(visitedOperand, visitedDimensionSizes, semanticModel: null, clause.Syntax, IsImplicit(clause));
            }
        }

        public override IOperation VisitReDimClause(IReDimClauseOperation operation, int? argument)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitTranslatedQuery(ITranslatedQueryOperation operation, int? captureIdForResult)
        {
            return new TranslatedQueryOperation(VisitRequired(operation.Operation), semanticModel: null, operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitConstantPattern(IConstantPatternOperation operation, int? captureIdForResult)
        {
            return new ConstantPatternOperation(VisitRequired(operation.Value), operation.InputType, operation.NarrowedType, semanticModel: null,
                syntax: operation.Syntax, isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitRelationalPattern(IRelationalPatternOperation operation, int? argument)
        {
            return new RelationalPatternOperation(
                operatorKind: operation.OperatorKind,
                value: VisitRequired(operation.Value),
                inputType: operation.InputType,
                narrowedType: operation.NarrowedType,
                semanticModel: null,
                syntax: operation.Syntax,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitBinaryPattern(IBinaryPatternOperation operation, int? argument)
        {
            if (operation.LeftPattern is not IBinaryPatternOperation)
            {
                return createOperation(this, operation, (IPatternOperation)VisitRequired(operation.LeftPattern));
            }

            // Use a manual stack to avoid overflowing on deeply-nested binary patterns
            var stack = ArrayBuilder<IBinaryPatternOperation>.GetInstance();
            IBinaryPatternOperation? current = operation;

            do
            {
                stack.Push(current);
                current = current.LeftPattern as IBinaryPatternOperation;
            } while (current != null);

            current = stack.Pop();
            var result = (IPatternOperation)VisitRequired(current.LeftPattern);
            do
            {
                result = createOperation(this, current, result);
            } while (stack.TryPop(out current));

            stack.Free();
            return result;

            static BinaryPatternOperation createOperation(ControlFlowGraphBuilder @this, IBinaryPatternOperation operation, IPatternOperation left)
            {
                return new BinaryPatternOperation(
                            operatorKind: operation.OperatorKind,
                            leftPattern: left,
                            rightPattern: (IPatternOperation)@this.VisitRequired(operation.RightPattern),
                            inputType: operation.InputType,
                            narrowedType: operation.NarrowedType,
                            semanticModel: null,
                            syntax: operation.Syntax,
                            isImplicit: @this.IsImplicit(operation));
            }
        }

        public override IOperation VisitNegatedPattern(INegatedPatternOperation operation, int? argument)
        {
            return new NegatedPatternOperation(
                pattern: (IPatternOperation)VisitRequired(operation.Pattern),
                inputType: operation.InputType,
                narrowedType: operation.NarrowedType,
                semanticModel: null,
                syntax: operation.Syntax,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitTypePattern(ITypePatternOperation operation, int? argument)
        {
            return new TypePatternOperation(
                matchedType: operation.MatchedType,
                inputType: operation.InputType,
                narrowedType: operation.NarrowedType,
                semanticModel: null,
                syntax: operation.Syntax,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitDeclarationPattern(IDeclarationPatternOperation operation, int? captureIdForResult)
        {
            return new DeclarationPatternOperation(
                operation.MatchedType,
                operation.MatchesNull,
                operation.DeclaredSymbol,
                operation.InputType,
                operation.NarrowedType,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitSlicePattern(ISlicePatternOperation operation, int? argument)
        {
            return new SlicePatternOperation(
                operation.SliceSymbol,
                (IPatternOperation?)Visit(operation.Pattern),
                operation.InputType,
                operation.NarrowedType,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitListPattern(IListPatternOperation operation, int? argument)
        {
            return new ListPatternOperation(
                operation.LengthSymbol,
                operation.IndexerSymbol,
                operation.Patterns.SelectAsArray((p, @this) => (IPatternOperation)@this.VisitRequired(p), this),
                operation.DeclaredSymbol,
                operation.InputType,
                operation.NarrowedType,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitRecursivePattern(IRecursivePatternOperation operation, int? argument)
        {
            return new RecursivePatternOperation(
                operation.MatchedType,
                operation.DeconstructSymbol,
                operation.DeconstructionSubpatterns.SelectAsArray((p, @this) => (IPatternOperation)@this.VisitRequired(p), this),
                operation.PropertySubpatterns.SelectAsArray((p, @this) => (IPropertySubpatternOperation)@this.VisitRequired(p), this),
                operation.DeclaredSymbol,
                operation.InputType,
                operation.NarrowedType,
                semanticModel: null,
                operation.Syntax,
                IsImplicit(operation));
        }

        public override IOperation VisitPropertySubpattern(IPropertySubpatternOperation operation, int? argument)
        {
            return new PropertySubpatternOperation(
                VisitRequired(operation.Member),
                (IPatternOperation)VisitRequired(operation.Pattern),
                semanticModel: null,
                syntax: operation.Syntax,
                isImplicit: IsImplicit(operation));
        }

        public override IOperation VisitDelegateCreation(IDelegateCreationOperation operation, int? captureIdForResult)
        {
            return new DelegateCreationOperation(VisitRequired(operation.Target), semanticModel: null,
                operation.Syntax, operation.Type, IsImplicit(operation));
        }

        public override IOperation VisitRangeOperation(IRangeOperation operation, int? argument)
        {
            if (operation.LeftOperand is object)
            {
                PushOperand(VisitRequired(operation.LeftOperand));
            }

            IOperation? visitedRightOperand = null;
            if (operation.RightOperand is object)
            {
                visitedRightOperand = Visit(operation.RightOperand);
            }

            IOperation? visitedLeftOperand = operation.LeftOperand is null ? null : PopOperand();

            return new RangeOperation(visitedLeftOperand, visitedRightOperand, operation.IsLifted, operation.Method, semanticModel: null, operation.Syntax, operation.Type, isImplicit: IsImplicit(operation));
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
            RegionBuilder resultCaptureRegion = CurrentRegionRequired;
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
                    var visitedPattern = (IPatternOperation)VisitRequired(arm.Pattern);
                    var patternTest = new IsPatternOperation(
                        OperationCloner.CloneOperation(capturedInput), visitedPattern, semanticModel: null,
                        arm.Syntax, booleanType, IsImplicit(arm));
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
                (IMethodSymbol?)(_compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor) ??
                                _compilation.CommonGetWellKnownTypeMember(WellKnownMember.System_InvalidOperationException__ctor))?.GetISymbol();
            var makeException = (matchFailureCtor is null)
                ? MakeInvalidOperation(operation.Syntax, type: _compilation.GetSpecialType(SpecialType.System_Object), ImmutableArray<IOperation>.Empty)
                : new ObjectCreationOperation(
                    matchFailureCtor, initializer: null, ImmutableArray<IArgumentOperation>.Empty, semanticModel: null, operation.Syntax,
                    type: matchFailureCtor.ContainingType, constantValue: null, isImplicit: true);
            LinkThrowStatement(makeException);
            _currentBasicBlock = null;

            // afterSwitch:
            AppendNewBlock(afterSwitch, linkToPrevious: false);

            // result = captureOutput
            return GetCaptureReference(captureOutput, operation);
        }

        private void VisitUsingVariableDeclarationOperation(IUsingDeclarationOperation operation, ReadOnlySpan<IOperation> statements)
        {
            IOperation? saveCurrentStatement = _currentStatement;
            _currentStatement = operation;
            StartVisitingStatement(operation);

            // a using statement introduces a 'logical' block after declaration, we synthesize one here in order to analyze it like a regular using. Don't include
            // local functions in this block: they still belong in the containing block. We'll visit any local functions in the list after we visit the statements
            // in this block.
            ArrayBuilder<IOperation> statementsBuilder = ArrayBuilder<IOperation>.GetInstance(statements.Length);
            ArrayBuilder<IOperation>? localFunctionsBuilder = null;

            foreach (var statement in statements)
            {
                if (statement.Kind == OperationKind.LocalFunction)
                {
                    (localFunctionsBuilder ??= ArrayBuilder<IOperation>.GetInstance()).Add(statement);
                }
                else
                {
                    statementsBuilder.Add(statement);
                }
            }

            BlockOperation logicalBlock = BlockOperation.CreateTemporaryBlock(statementsBuilder.ToImmutableAndFree(), ((Operation)operation).OwningSemanticModel!, operation.Syntax);

            DisposeOperationInfo disposeInfo = ((UsingDeclarationOperation)operation).DisposeInfo;

            HandleUsingOperationParts(
                resources: operation.DeclarationGroup,
                body: logicalBlock,
                disposeInfo.DisposeMethod,
                disposeInfo.DisposeArguments,
                locals: ImmutableArray<ILocalSymbol>.Empty,
                isAsynchronous: operation.IsAsynchronous);

            FinishVisitingStatement(operation);
            _currentStatement = saveCurrentStatement;

            if (localFunctionsBuilder != null)
            {
                VisitStatements(localFunctionsBuilder.ToImmutableAndFree());
            }
        }

        public IOperation? Visit(IOperation? operation)
        {
            // We should never be revisiting nodes we've already visited, and we don't set SemanticModel in this builder.
            Debug.Assert(operation == null || ((Operation)operation).OwningSemanticModel!.Compilation == _compilation);
            return Visit(operation, argument: null);
        }

        [return: NotNullIfNotNull(nameof(operation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IOperation? VisitRequired(IOperation? operation, int? argument = null)
        {
            Debug.Assert(operation == null || ((Operation)operation).OwningSemanticModel!.Compilation == _compilation);
            var result = Visit(operation, argument);
            Debug.Assert((result == null) == (operation == null));
            return result;
        }

        [return: NotNullIfNotNull(nameof(operation))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IOperation? BaseVisitRequired(IOperation? operation, int? argument)
        {
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            _recursionDepth++;
            var result = base.Visit(operation, argument);
            Debug.Assert((result == null) == (operation == null));
            _recursionDepth--;

            return result;
        }

        public override IOperation? Visit(IOperation? operation, int? argument)
        {
            if (operation == null)
            {
                return null;
            }

            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            _recursionDepth++;
            var result = PopStackFrame(PushStackFrame(), base.Visit(operation, argument));
            _recursionDepth--;

            return result;
        }

        public override IOperation DefaultVisit(IOperation operation, int? captureIdForResult)
        {
            // this should never reach, otherwise, there is missing override for IOperation type
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitArgument(IArgumentOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitUsingDeclaration(IUsingDeclarationOperation operation, int? captureIdForResult)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override IOperation VisitWith(IWithOperation operation, int? captureIdForResult)
        {
            if (operation.Type!.IsAnonymousType)
            {
                return handleAnonymousTypeWithExpression((WithOperation)operation, captureIdForResult);
            }

            EvalStackFrame frame = PushStackFrame();
            // Initializer is removed from the tree and turned into a series of statements that assign to the cloned instance
            IOperation visitedInstance = VisitRequired(operation.Operand);
            IOperation cloned;
            if (operation.Type.IsValueType)
            {
                cloned = visitedInstance;
            }
            else
            {
                cloned = operation.CloneMethod is null
                    ? MakeInvalidOperation(visitedInstance.Type, visitedInstance)
                    : new InvocationOperation(operation.CloneMethod, constrainedToType: null, visitedInstance,
                        isVirtual: true, arguments: ImmutableArray<IArgumentOperation>.Empty,
                        semanticModel: null, operation.Syntax, operation.Type, isImplicit: true);
            }

            return PopStackFrame(frame, HandleObjectOrCollectionInitializer(operation.Initializer, cloned));

            // For `old with { Property = ... }` we're going to do the same as `new { Property = ..., OtherProperty = old.OtherProperty }`
            IOperation handleAnonymousTypeWithExpression(WithOperation operation, int? captureIdForResult)
            {
                Debug.Assert(operation.Type!.IsAnonymousType);
                SpillEvalStack(); // before entering a new region, we ensure that anything that needs spilling was spilled

                // The outer region holds captures for all the values for the anonymous object creation
                var outerCaptureRegion = CurrentRegionRequired;

                // The inner region holds the capture for the operand (ie. old value)
                var innerCaptureRegion = new RegionBuilder(ControlFlowRegionKind.LocalLifetime);
                EnterRegion(innerCaptureRegion);

                var initializers = operation.Initializer.Initializers;

                var properties = operation.Type.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Property)
                    .Select(m => (IPropertySymbol)m);

                int oldValueCaptureId;
                if (setsAllProperties(initializers, properties))
                {
                    // Avoid capturing the old value since we won't need it
                    oldValueCaptureId = -1;
                    AddStatement(VisitRequired(operation.Operand));
                }
                else
                {
                    oldValueCaptureId = GetNextCaptureId(innerCaptureRegion);
                    VisitAndCapture(operation.Operand, oldValueCaptureId);
                }
                // calls to Visit may enter regions, so we reset things
                LeaveRegionsUpTo(innerCaptureRegion);

                var explicitProperties = new Dictionary<IPropertySymbol, IOperation>(SymbolEqualityComparer.IgnoreAll);
                var initializerBuilder = ArrayBuilder<IOperation>.GetInstance(initializers.Length);

                // Visit and capture all the values, and construct assignments using capture references
                foreach (IOperation initializer in initializers)
                {
                    if (initializer is not ISimpleAssignmentOperation simpleAssignment)
                    {
                        AddStatement(VisitRequired(initializer));
                        continue;
                    }

                    if (simpleAssignment.Target.Kind != OperationKind.PropertyReference)
                    {
                        Debug.Assert(simpleAssignment.Target is InvalidOperation);
                        AddStatement(VisitRequired(simpleAssignment.Value));
                        continue;
                    }

                    var propertyReference = (IPropertyReferenceOperation)simpleAssignment.Target;

                    Debug.Assert(propertyReference != null);
                    Debug.Assert(propertyReference.Arguments.IsEmpty);
                    Debug.Assert(propertyReference.Instance != null);
                    Debug.Assert(propertyReference.Instance.Kind == OperationKind.InstanceReference);
                    Debug.Assert(((IInstanceReferenceOperation)propertyReference.Instance).ReferenceKind == InstanceReferenceKind.ImplicitReceiver);

                    var property = propertyReference.Property;
                    if (explicitProperties.ContainsKey(property))
                    {
                        AddStatement(VisitRequired(simpleAssignment.Value));
                        continue;
                    }

                    int valueCaptureId = GetNextCaptureId(outerCaptureRegion);
                    VisitAndCapture(simpleAssignment.Value, valueCaptureId);
                    LeaveRegionsUpTo(innerCaptureRegion);
                    var valueCaptureRef = new FlowCaptureReferenceOperation(valueCaptureId, operation.Operand.Syntax,
                        operation.Operand.Type, constantValue: operation.Operand.GetConstantValue());

                    var assignment = makeAssignment(property, valueCaptureRef, operation);

                    explicitProperties.Add(property, assignment);
                }

                // Make a sequence for all properties (in order), constructing assignments for the implicitly set properties
                var type = (INamedTypeSymbol)operation.Type;
                foreach (IPropertySymbol property in properties)
                {
                    if (explicitProperties.TryGetValue(property, out var assignment))
                    {
                        initializerBuilder.Add(assignment);
                    }
                    else
                    {
                        Debug.Assert(oldValueCaptureId >= 0);

                        // `oldInstance`
                        var oldInstance = new FlowCaptureReferenceOperation(oldValueCaptureId, operation.Operand.Syntax,
                            operation.Operand.Type, constantValue: operation.Operand.GetConstantValue());

                        // `oldInstance.Property`
                        var visitedValue = new PropertyReferenceOperation(property, constrainedToType: null, ImmutableArray<IArgumentOperation>.Empty, oldInstance,
                            semanticModel: null, operation.Syntax, property.Type, isImplicit: true);

                        int extraValueCaptureId = GetNextCaptureId(outerCaptureRegion);
                        AddStatement(new FlowCaptureOperation(extraValueCaptureId, operation.Syntax, visitedValue));

                        var extraValueCaptureRef = new FlowCaptureReferenceOperation(extraValueCaptureId, operation.Operand.Syntax,
                            operation.Operand.Type, constantValue: operation.Operand.GetConstantValue());

                        assignment = makeAssignment(property, extraValueCaptureRef, operation);
                        initializerBuilder.Add(assignment);
                    }
                }
                LeaveRegionsUpTo(outerCaptureRegion);

                return new AnonymousObjectCreationOperation(initializerBuilder.ToImmutableAndFree(), semanticModel: null, operation.Syntax, operation.Type, operation.IsImplicit);
            }

            // Build an operation for `<implicitReceiver>.Property = <capturedValue>`
            SimpleAssignmentOperation makeAssignment(IPropertySymbol property, IOperation capturedValue, WithOperation operation)
            {
                // <implicitReceiver>
                var implicitReceiver = new InstanceReferenceOperation(InstanceReferenceKind.ImplicitReceiver,
                    semanticModel: null, operation.Syntax, operation.Type, isImplicit: true);

                // <implicitReceiver>.Property
                var target = new PropertyReferenceOperation(property, constrainedToType: null, ImmutableArray<IArgumentOperation>.Empty, implicitReceiver,
                    semanticModel: null, operation.Syntax, property.Type, isImplicit: true);

                // <implicitReceiver>.Property = <capturedValue>
                return new SimpleAssignmentOperation(isRef: false, target, capturedValue,
                    semanticModel: null, operation.Syntax, property.Type, constantValue: null, isImplicit: true);
            }

            static bool setsAllProperties(ImmutableArray<IOperation> initializers, IEnumerable<IPropertySymbol> properties)
            {
                var set = new HashSet<IPropertySymbol>(SymbolEqualityComparer.IgnoreAll);
                foreach (var initializer in initializers)
                {
                    if (initializer is not ISimpleAssignmentOperation simpleAssignment)
                    {
                        continue;
                    }
                    if (simpleAssignment.Target.Kind != OperationKind.PropertyReference)
                    {
                        Debug.Assert(simpleAssignment.Target is InvalidOperation);
                        continue;
                    }

                    var propertyReference = (IPropertyReferenceOperation)simpleAssignment.Target;
                    Debug.Assert(properties.Contains(propertyReference.Property, SymbolEqualityComparer.IgnoreAll));
                    set.Add(propertyReference.Property);
                }

                return set.Count == properties.Count();
            }
        }

        public override IOperation VisitAttribute(IAttributeOperation operation, int? captureIdForResult)
        {
            return new AttributeOperation(Visit(operation.Operation, captureIdForResult)!, semanticModel: null, operation.Syntax, IsImplicit(operation));
        }
    }
}
