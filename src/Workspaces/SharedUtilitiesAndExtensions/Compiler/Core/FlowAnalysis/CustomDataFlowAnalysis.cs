// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis;

internal static class CustomDataFlowAnalysis<TBlockAnalysisData>
{
    /// <summary>
    /// Runs dataflow analysis for the given <paramref name="analyzer"/> on the given <paramref name="controlFlowGraph"/>.
    /// </summary>
    /// <param name="controlFlowGraph">Control flow graph on which to execute analysis.</param>
    /// <param name="analyzer">Dataflow analyzer.</param>
    /// <returns>Block analysis data at the end of the exit block.</returns>
    /// <remarks>
    /// Algorithm for this CFG walker has been forked from <see cref="ControlFlowGraphBuilder"/>'s internal
    /// implementation for basic block reachability computation: "MarkReachableBlocks",
    /// we should keep them in sync as much as possible.
    /// </remarks>
    public static TBlockAnalysisData Run(ControlFlowGraph controlFlowGraph, DataFlowAnalyzer<TBlockAnalysisData> analyzer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blocks = controlFlowGraph.Blocks;
        var continueDispatchAfterFinally = PooledDictionary<ControlFlowRegion, bool>.GetInstance();
        var dispatchedExceptionsFromRegions = PooledHashSet<ControlFlowRegion>.GetInstance();
        var firstBlockOrdinal = 0;
        var lastBlockOrdinal = blocks.Length - 1;

        var unreachableBlocksToVisit = ArrayBuilder<BasicBlock>.GetInstance();
        if (analyzer.AnalyzeUnreachableBlocks)
        {
            for (var i = firstBlockOrdinal; i <= lastBlockOrdinal; i++)
            {
                if (!blocks[i].IsReachable)
                {
                    unreachableBlocksToVisit.Add(blocks[i]);
                }
            }
        }

        var initialAnalysisData = analyzer.GetCurrentAnalysisData(blocks[0]);

        var result = RunCore(blocks, analyzer, firstBlockOrdinal, lastBlockOrdinal,
                             initialAnalysisData,
                             unreachableBlocksToVisit,
                             outOfRangeBlocksToVisit: null,
                             continueDispatchAfterFinally,
                             dispatchedExceptionsFromRegions,
                             cancellationToken);
        Debug.Assert(unreachableBlocksToVisit.Count == 0);
        unreachableBlocksToVisit.Free();
        continueDispatchAfterFinally.Free();
        dispatchedExceptionsFromRegions.Free();
        return result;
    }

    private static TBlockAnalysisData RunCore(
        ImmutableArray<BasicBlock> blocks,
        DataFlowAnalyzer<TBlockAnalysisData> analyzer,
        int firstBlockOrdinal,
        int lastBlockOrdinal,
        TBlockAnalysisData initialAnalysisData,
        ArrayBuilder<BasicBlock> unreachableBlocksToVisit,
        SortedSet<int> outOfRangeBlocksToVisit,
        PooledDictionary<ControlFlowRegion, bool> continueDispatchAfterFinally,
        PooledHashSet<ControlFlowRegion> dispatchedExceptionsFromRegions,
        CancellationToken cancellationToken)
    {
        var toVisit = new SortedSet<int>();

        var firstBlock = blocks[firstBlockOrdinal];
        analyzer.SetCurrentAnalysisData(firstBlock, initialAnalysisData, cancellationToken);
        toVisit.Add(firstBlock.Ordinal);

        var processedBlocks = PooledHashSet<BasicBlock>.GetInstance();
        TBlockAnalysisData resultAnalysisData = default;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            BasicBlock current;
            if (toVisit.Count > 0)
            {
                var min = toVisit.Min;
                toVisit.Remove(min);
                current = blocks[min];
            }
            else
            {
                int index;
                current = null;
                for (index = 0; index < unreachableBlocksToVisit.Count; index++)
                {
                    var unreachableBlock = unreachableBlocksToVisit[index];
                    if (unreachableBlock.Ordinal >= firstBlockOrdinal && unreachableBlock.Ordinal <= lastBlockOrdinal)
                    {
                        current = unreachableBlock;
                        break;
                    }
                }

                if (current == null)
                {
                    continue;
                }

                unreachableBlocksToVisit.RemoveAt(index);
                if (processedBlocks.Contains(current))
                {
                    // Already processed from a branch from another unreachable block.
                    continue;
                }

                analyzer.SetCurrentAnalysisData(current, analyzer.GetEmptyAnalysisData(), cancellationToken);
            }

            if (current.Ordinal < firstBlockOrdinal || current.Ordinal > lastBlockOrdinal)
            {
                outOfRangeBlocksToVisit.Add(current.Ordinal);
                continue;
            }

            if (current.Ordinal == current.EnclosingRegion.FirstBlockOrdinal)
            {
                // We are revisiting first block of a region, so we need to again dispatch exceptions from region.
                dispatchedExceptionsFromRegions.Remove(current.EnclosingRegion);
            }

            var fallThroughAnalysisData = analyzer.AnalyzeBlock(current, cancellationToken);
            var fallThroughSuccessorIsReachable = true;

            if (current.ConditionKind != ControlFlowConditionKind.None)
            {
                TBlockAnalysisData conditionalSuccessorAnalysisData;
                (fallThroughAnalysisData, conditionalSuccessorAnalysisData) = analyzer.AnalyzeConditionalBranch(current, fallThroughAnalysisData, cancellationToken);

                var conditionalSuccesorIsReachable = true;
                if (current.BranchValue.ConstantValue.HasValue && current.BranchValue.ConstantValue.Value is bool constant)
                {
                    if (constant == (current.ConditionKind == ControlFlowConditionKind.WhenTrue))
                    {
                        fallThroughSuccessorIsReachable = false;
                    }
                    else
                    {
                        conditionalSuccesorIsReachable = false;
                    }
                }

                if (conditionalSuccesorIsReachable || analyzer.AnalyzeUnreachableBlocks)
                {
                    FollowBranch(current, current.ConditionalSuccessor, conditionalSuccessorAnalysisData);
                }
            }
            else
            {
                fallThroughAnalysisData = analyzer.AnalyzeNonConditionalBranch(current, fallThroughAnalysisData, cancellationToken);
            }

            if (fallThroughSuccessorIsReachable || analyzer.AnalyzeUnreachableBlocks)
            {
                var branch = current.FallThroughSuccessor;
                FollowBranch(current, branch, fallThroughAnalysisData);

                if (current.EnclosingRegion.Kind == ControlFlowRegionKind.Finally &&
                    current.Ordinal == lastBlockOrdinal)
                {
                    continueDispatchAfterFinally[current.EnclosingRegion] = branch.Semantics != ControlFlowBranchSemantics.Throw &&
                        branch.Semantics != ControlFlowBranchSemantics.Rethrow &&
                        current.FallThroughSuccessor.Semantics == ControlFlowBranchSemantics.StructuredExceptionHandling;
                }
            }

            if (current.Ordinal == lastBlockOrdinal)
            {
                resultAnalysisData = fallThroughAnalysisData;
            }

            // We are using very simple approach: 
            // If try block is reachable, we should dispatch an exception from it, even if it is empty.
            // To simplify implementation, we dispatch exception from every reachable basic block and rely
            // on dispatchedExceptionsFromRegions cache to avoid doing duplicate work.
            DispatchException(current.EnclosingRegion);

            processedBlocks.Add(current);
        }
        while (toVisit.Count != 0 || unreachableBlocksToVisit.Count != 0);

