// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Subtype for all dataflow analyses on a control flow graph.
    /// It performs a worklist based approach to flow abstract data values for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> across the basic blocks until a fix point is reached.
    /// </summary>
    internal abstract class DataFlowAnalysis<TAnalysisData, TAnalysisContext, TAnalysisResult, TBlockAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : class
        where TAnalysisContext: AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>
        where TBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        private static readonly ConditionalWeakTable<IOperation, ConcurrentDictionary<DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>, TAnalysisResult>> s_resultCache =
            new ConditionalWeakTable<IOperation, ConcurrentDictionary<DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>, TAnalysisResult>>();

        protected DataFlowAnalysis(AbstractAnalysisDomain<TAnalysisData> analysisDomain, DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor)
        {
            AnalysisDomain = analysisDomain;
            OperationVisitor = operationVisitor;
        }

        protected AbstractAnalysisDomain<TAnalysisData> AnalysisDomain { get; }
        protected DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> OperationVisitor { get; }
        private Dictionary<ControlFlowRegion, TAnalysisData> MergedInputAnalysisDataForFinallyRegions { get; set; }

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
            var resultBuilder = new DataFlowAnalysisResultBuilder<TAnalysisData>();
            var uniqueSuccessors = new HashSet<BasicBlock>();
            var ordinalToBlockMap = new Dictionary<int, BasicBlock>();
            var finallyBlockSuccessorsMap = new Dictionary<int, List<BranchWithInfo>>();
            var catchBlockInputDataMap = new Dictionary<ControlFlowRegion, TAnalysisData>();
            var inputDataFromInfeasibleBranchesMap = new Dictionary<int, TAnalysisData>();
            var unreachableBlocks = new HashSet<int>();
            var cfg = analysisContext.ControlFlowGraph;

            // Add each basic block to the result.
            foreach (var block in cfg.Blocks)
            {
                resultBuilder.Add(block);
                ordinalToBlockMap.Add(block.Ordinal, block);
                if (!block.IsReachable)
                {
                    unreachableBlocks.Add(block.Ordinal);
                }
            }

            var worklist = new Queue<BasicBlock>();
            var pendingBlocksNeedingAtLeastOnePass = new HashSet<BasicBlock>(cfg.Blocks);
            var entry = cfg.GetEntry();

            // Initialize the input of the entry block.
            // For context sensitive inter-procedural analysis, use the provided initial analysis data.
            // Otherwise, initialize with the default bottom value of the analysis domain.
            var initialAnalysisData = analysisContext.InterproceduralAnalysisDataOpt?.InitialAnalysisData ?? AnalysisDomain.Bottom;
            UpdateInput(resultBuilder, entry, initialAnalysisData);

            // Add the block to the worklist.
            worklist.Enqueue(entry);

            while (worklist.Count > 0 || pendingBlocksNeedingAtLeastOnePass.Count > 0)
            {
                updateUnreachableBlocks();

                // Get the next block to process from the worklist.
                // If worklist is empty, get any one of the pendingBlocksNeedingAtLeastOnePass, which must be unreachable from Entry block.
                var block = worklist.Count > 0 ? worklist.Dequeue() : pendingBlocksNeedingAtLeastOnePass.ElementAt(0);

                // Optimization: We process the block only if all its predecessor blocks have been processed once.
                if (HasUnprocessedPredecessorBlock(block))
                {
                    continue;
                }

                var needsAtLeastOnePass = pendingBlocksNeedingAtLeastOnePass.Remove(block);
                var isUnreachableBlock = unreachableBlocks.Contains(block.Ordinal);

                // Get the input data for the block.
                var input = GetInput(resultBuilder[block]);
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

                    input = input ?? AnalysisDomain.Bottom;

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

                // Compare the previous output with the new output.
                if (!needsAtLeastOnePass)
                {
                    int compare = AnalysisDomain.Compare(GetOutput(resultBuilder[block]), output);

                    // The newly computed abstract values for each basic block
                    // must be always greater or equal than the previous value
                    // to ensure termination. 
                    Debug.Assert(compare <= 0, "The newly computed abstract value must be greater or equal than the previous one.");

                    // Is old output value >= new output value ?
                    if (compare >= 0)
                    {
                        Debug.Assert(IsValidWorklistState());
                        continue;
                    }
                }

                // The newly computed value is greater than the previous value,
                // so we need to update the current block result's
                // output values with the new ones.
                UpdateOutput(resultBuilder, block, output);

                // Since the new output value is different than the previous one, 
                // we need to propagate it to all the successor blocks of the current block.
                uniqueSuccessors.Clear();

                // Get the successors with corresponding flow branches.
                // CONSIDER: Currently we need to do a bunch of branch adjusments for branches to/from finally, catch and filter regions.
                //           We should revisit the overall CFG API and the walker to avoid such adjustments.
                var successorsWithAdjustedBranches = GetSuccessorsWithAdjustedBranches(block).ToArray();
                foreach ((BranchWithInfo successorWithBranch, BranchWithInfo preadjustSuccessorWithBranch) successorWithAdjustedBranch in successorsWithAdjustedBranches)
                {
                    // successorWithAdjustedBranch returns a pair of branches:
                    //  1. successorWithBranch - This is the adjusted branch for a branch from inside a try region to outside the try region, where we don't flow into finally region.
                    //                           The adjusted branch is targeted into the finally.
                    //  2. preadjustSuccessorWithBranch - This is the original branch, which is primarily used to update the input data and successors of finally and catch region regions.
                    //                                    Currently, these blocks have no branch coming out from it.

                    // Flow the current analysis data through the branch.
                    (TAnalysisData newSuccessorInput, bool isFeasibleBranch) = OperationVisitor.FlowBranch(block, successorWithAdjustedBranch.successorWithBranch, AnalysisDomain.Clone(output));

                    if (successorWithAdjustedBranch.preadjustSuccessorWithBranch != null)
                    {
                        UpdateFinallySuccessorsAndCatchInput(successorWithAdjustedBranch.preadjustSuccessorWithBranch, newSuccessorInput);
                    }

                    // Certain branches have no destination (e.g. BranchKind.Throw), so we don't need to update the input data for the branch destination block.
                    var successorBlockOpt = successorWithAdjustedBranch.successorWithBranch.Destination;
                    if (successorBlockOpt == null)
                    {
                        continue;
                    }

                    // Perf: We can stop tracking data for entities whose lifetime is limited by the leaving regions.
                    //       Below invocation explicitly drops such data from destination input.
                    newSuccessorInput = OperationVisitor.OnLeavingRegions(successorWithAdjustedBranch.successorWithBranch.LeavingRegions, block, newSuccessorInput);

                    var isBackEdge = block.Ordinal >= successorBlockOpt.Ordinal;
                    if (isUnreachableBlock && !unreachableBlocks.Contains(successorBlockOpt.Ordinal))
                    {
                        // Skip processing successor input for branch from an unreachable block to a reachable block.
                        continue;
                    }
                    else if (!isFeasibleBranch)
                    {
                        // Skip processing the successor input for conditional branch that can never be taken.
                        if (inputDataFromInfeasibleBranchesMap.TryGetValue(successorBlockOpt.Ordinal, out TAnalysisData currentInfeasibleData))
                        {
                            newSuccessorInput = OperationVisitor.MergeAnalysisData(currentInfeasibleData, newSuccessorInput, isBackEdge);
                        }

                        inputDataFromInfeasibleBranchesMap[successorBlockOpt.Ordinal] = newSuccessorInput;
                        continue;
                    }

                    // Get the current input data for the successor block, and check if it changes after merging the new input data.
                    var currentSuccessorInput = GetInput(resultBuilder[successorBlockOpt]);
                    var mergedSuccessorInput = currentSuccessorInput != null ?
                        OperationVisitor.MergeAnalysisData(currentSuccessorInput, newSuccessorInput, isBackEdge) :
                        newSuccessorInput;

                    if (currentSuccessorInput != null)
                    {
                        int compare = AnalysisDomain.Compare(currentSuccessorInput, mergedSuccessorInput);

                        // The newly computed abstract values for each basic block
                        // must be always greater or equal than the previous value
                        // to ensure termination.
                        Debug.Assert(compare <= 0, "The newly computed abstract value must be greater or equal than the previous one.");

                        // Is old input value >= new input value
                        if (compare >= 0)
                        {
                            continue;
                        }
                    }

                    // Input to successor has changed, so we need to update its new input and
                    // reprocess the successor by adding it to the worklist.
                    UpdateInput(resultBuilder, successorBlockOpt, mergedSuccessorInput);

                    if (uniqueSuccessors.Add(successorBlockOpt))
                    {
                        worklist.Enqueue(successorBlockOpt);
                    }
                }

                Debug.Assert(IsValidWorklistState());
            }

            var dataflowAnalysisResult = resultBuilder.ToResult(ToBlockResult, OperationVisitor.GetStateMap(),
                OperationVisitor.GetPredicateValueKindMap(), OperationVisitor.GetReturnValueAndPredicateKind(), OperationVisitor.InterproceduralResultsMap,
                OperationVisitor.GetMergedDataForUnhandledThrowOperations(), cfg, OperationVisitor.ValueDomain.UnknownOrMayBeValue);
            return ToResult(analysisContext, dataflowAnalysisResult);

            void updateUnreachableBlocks()
            {
                if (worklist.Count == 0)
                {
                    foreach (var block in pendingBlocksNeedingAtLeastOnePass)
                    {
                        unreachableBlocks.Add(block.Ordinal);
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

                var catchBlockInputData = GetInput(resultBuilder[catchBlock]);
                if (catchBlockInputData != null)
                {
                    UpdateInput(resultBuilder, catchBlock, AnalysisDomain.Merge(catchBlockInputData, dataToMerge));
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

                foreach (var block in worklist.Concat(pendingBlocksNeedingAtLeastOnePass))
                {
                    if (block.Predecessors.IsEmpty || !HasUnprocessedPredecessorBlock(block))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool HasUnprocessedPredecessorBlock(BasicBlock block)
            {
                var predecessorsWithBranches = block.GetPredecessorsWithBranches(ordinalToBlockMap);
                return predecessorsWithBranches.Any(predecessorWithBranch =>
                    predecessorWithBranch.predecessorBlock.Ordinal < block.Ordinal &&
                    pendingBlocksNeedingAtLeastOnePass.Contains(predecessorWithBranch.predecessorBlock));
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
                    var destination = ordinalToBlockMap[firstFinally.FirstBlockOrdinal];
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
                        successor = new BranchWithInfo(destination: ordinalToBlockMap[finallyRegion.FirstBlockOrdinal]);
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
                            worklist.Enqueue(ordinalToBlockMap[catchRegion.FirstBlockOrdinal]);
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

        internal abstract TAnalysisResult ToResult(TAnalysisContext analysisContext, DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> dataFlowAnalysisResult);
        internal abstract TBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TAnalysisData> blockAnalysisData);
        private static TAnalysisData GetInput(DataFlowAnalysisInfo<TAnalysisData> result) => result.Input;
        private static TAnalysisData GetOutput(DataFlowAnalysisInfo<TAnalysisData> result) => result.Output;
        
        private static void UpdateInput(DataFlowAnalysisResultBuilder<TAnalysisData> builder, BasicBlock block, TAnalysisData newInput)
        {
            var currentData = builder[block];
            var newData = currentData.WithInput(newInput);
            builder.Update(block, newData);
        }

        private static void UpdateOutput(DataFlowAnalysisResultBuilder<TAnalysisData> builder, BasicBlock block, TAnalysisData newOutput)
        {
            var currentData = builder[block];
            var newData = currentData.WithOutput(newOutput);
            builder.Update(block, newData);
        }
    }
}