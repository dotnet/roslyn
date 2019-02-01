// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Subtype for all dataflow analyses on a control flow graph.
    /// It performs a worklist based approach to flow abstract data values for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> across the basic blocks until a fix point is reached.
    /// </summary>
    internal abstract class DataFlowAnalysis<TAnalysisData, TAnalysisContext, TAnalysisResult, TBlockAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>
        where TBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        private static readonly ConditionalWeakTable<IOperation, SingleThreadedConcurrentDictionary<DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>, TAnalysisResult>> s_resultCache =
            new ConditionalWeakTable<IOperation, SingleThreadedConcurrentDictionary<DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>, TAnalysisResult>>();

        protected DataFlowAnalysis(AbstractAnalysisDomain<TAnalysisData> analysisDomain, DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor)
        {
            AnalysisDomain = analysisDomain;
            OperationVisitor = operationVisitor;
        }

        protected AbstractAnalysisDomain<TAnalysisData> AnalysisDomain { get; }
        protected DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> OperationVisitor { get; }

        protected TAnalysisResult GetOrComputeResultCore(TAnalysisContext analysisContext, bool cacheResult)
        {
            if (analysisContext == null)
            {
                throw new ArgumentNullException(nameof(analysisContext));
            }

            if (!cacheResult)
            {
                return Run(analysisContext);
            }

            var analysisResultsMap = s_resultCache.GetOrCreateValue(analysisContext.ControlFlowGraph.OriginalOperation);
            return analysisResultsMap.GetOrAdd(OperationVisitor, _ => Run(analysisContext));
        }

        private TAnalysisResult Run(TAnalysisContext analysisContext)
        {
            var cfg = analysisContext.ControlFlowGraph;
            var resultBuilder = new DataFlowAnalysisResultBuilder<TAnalysisData>();
            var uniqueSuccessors = PooledHashSet<BasicBlock>.GetInstance();
            var finallyBlockSuccessorsMap = PooledDictionary<int, List<BranchWithInfo>>.GetInstance();
            var catchBlockInputDataMap = PooledDictionary<ControlFlowRegion, TAnalysisData>.GetInstance();
            var inputDataFromInfeasibleBranchesMap = PooledDictionary<int, TAnalysisData>.GetInstance();
            var unreachableBlocks = PooledHashSet<int>.GetInstance();
            var worklist = new SortedSet<int>();
            var pendingBlocksNeedingAtLeastOnePass = new SortedSet<int>(cfg.Blocks.Select(b => b.Ordinal));

            try
            {

                // Add each basic block to the result.
                foreach (var block in cfg.Blocks)
                {
                    resultBuilder.Add(block);
                    if (!block.IsReachable)
                    {
                        unreachableBlocks.Add(block.Ordinal);
                    }
                }

                var entry = cfg.GetEntry();

                // Initialize the input of the entry block.
                // For context sensitive inter-procedural analysis, use the provided initial analysis data.
                // Otherwise, initialize with the default bottom value of the analysis domain.
                var initialAnalysisData = analysisContext.InterproceduralAnalysisDataOpt?.InitialAnalysisData ?? OperationVisitor.GetEmptyAnalysisData();
                UpdateInput(resultBuilder, entry, initialAnalysisData);

                // Add the block to the worklist.
                worklist.Add(entry.Ordinal);

                while (worklist.Count > 0 || pendingBlocksNeedingAtLeastOnePass.Count > 0)
                {
                    updateUnreachableBlocks();

                    // Get the next block to process from the worklist.
                    // If worklist is empty, get any one of the pendingBlocksNeedingAtLeastOnePass, which must be unreachable from Entry block.
                    int blockOrdinal;
                    if (worklist.Count > 0)
                    {
                        blockOrdinal = worklist.Min;
                        worklist.Remove(blockOrdinal);
                    }
                    else
                    {
                        blockOrdinal = pendingBlocksNeedingAtLeastOnePass.Min;
                    }

                    var block = cfg.Blocks[blockOrdinal];

                    var needsAtLeastOnePass = pendingBlocksNeedingAtLeastOnePass.Remove(blockOrdinal);
                    var isUnreachableBlock = unreachableBlocks.Contains(block.Ordinal);

                    // Get the input data for the block.
                    var input = resultBuilder[block];
                    if (input == null)
                    {
                        Debug.Assert(needsAtLeastOnePass);

                        if (isUnreachableBlock &&
                            inputDataFromInfeasibleBranchesMap.TryGetValue(block.Ordinal, out var currentInfeasibleData))
                        {
                            // Block is unreachable due to predicate analysis.
                            // Initialize the input from predecessors to avoid false reports in unreachable code.
                            input = currentInfeasibleData;
                            inputDataFromInfeasibleBranchesMap.Remove(block.Ordinal);
                        }
                        else
                        {
                            // For catch and filter regions, we track the initial input data in the catchBlockInputDataMap.
                            ControlFlowRegion enclosingTryAndCatchRegion = GetEnclosingTryAndCatchRegionIfStartsHandler(block);
                            if (enclosingTryAndCatchRegion != null)
                            {
                                Debug.Assert(enclosingTryAndCatchRegion.Kind == ControlFlowRegionKind.TryAndCatch);
                                Debug.Assert(block.EnclosingRegion.Kind == ControlFlowRegionKind.Catch || block.EnclosingRegion.Kind == ControlFlowRegionKind.Filter);
                                Debug.Assert(block.EnclosingRegion.FirstBlockOrdinal == block.Ordinal);
                                input = catchBlockInputDataMap[enclosingTryAndCatchRegion];
                            }
                        }

                        input = input != null ?
                            AnalysisDomain.Clone(input) :
                            OperationVisitor.GetEmptyAnalysisData();

                        UpdateInput(resultBuilder, block, input);
                    }

                    // Check if we are starting a try region which has one or more associated catch/filter regions.
                    // If so, we conservatively merge the input data for try region into the input data for the associated catch/filter regions.
                    if (block.EnclosingRegion?.Kind == ControlFlowRegionKind.Try &&
                        block.EnclosingRegion?.EnclosingRegion?.Kind == ControlFlowRegionKind.TryAndCatch &&
                        block.EnclosingRegion.EnclosingRegion.FirstBlockOrdinal == block.Ordinal)
                    {
                        MergeIntoCatchInputData(block.EnclosingRegion.EnclosingRegion, input);
                    }

                    // Flow the new input through the block to get a new output.
                    var output = Flow(OperationVisitor, block, AnalysisDomain.Clone(input));

                    try
                    {
                        // Update the current block result's
                        // output values with the new ones.
                        CloneAndUpdateOutputIfEntryOrExitBlock(resultBuilder, block, output);

                        // Propagate the output data to all the successor blocks of the current block.
                        uniqueSuccessors.Clear();

                        // Get the successors with corresponding flow branches.
                        // CONSIDER: Currently we need to do a bunch of branch adjusments for branches to/from finally, catch and filter regions.
                        //           We should revisit the overall CFG API and the walker to avoid such adjustments.
                        var successorsWithAdjustedBranches = GetSuccessorsWithAdjustedBranches(block).ToArray();
                        foreach ((BranchWithInfo successorWithBranch, BranchWithInfo preadjustSuccessorWithBranch) in successorsWithAdjustedBranches)
                        {
                            // successorWithAdjustedBranch returns a pair of branches:
                            //  1. successorWithBranch - This is the adjusted branch for a branch from inside a try region to outside the try region, where we don't flow into finally region.
                            //                           The adjusted branch is targeted into the finally.
                            //  2. preadjustSuccessorWithBranch - This is the original branch, which is primarily used to update the input data and successors of finally and catch region regions.
                            //                                    Currently, these blocks have no branch coming out from it.

                            // Flow the current analysis data through the branch.
                            (TAnalysisData newSuccessorInput, bool isFeasibleBranch) = OperationVisitor.FlowBranch(block, successorWithBranch, AnalysisDomain.Clone(output));

                            if (preadjustSuccessorWithBranch != null)
                            {
                                UpdateFinallySuccessorsAndCatchInput(preadjustSuccessorWithBranch, newSuccessorInput);
                            }

                            // Certain branches have no destination (e.g. BranchKind.Throw), so we don't need to update the input data for the branch destination block.
                            var successorBlockOpt = successorWithBranch.Destination;
                            if (successorBlockOpt == null)
                            {
                                newSuccessorInput.Dispose();
                                continue;
                            }

                            // Perf: We can stop tracking data for entities whose lifetime is limited by the leaving regions.
                            //       Below invocation explicitly drops such data from destination input.
                            newSuccessorInput = OperationVisitor.OnLeavingRegions(successorWithBranch.LeavingRegions, block, newSuccessorInput);

                            var isBackEdge = block.Ordinal >= successorBlockOpt.Ordinal;
                            if (isUnreachableBlock && !unreachableBlocks.Contains(successorBlockOpt.Ordinal))
                            {
                                // Skip processing successor input for branch from an unreachable block to a reachable block.
                                newSuccessorInput.Dispose();
                                continue;
                            }
                            else if (!isFeasibleBranch)
                            {
                                // Skip processing the successor input for conditional branch that can never be taken.
                                if (inputDataFromInfeasibleBranchesMap.TryGetValue(successorBlockOpt.Ordinal, out TAnalysisData currentInfeasibleData))
                                {
                                    var dataToDispose = newSuccessorInput;
                                    newSuccessorInput = OperationVisitor.MergeAnalysisData(currentInfeasibleData, newSuccessorInput, isBackEdge);
                                    Debug.Assert(!ReferenceEquals(dataToDispose, newSuccessorInput));
                                    dataToDispose.Dispose();
                                }

                                inputDataFromInfeasibleBranchesMap[successorBlockOpt.Ordinal] = newSuccessorInput;
                                continue;
                            }

                            TAnalysisData mergedSuccessorInput;
                            var currentSuccessorInput = resultBuilder[successorBlockOpt];
                            if (currentSuccessorInput != null)
                            {
                                // Check if the current input data for the successor block is equal to the new input data from this branch.
                                // If so, we don't need to propagate new input data from this branch.
                                if (AnalysisDomain.Equals(currentSuccessorInput, newSuccessorInput))
                                {
                                    newSuccessorInput.Dispose();
                                    continue;
                                }

                                // Otherwise, check if the input data for the successor block changes after merging with the new input data.
                                mergedSuccessorInput = OperationVisitor.MergeAnalysisData(currentSuccessorInput, newSuccessorInput, isBackEdge);
                                newSuccessorInput.Dispose();

                                int compare = AnalysisDomain.Compare(currentSuccessorInput, mergedSuccessorInput);

                                // The newly computed abstract values for each basic block
                                // must be always greater or equal than the previous value
                                // to ensure termination.
                                Debug.Assert(compare <= 0, "The newly computed abstract value must be greater or equal than the previous one.");

                                // Is old input value >= new input value
                                if (compare >= 0)
                                {
                                    mergedSuccessorInput.Dispose();
                                    continue;
                                }
                            }
                            else
                            {
                                mergedSuccessorInput = newSuccessorInput;
                            }

                            // Input to successor has changed, so we need to update its new input and
                            // reprocess the successor by adding it to the worklist.
                            UpdateInput(resultBuilder, successorBlockOpt, mergedSuccessorInput);

                            if (uniqueSuccessors.Add(successorBlockOpt))
                            {
                                worklist.Add(successorBlockOpt.Ordinal);
                            }
                        }

                        Debug.Assert(IsValidWorklistState());
                    }
                    finally
                    {
                        output.Dispose();
                    }
                }

                var mergedDataForUnhandledThrowOperationsOpt = OperationVisitor.GetMergedDataForUnhandledThrowOperations();

                var dataflowAnalysisResult = resultBuilder.ToResult(ToBlockResult, OperationVisitor.GetStateMap(),
                    OperationVisitor.GetPredicateValueKindMap(), OperationVisitor.GetReturnValueAndPredicateKind(), OperationVisitor.InterproceduralResultsMap,
                    resultBuilder.EntryBlockOutputData, resultBuilder.ExitBlockOutputData,
                    mergedDataForUnhandledThrowOperationsOpt, cfg, OperationVisitor.ValueDomain.UnknownOrMayBeValue);
                mergedDataForUnhandledThrowOperationsOpt?.Dispose();

                return ToResult(analysisContext, dataflowAnalysisResult);
            }
            finally
            {
                resultBuilder.Dispose();
                uniqueSuccessors.Free();
                finallyBlockSuccessorsMap.Free();
                catchBlockInputDataMap.Values.Dispose();
                catchBlockInputDataMap.Free();
                inputDataFromInfeasibleBranchesMap.Values.Dispose();
                inputDataFromInfeasibleBranchesMap.Free();
                unreachableBlocks.Free();
            }

            // Local functions.
            void updateUnreachableBlocks()
            {
                if (worklist.Count == 0)
                {
                    foreach (var blockOrdinal in pendingBlocksNeedingAtLeastOnePass)
                    {
                        unreachableBlocks.Add(blockOrdinal);
                    }
                }
            }

            ControlFlowRegion MergeIntoCatchInputData(ControlFlowRegion tryAndCatchRegion, TAnalysisData dataToMerge)
            {
                Debug.Assert(tryAndCatchRegion.Kind == ControlFlowRegionKind.TryAndCatch);

                var catchRegion = tryAndCatchRegion.NestedRegions.FirstOrDefault(region => region.Kind == ControlFlowRegionKind.Catch || region.Kind == ControlFlowRegionKind.FilterAndHandler);
                if (catchRegion == null)
                {
                    return null;
                }

                var catchBlock = cfg.Blocks[catchRegion.FirstBlockOrdinal];

                // Check if we have already visited the catch block once, and hence have a non-null input.
                // If so, just update the resultBuilder input.
                // Otherwise, update the catchBlockInputDataMap.

                var catchBlockInputData = resultBuilder[catchBlock];
                if (catchBlockInputData != null)
                {
                    // Check if the current input data for the catch block is equal to the new input data from this branch.
                    // If so, we don't need to propagate new input data from this branch.
                    if (AnalysisDomain.Equals(catchBlockInputData, dataToMerge))
                    {
                        return null;
                    }

                    // Otherwise, check if the input data for the catch block changes after merging with the new input data.
                    var mergedData = AnalysisDomain.Merge(catchBlockInputData, dataToMerge);
                    int compare = AnalysisDomain.Compare(catchBlockInputData, mergedData);

                    // The newly computed abstract values for each basic block
                    // must be always greater or equal than the previous value
                    // to ensure termination.
                    Debug.Assert(compare <= 0, "The newly computed abstract value must be greater or equal than the previous one.");

                    if (compare == 0)
                    {
                        return null;
                    }

                    UpdateInput(resultBuilder, catchBlock, mergedData);
                }
                else
                {
                    if (!catchBlockInputDataMap.TryGetValue(tryAndCatchRegion, out catchBlockInputData))
                    {
                        catchBlockInputData = AnalysisDomain.Clone(dataToMerge);
                    }
                    else
                    {
                        catchBlockInputData = AnalysisDomain.Merge(catchBlockInputData, dataToMerge);
                    }

                    catchBlockInputDataMap[tryAndCatchRegion] = catchBlockInputData;
                }

                return catchRegion;
            }

            // Ensures that we have a valid worklist/pendingBlocksNeedingAtLeastOnePass state.
            bool IsValidWorklistState()
            {
                if (worklist.Count == 0 && pendingBlocksNeedingAtLeastOnePass.Count == 0)
                {
                    return true;
                }

                foreach (var blockOrdinal in worklist.Concat(pendingBlocksNeedingAtLeastOnePass))
                {
                    var block = cfg.Blocks[blockOrdinal];
                    if (block.Predecessors.IsEmpty || !HasUnprocessedPredecessorBlock(block))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool HasUnprocessedPredecessorBlock(BasicBlock block)
            {
                var predecessorsWithBranches = block.GetPredecessorsWithBranches(cfg);
                return predecessorsWithBranches.Any(predecessorWithBranch =>
                    predecessorWithBranch.predecessorBlock.Ordinal < block.Ordinal &&
                    pendingBlocksNeedingAtLeastOnePass.Contains(predecessorWithBranch.predecessorBlock.Ordinal));
            }

            // If this block starts a catch/filter region, return the enclosing TryAndCatch region.
            ControlFlowRegion GetEnclosingTryAndCatchRegionIfStartsHandler(BasicBlock block)
            {
                if (block.EnclosingRegion?.FirstBlockOrdinal == block.Ordinal)
                {
                    switch (block.EnclosingRegion.Kind)
                    {
                        case ControlFlowRegionKind.Catch:
                            if (block.EnclosingRegion.EnclosingRegion.Kind == ControlFlowRegionKind.TryAndCatch)
                            {
                                return block.EnclosingRegion.EnclosingRegion;
                            }
                            break;

                        case ControlFlowRegionKind.Filter:
                            if (block.EnclosingRegion.EnclosingRegion.Kind == ControlFlowRegionKind.FilterAndHandler &&
                                block.EnclosingRegion.EnclosingRegion.EnclosingRegion?.Kind == ControlFlowRegionKind.TryAndCatch)
                            {
                                return block.EnclosingRegion.EnclosingRegion.EnclosingRegion;
                            }
                            break;
                    }
                }

                return null;
            }

            IEnumerable<(BranchWithInfo successorWithBranch, BranchWithInfo preadjustSuccessorWithBranch)> GetSuccessorsWithAdjustedBranches(BasicBlock basicBlock)
            {
                if (basicBlock.Kind != BasicBlockKind.Exit)
                {
                    // If this is the last block of finally region, use the finallyBlockSuccessorsMap to get its successors.
                    if (finallyBlockSuccessorsMap.TryGetValue(basicBlock.Ordinal, out var finallySuccessors))
                    {
                        Debug.Assert(basicBlock.EnclosingRegion.Kind == ControlFlowRegionKind.Finally);
                        foreach (var successor in finallySuccessors)
                        {
                            yield return (successor, null);
                        }
                    }
                    else
                    {
                        var preadjustSuccessorWithbranch = new BranchWithInfo(basicBlock.FallThroughSuccessor);
                        var adjustedSuccessorWithBranch = AdjustBranchIfFinalizing(preadjustSuccessorWithbranch);
                        yield return (successorWithBranch: adjustedSuccessorWithBranch, preadjustSuccessorWithBranch: preadjustSuccessorWithbranch);

                        if (basicBlock.ConditionalSuccessor?.Destination != null)
                        {
                            preadjustSuccessorWithbranch = new BranchWithInfo(basicBlock.ConditionalSuccessor);
                            adjustedSuccessorWithBranch = AdjustBranchIfFinalizing(preadjustSuccessorWithbranch);
                            yield return (successorWithBranch: adjustedSuccessorWithBranch, preadjustSuccessorWithBranch: preadjustSuccessorWithbranch);
                        }
                    }
                }
            }

            // Adjust the branch if we are going to be executing one or more finally regions, but the CFG's branch doesn't account for these.
            BranchWithInfo AdjustBranchIfFinalizing(BranchWithInfo branch)
            {
                if (branch.FinallyRegions.Length > 0)
                {
                    var firstFinally = branch.FinallyRegions[0];
                    var destination = cfg.Blocks[firstFinally.FirstBlockOrdinal];
                    return branch.With(destination, enteringRegions: ImmutableArray<ControlFlowRegion>.Empty,
                        leavingRegions: ImmutableArray<ControlFlowRegion>.Empty, finallyRegions: ImmutableArray<ControlFlowRegion>.Empty);
                }
                else
                {
                    return branch;
                }
            }

            // Updates the successors of finally blocks.
            // Also updates the merged input data tracked for catch blocks.
            void UpdateFinallySuccessorsAndCatchInput(BranchWithInfo branch, TAnalysisData branchData)
            {
                // Compute and update finally successors.
                if (branch.FinallyRegions.Length > 0)
                {
                    var successor = branch.With(branchValueOpt: null, controlFlowConditionKind: ControlFlowConditionKind.None);
                    for (var i = branch.FinallyRegions.Length - 1; i >= 0; i--)
                    {
                        ControlFlowRegion finallyRegion = branch.FinallyRegions[i];
                        UpdateFinallySuccessor(finallyRegion, successor);
                        successor = new BranchWithInfo(destination: cfg.Blocks[finallyRegion.FirstBlockOrdinal]);
                    }
                }

                // Update catch input data.
                if (branch.LeavingRegions.Length > 0)
                {
                    foreach (var tryAndCatchRegion in branch.LeavingRegions.Where(region => region.Kind == ControlFlowRegionKind.TryAndCatch))
                    {
                        var catchRegion = MergeIntoCatchInputData(tryAndCatchRegion, branchData);
                        if (catchRegion != null)
                        {
                            // We also need to enqueue the catch block into the worklist as there is no direct branch into catch.
                            worklist.Add(catchRegion.FirstBlockOrdinal);
                        }
                    }
                }
            }

            void UpdateFinallySuccessor(ControlFlowRegion finallyRegion, BranchWithInfo successor)
            {
                Debug.Assert(finallyRegion.Kind == ControlFlowRegionKind.Finally);
                if (!finallyBlockSuccessorsMap.TryGetValue(finallyRegion.LastBlockOrdinal, out var lastBlockSuccessors))
                {
                    lastBlockSuccessors = new List<BranchWithInfo>();
                    finallyBlockSuccessorsMap.Add(finallyRegion.LastBlockOrdinal, lastBlockSuccessors);
                }

                lastBlockSuccessors.Add(successor);
            }
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static TAnalysisData Flow(DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor, BasicBlock block, TAnalysisData data)
        {
            operationVisitor.OnStartBlockAnalysis(block, data);

            foreach (var statement in block.Operations)
            {
                data = operationVisitor.Flow(statement, block, data);
            }

            operationVisitor.OnEndBlockAnalysis(block);

            return data;
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        internal abstract TAnalysisResult ToResult(TAnalysisContext analysisContext, DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> dataFlowAnalysisResult);
        internal abstract TBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, TAnalysisData blockAnalysisData);

        private void UpdateInput(DataFlowAnalysisResultBuilder<TAnalysisData> builder, BasicBlock block, TAnalysisData newInput)
        {
            Debug.Assert(newInput != null);
            Debug.Assert(builder[block] == null || AnalysisDomain.Compare(builder[block], newInput) <= 0, "Non-monotonic update");
            builder.Update(block, newInput);
        }

        private void CloneAndUpdateOutputIfEntryOrExitBlock(DataFlowAnalysisResultBuilder<TAnalysisData> builder, BasicBlock block, TAnalysisData newOutput)
        {
            switch (block.Kind)
            {
                case BasicBlockKind.Entry:
                    builder.EntryBlockOutputData = AnalysisDomain.Clone(newOutput);
                    break;

                case BasicBlockKind.Exit:
                    builder.ExitBlockOutputData = AnalysisDomain.Clone(newOutput);
                    break;
            }
        }
    }
}