        return resultAnalysisData;

        // Local functions.
        void FollowBranch(BasicBlock current, ControlFlowBranch branch, TBlockAnalysisData currentAnalsisData)
        {
            if (branch == null)
            {
                return;
            }

            switch (branch.Semantics)
            {
                case ControlFlowBranchSemantics.None:
                case ControlFlowBranchSemantics.ProgramTermination:
                case ControlFlowBranchSemantics.StructuredExceptionHandling:
                case ControlFlowBranchSemantics.Error:
                    Debug.Assert(branch.Destination == null);
                    return;

                case ControlFlowBranchSemantics.Throw:
                case ControlFlowBranchSemantics.Rethrow:
                    Debug.Assert(branch.Destination == null);
                    StepThroughFinally(current.EnclosingRegion, destinationOrdinal: lastBlockOrdinal, ref currentAnalsisData);
                    return;

                case ControlFlowBranchSemantics.Regular:
                case ControlFlowBranchSemantics.Return:
                    Debug.Assert(branch.Destination != null);

                    if (StepThroughFinally(current.EnclosingRegion, branch.Destination.Ordinal, ref currentAnalsisData))
                    {
                        var destination = branch.Destination;
                        var currentDestinationData = analyzer.GetCurrentAnalysisData(destination);
                        var mergedAnalysisData = analyzer.Merge(currentDestinationData, currentAnalsisData, cancellationToken);
                        // We need to analyze the destination block if both the following conditions are met:
                        //  1. Either the current block is reachable both destination and current are non-reachable
                        //  2. Either the new analysis data for destination has changed or destination block hasn't
                        //     been processed.
                        if ((current.IsReachable || !destination.IsReachable) &&
                            (!analyzer.IsEqual(currentDestinationData, mergedAnalysisData) || !processedBlocks.Contains(destination)))
                        {
                            analyzer.SetCurrentAnalysisData(destination, mergedAnalysisData, cancellationToken);
                            toVisit.Add(branch.Destination.Ordinal);
                        }
                    }

                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(branch.Semantics);
            }
        }

        // Returns whether we should proceed to the destination after finallies were taken care of.
        bool StepThroughFinally(ControlFlowRegion region, int destinationOrdinal, ref TBlockAnalysisData currentAnalysisData)
        {
            while (!region.ContainsBlock(destinationOrdinal))
            {
                Debug.Assert(region.Kind != ControlFlowRegionKind.Root);
                var enclosing = region.EnclosingRegion;
                if (region.Kind == ControlFlowRegionKind.Try && enclosing.Kind == ControlFlowRegionKind.TryAndFinally)
                {
                    Debug.Assert(enclosing.NestedRegions[0] == region);
                    Debug.Assert(enclosing.NestedRegions[1].Kind == ControlFlowRegionKind.Finally);
                    if (!StepThroughSingleFinally(enclosing.NestedRegions[1], ref currentAnalysisData))
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
        bool StepThroughSingleFinally(ControlFlowRegion @finally, ref TBlockAnalysisData currentAnalysisData)
        {
            Debug.Assert(@finally.Kind == ControlFlowRegionKind.Finally);
            var previousAnalysisData = analyzer.GetCurrentAnalysisData(blocks[@finally.FirstBlockOrdinal]);
            var mergedAnalysisData = analyzer.Merge(previousAnalysisData, currentAnalysisData, cancellationToken);
            if (!analyzer.IsEqual(previousAnalysisData, mergedAnalysisData))
            {
                // For simplicity, we do a complete walk of the finally/filter region in isolation
                // to make sure that the resume dispatch point is reachable from its beginning.
                // It could also be reachable through invalid branches into the finally and we don't want to consider 
                // these cases for regular finally handling.
                currentAnalysisData = RunCore(blocks,
                                              analyzer,
                                              @finally.FirstBlockOrdinal,
                                              @finally.LastBlockOrdinal,
                                              mergedAnalysisData,
                                              unreachableBlocksToVisit,
                                              outOfRangeBlocksToVisit: toVisit,
                                              continueDispatchAfterFinally,
                                              dispatchedExceptionsFromRegions,
                                              cancellationToken);
            }

            if (!continueDispatchAfterFinally.TryGetValue(@finally, out var dispatch))
            {
                dispatch = false;
                continueDispatchAfterFinally.Add(@finally, false);
            }

            return dispatch;
        }

        void DispatchException(ControlFlowRegion fromRegion)
        {
            do
            {
                if (!dispatchedExceptionsFromRegions.Add(fromRegion))
                {
                    return;
                }

                var enclosing = fromRegion.Kind == ControlFlowRegionKind.Root ? null : fromRegion.EnclosingRegion;
                if (fromRegion.Kind == ControlFlowRegionKind.Try)
                {
                    switch (enclosing.Kind)
                    {
                        case ControlFlowRegionKind.TryAndFinally:
                            Debug.Assert(enclosing.NestedRegions[0] == fromRegion);
                            Debug.Assert(enclosing.NestedRegions[1].Kind == ControlFlowRegionKind.Finally);
                            var currentAnalysisData = analyzer.GetCurrentAnalysisData(blocks[fromRegion.FirstBlockOrdinal]);
                            if (!StepThroughSingleFinally(enclosing.NestedRegions[1], ref currentAnalysisData))
                            {
                                // The point that continues dispatch is not reachable. Cancel the dispatch.
                                return;
                            }

                            break;

                        case ControlFlowRegionKind.TryAndCatch:
                            Debug.Assert(enclosing.NestedRegions[0] == fromRegion);
                            DispatchExceptionThroughCatches(enclosing, startAt: 1);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(enclosing.Kind);
                    }
                }
                else if (fromRegion.Kind == ControlFlowRegionKind.Filter)
                {
                    // If filter throws, dispatch is resumed at the next catch with an original exception
                    Debug.Assert(enclosing.Kind == ControlFlowRegionKind.FilterAndHandler);
                    var tryAndCatch = enclosing.EnclosingRegion;
                    Debug.Assert(tryAndCatch.Kind == ControlFlowRegionKind.TryAndCatch);

                    var index = tryAndCatch.NestedRegions.IndexOf(enclosing, startIndex: 1);

                    if (index > 0)
                    {
                        DispatchExceptionThroughCatches(tryAndCatch, startAt: index + 1);
                        fromRegion = tryAndCatch;
                        continue;
                    }

                    throw ExceptionUtilities.Unreachable();
                }

                fromRegion = enclosing;
            }
            while (fromRegion != null);
        }

        void DispatchExceptionThroughCatches(ControlFlowRegion tryAndCatch, int startAt)
        {
            // For simplicity, we do not try to figure out whether a catch clause definitely
            // handles all exceptions.

            Debug.Assert(tryAndCatch.Kind == ControlFlowRegionKind.TryAndCatch);
            Debug.Assert(startAt > 0);
            Debug.Assert(startAt <= tryAndCatch.NestedRegions.Length);

            for (var i = startAt; i < tryAndCatch.NestedRegions.Length; i++)
            {
                var @catch = tryAndCatch.NestedRegions[i];

                switch (@catch.Kind)
                {
                    case ControlFlowRegionKind.Catch:
                        toVisit.Add(@catch.FirstBlockOrdinal);
                        break;

                    case ControlFlowRegionKind.FilterAndHandler:
                        var entryBlock = blocks[@catch.FirstBlockOrdinal];
                        Debug.Assert(@catch.NestedRegions[0].Kind == ControlFlowRegionKind.Filter);
                        Debug.Assert(entryBlock.Ordinal == @catch.NestedRegions[0].FirstBlockOrdinal);

                        toVisit.Add(entryBlock.Ordinal);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(@catch.Kind);
                }
            }
        }
    }
}
