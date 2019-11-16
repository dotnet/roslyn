// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Subtype for all dataflow analyses on a control flow graph.
    /// It performs a worklist based approach to flow abstract data values for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> across the basic blocks until a fix point is reached.
    /// </summary>
    public abstract class DataFlowAnalysis<TAnalysisData, TAnalysisContext, TAnalysisResult, TBlockAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue>
        where TBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        private static readonly BoundedCache<IOperation, SingleThreadedConcurrentDictionary<TAnalysisContext, TAnalysisResult>> s_resultCache =
            new BoundedCache<IOperation, SingleThreadedConcurrentDictionary<TAnalysisContext, TAnalysisResult>>();

        protected DataFlowAnalysis(AbstractAnalysisDomain<TAnalysisData> analysisDomain, DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor)
        {
            AnalysisDomain = analysisDomain;
            OperationVisitor = operationVisitor;
        }

        protected AbstractAnalysisDomain<TAnalysisData> AnalysisDomain { get; }
        protected DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> OperationVisitor { get; }

        protected TAnalysisResult? TryGetOrComputeResultCore(TAnalysisContext analysisContext, bool cacheResult)
        {
            if (analysisContext == null)
            {
                throw new ArgumentNullException(nameof(analysisContext));
            }

            // Don't add interprocedural analysis result to our static results cache.
            if (!cacheResult || analysisContext.InterproceduralAnalysisDataOpt != null)
            {
                return Run(analysisContext);
            }

            var analysisResultsMap = s_resultCache.GetOrCreateValue(analysisContext.ControlFlowGraph.OriginalOperation);
            return analysisResultsMap.GetOrAdd(analysisContext, _ => Run(analysisContext));
        }

        private TAnalysisResult? Run(TAnalysisContext analysisContext)
        {
            var cfg = analysisContext.ControlFlowGraph;
            if (cfg?.SupportsFlowAnalysis() != true)
            {
                return default;
            }

            var resultBuilder = new DataFlowAnalysisResultBuilder<TAnalysisData>();
            var uniqueSuccessors = PooledHashSet<BasicBlock>.GetInstance();
            var finallyBlockSuccessorsMap = PooledDictionary<int, List<BranchWithInfo>>.GetInstance();
            var catchBlockInputDataMap = PooledDictionary<ControlFlowRegion, TAnalysisData>.GetInstance();
            var inputDataFromInfeasibleBranchesMap = PooledDictionary<int, TAnalysisData>.GetInstance();
            var unreachableBlocks = PooledHashSet<int>.GetInstance();
            var worklist = new SortedSet<int>();
            var pendingBlocksNeedingAtLeastOnePass = new SortedSet<int>(cfg.Blocks.Select(b => b.Ordinal));

            // Map from Ordinal -> (Ordinal, ControlFlowConditionKind)? with following semantics:
            //  1. Key is a valid basic block ordinal.
            //  2. Value tuple indicates the following:
            //      a. Non-null tuple value: Indicates a unique branch entering the block, with following tuple values:
            //         i. Ordinal of the single unique block from which analysis data has been transferred into the Key, 
            //            which is normally a predecessor but can be a non-predecessor block for finally/catch.
            //         ii. ControlFlowConditionKind indicating the nature of branch, i.e. conditional or fall through.
            //             This is required as CFG can have both conditional and fall through branches
            //             with the same source and destination blocks.
            //      b. Null tuple value: Block had analysis data flowing into it from multiple different branches.
            //     
            //  This map allows us to optimize the number of merge operations. We can avoid merge and directly
            //  overwrite analysis data into a successor if successor block has no entry or entry with non-null tuple value
            //  with the matching input branch.
            var blockToUniqueInputFlowMap = PooledDictionary<int, (int Ordinal, ControlFlowConditionKind BranchKind)?>.GetInstance();

            // Map from basic block ordinals that are destination of back edge(s) to the minimum block ordinal that dominates it,
            // i.e. for every '{key, value}' pair in the dictionary, 'key' is the destination of at least one back edge
            // and 'value' is the minimum ordinal such that there is no back edge to 'key' from any basic block with ordinal > 'value'.
            var loopRangeMap = PooledDictionary<int, int>.GetInstance();
            ComputeLoopRangeMap(cfg, loopRangeMap);

            TAnalysisData? normalPathsExitBlockData = null, exceptionPathsExitBlockDataOpt = null;

            try
            {

                // Add each basic block to the result.
                foreach (var block in cfg.Blocks)
                {
                    resultBuilder.Add(block);
                }

                var entry = cfg.GetEntry();

                // Initialize the input of the entry block.
                // For context sensitive inter-procedural analysis, use the provided initial analysis data.
                // Otherwise, initialize with the default bottom value of the analysis domain.
                var initialAnalysisDataOpt = analysisContext.InterproceduralAnalysisDataOpt?.InitialAnalysisData;
                UpdateInput(resultBuilder, entry, GetClonedAnalysisDataOrEmptyData(initialAnalysisDataOpt));

                // Add the block to the worklist.
                worklist.Add(entry.Ordinal);

                RunCore(cfg, worklist, pendingBlocksNeedingAtLeastOnePass, initialAnalysisDataOpt, resultBuilder,
                    uniqueSuccessors, finallyBlockSuccessorsMap, catchBlockInputDataMap, inputDataFromInfeasibleBranchesMap,
                    blockToUniqueInputFlowMap, loopRangeMap, exceptionPathsAnalysisPostPass: false);
                normalPathsExitBlockData = resultBuilder.ExitBlockOutputData;

                if (analysisContext.ExceptionPathsAnalysis)
                {
                    RoslynDebug.Assert(normalPathsExitBlockData != null);

                    // Clone and save exit block data
                    normalPathsExitBlockData = AnalysisDomain.Clone(normalPathsExitBlockData);

                    OperationVisitor.ExecutingExceptionPathsAnalysisPostPass = true;
                    foreach (var block in cfg.Blocks)
                    {
                        blockToUniqueInputFlowMap[block.Ordinal] = null;

                        // Skip entry block analysis.
                        if (block.Kind == BasicBlockKind.Entry)
                        {
                            continue;
                        }

                        if (block.IsReachable)
                        {
                            worklist.Add(block.Ordinal);
                        }
                        else
                        {
                            pendingBlocksNeedingAtLeastOnePass.Add(block.Ordinal);
                        }
                    }

                    RunCore(cfg, worklist, pendingBlocksNeedingAtLeastOnePass, initialAnalysisDataOpt, resultBuilder, uniqueSuccessors,
                        finallyBlockSuccessorsMap, catchBlockInputDataMap, inputDataFromInfeasibleBranchesMap,
                        blockToUniqueInputFlowMap, loopRangeMap, exceptionPathsAnalysisPostPass: true);
                    exceptionPathsExitBlockDataOpt = resultBuilder.ExitBlockOutputData;
                    OperationVisitor.ExecutingExceptionPathsAnalysisPostPass = false;
                }

                var mergedDataForUnhandledThrowOperationsOpt = OperationVisitor.GetMergedDataForUnhandledThrowOperations();

                var dataflowAnalysisResult = resultBuilder.ToResult(ToBlockResult, OperationVisitor.GetStateMap(),
                    OperationVisitor.GetPredicateValueKindMap(), OperationVisitor.GetReturnValueAndPredicateKind(), OperationVisitor.InterproceduralResultsMap,
                    resultBuilder.EntryBlockOutputData!, normalPathsExitBlockData!, exceptionPathsExitBlockDataOpt,
                    mergedDataForUnhandledThrowOperationsOpt, OperationVisitor.AnalysisDataForUnhandledThrowOperations,
                    OperationVisitor.TaskWrappedValuesMapOpt, cfg, OperationVisitor.ValueDomain.UnknownOrMayBeValue);
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
                blockToUniqueInputFlowMap.Free();
                loopRangeMap.Free();
            }
        }

        private void RunCore(
            ControlFlowGraph cfg,
            SortedSet<int> worklist,
            SortedSet<int> pendingBlocksNeedingAtLeastOnePass,
            TAnalysisData? initialAnalysisDataOpt,
            DataFlowAnalysisResultBuilder<TAnalysisData> resultBuilder,
            PooledHashSet<BasicBlock> uniqueSuccessors,
            PooledDictionary<int, List<BranchWithInfo>> finallyBlockSuccessorsMap,
            PooledDictionary<ControlFlowRegion, TAnalysisData> catchBlockInputDataMap,
            PooledDictionary<int, TAnalysisData> inputDataFromInfeasibleBranchesMap,
            PooledDictionary<int, (int Ordinal, ControlFlowConditionKind BranchKind)?> blockToUniqueInputFlowMap,
            PooledDictionary<int, int> loopRangeMap,
            bool exceptionPathsAnalysisPostPass)
        {
            var unreachableBlocks = PooledHashSet<int>.GetInstance();
            try
            {
                // Add each basic block to the result.
                foreach (var block in cfg.Blocks)
                {
                    if (!block.IsReachable)
                    {
                        unreachableBlocks.Add(block.Ordinal);
                    }
                }

                while (worklist.Count > 0 || pendingBlocksNeedingAtLeastOnePass.Count > 0)
                {
                    UpdateUnreachableBlocks();

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

                    // Ensure that we execute potential nested catch blocks before the finally region.
                    if (pendingBlocksNeedingAtLeastOnePass.Any())
                    {
                        var finallyRegionOpt = block.GetInnermostRegionStartedByBlock(ControlFlowRegionKind.Finally);
                        if (finallyRegionOpt?.EnclosingRegion.Kind == ControlFlowRegionKind.TryAndFinally)
                        {
                            // Add all catch blocks in the try region corresponding to the finally.
                            var tryRegion = finallyRegionOpt.EnclosingRegion.NestedRegions[0];
                            Debug.Assert(tryRegion.Kind == ControlFlowRegionKind.Try);

                            var nestedCatchBlockOrdinals = pendingBlocksNeedingAtLeastOnePass.Where(
                                p => p >= tryRegion.FirstBlockOrdinal &&
                                     p <= tryRegion.LastBlockOrdinal &&
                                     cfg.Blocks[p].GetInnermostRegionStartedByBlock(ControlFlowRegionKind.Catch) != null);
                            if (nestedCatchBlockOrdinals.Any())
                            {
                                foreach (var catchBlockOrdinal in nestedCatchBlockOrdinals)
                                {
                                    worklist.Add(catchBlockOrdinal);
                                }

                                // Also add back the finally start block to be processed after catch blocks.
                                worklist.Add(blockOrdinal);
                                continue;
                            }
                        }
                    }

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
                            Debug.Assert(!currentInfeasibleData.IsDisposed);
                            input = currentInfeasibleData;
                            inputDataFromInfeasibleBranchesMap.Remove(block.Ordinal);
                        }
                        else
                        {
                            // For catch and filter regions, we track the initial input data in the catchBlockInputDataMap.
                            ControlFlowRegion? enclosingTryAndCatchRegion = GetEnclosingTryAndCatchRegionIfStartsHandler(block);
                            if (enclosingTryAndCatchRegion != null &&
                                catchBlockInputDataMap.TryGetValue(enclosingTryAndCatchRegion, out var catchBlockInput))
                            {
                                Debug.Assert(enclosingTryAndCatchRegion.Kind == ControlFlowRegionKind.TryAndCatch);
                                Debug.Assert(block.EnclosingRegion.Kind == ControlFlowRegionKind.Catch || block.EnclosingRegion.Kind == ControlFlowRegionKind.Filter);
                                Debug.Assert(block.EnclosingRegion.FirstBlockOrdinal == block.Ordinal);
                                Debug.Assert(!catchBlockInput.IsDisposed);
                                input = catchBlockInput;

                                // Mark that all input into successorBlockOpt requires a merge.
                                blockToUniqueInputFlowMap[block.Ordinal] = null;
                            }
                        }

                        input = input != null ?
                            AnalysisDomain.Clone(input) :
                            GetClonedAnalysisDataOrEmptyData(initialAnalysisDataOpt);

                        UpdateInput(resultBuilder, block, input);
                    }

                    // Check if we are starting a try region which has one or more associated catch/filter regions.
                    // If so, we conservatively merge the input data for try region into the input data for the associated catch/filter regions.
                    if (block.EnclosingRegion?.Kind == ControlFlowRegionKind.Try &&
                        block.EnclosingRegion?.EnclosingRegion?.Kind == ControlFlowRegionKind.TryAndCatch &&
                        block.EnclosingRegion.EnclosingRegion.FirstBlockOrdinal == block.Ordinal)
                    {
                        MergeIntoCatchInputData(block.EnclosingRegion.EnclosingRegion, input, block);
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
                                UpdateFinallySuccessorsAndCatchInput(preadjustSuccessorWithBranch, newSuccessorInput, block);
                            }

                            // Certain branches have no destination (e.g. BranchKind.Throw), so we don't need to update the input data for the branch destination block.
                            var successorBlockOpt = successorWithBranch.Destination;
                            if (successorBlockOpt == null)
                            {
                                newSuccessorInput.Dispose();
                                continue;
                            }

                            if (exceptionPathsAnalysisPostPass)
                            {
                                // For exception paths analysis, we need to force re-analysis of entire finally region
                                // whenever we start try region analysis so analysis data for unhandled exceptions at end of the finally region is correctly updated.
                                if (successorBlockOpt.IsFirstBlockOfRegionKind(ControlFlowRegionKind.TryAndFinally, out var tryAndFinally))
                                {
                                    var finallyRegion = tryAndFinally.NestedRegions[1];
                                    Debug.Assert(finallyRegion.Kind == ControlFlowRegionKind.Finally);

                                    for (int i = finallyRegion.FirstBlockOrdinal; i <= finallyRegion.LastBlockOrdinal; i++)
                                    {
                                        worklist.Add(i);
                                    }
                                }
                            }

                            // Perf: We can stop tracking data for entities whose lifetime is limited by the leaving regions.
                            //       Below invocation explicitly drops such data from destination input.
                            newSuccessorInput = OperationVisitor.OnLeavingRegions(successorWithBranch.LeavingRegionLocals,
                                successorWithBranch.LeavingRegionFlowCaptures, block, newSuccessorInput);

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

                            var blockToSuccessorBranchKind = successorWithBranch.ControlFlowConditionKind;
                            var currentSuccessorInput = resultBuilder[successorBlockOpt];

                            // We need to merge the incoming analysis data if both the following conditions are satisfied:
                            //  1. Successor already has a non-null input from prior analysis iteration.
                            //  2. 'blockToPreviousInputBlockMap' has an entry for successor block such that one of the following conditions are satisfied:
                            //      a. Value is null, indicating it already had analysis data flow in from multiple branches OR
                            //      b. Value is non-null, indicating it has unique input from prior analysis, but the prior input
                            //         analysis data was from a different branch, i.e. either different source block or different condition kind.
                            var needsMerge = currentSuccessorInput != null &&
                                blockToUniqueInputFlowMap.TryGetValue(successorBlockOpt.Ordinal, out var uniqueInputBranchOpt) &&
                                (uniqueInputBranchOpt == null ||
                                 uniqueInputBranchOpt.Value.Ordinal != block.Ordinal ||
                                 uniqueInputBranchOpt.Value.BranchKind != blockToSuccessorBranchKind);

                            TAnalysisData mergedSuccessorInput;
                            if (needsMerge)
                            {
                                RoslynDebug.Assert(currentSuccessorInput != null);

                                // Mark that all input into successorBlockOpt requires a merge as we have non-unique input flow branches into successor block.
                                blockToUniqueInputFlowMap[successorBlockOpt.Ordinal] = null;

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
                                Debug.Assert(currentSuccessorInput == null || AnalysisDomain.Compare(currentSuccessorInput, newSuccessorInput) <= 0);
                                mergedSuccessorInput = newSuccessorInput;

                                // Mark that all input into successorBlockOpt can skip merge as long as it from the current input flow branch.
                                blockToUniqueInputFlowMap[successorBlockOpt.Ordinal] = (block.Ordinal, blockToSuccessorBranchKind);
                            }

                            // Input to successor has changed, so we need to update its new input and
                            // reprocess the successor by adding it to the worklist.
                            UpdateInput(resultBuilder, successorBlockOpt, mergedSuccessorInput);

                            if (isBackEdge)
                            {
                                // For back edges, analysis data in subsequent iterations needs
                                // to be merged with analysis data from previous iterations.
                                var dominatorBlockOrdinal = loopRangeMap[successorBlockOpt.Ordinal];
                                Debug.Assert(dominatorBlockOrdinal >= block.Ordinal);
                                Debug.Assert(dominatorBlockOrdinal >= successorBlockOpt.Ordinal);

                                for (int i = successorBlockOpt.Ordinal; i <= dominatorBlockOrdinal + 1; i++)
                                {
                                    blockToUniqueInputFlowMap[i] = null;
                                }
                            }

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
            }
            finally
            {
                unreachableBlocks.Free();
            }

            // Local functions.
            void UpdateUnreachableBlocks()
            {
                if (worklist.Count == 0)
                {
                    foreach (var blockOrdinal in pendingBlocksNeedingAtLeastOnePass)
                    {
                        unreachableBlocks.Add(blockOrdinal);
                    }
                }
            }

            static ControlFlowRegion? TryGetReachableCatchRegionStartingHandler(ControlFlowRegion tryAndCatchRegion, BasicBlock sourceBlock)
            {
                Debug.Assert(tryAndCatchRegion.Kind == ControlFlowRegionKind.TryAndCatch);

                // Get the catch region to merge input data.
                // Ensure that the source block is not itself within the catch region,
                // in which case a throw cannot enter the catch region. 
                var catchRegion = tryAndCatchRegion.NestedRegions.FirstOrDefault(region => region.Kind == ControlFlowRegionKind.Catch || region.Kind == ControlFlowRegionKind.FilterAndHandler);
                if (catchRegion == null || sourceBlock.Ordinal >= catchRegion.FirstBlockOrdinal)
                {
                    return null;
                }

                return catchRegion;
            }

            ControlFlowRegion? MergeIntoCatchInputData(ControlFlowRegion tryAndCatchRegion, TAnalysisData dataToMerge, BasicBlock sourceBlock)
            {
                var catchRegion = TryGetReachableCatchRegionStartingHandler(tryAndCatchRegion, sourceBlock);
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
            static ControlFlowRegion? GetEnclosingTryAndCatchRegionIfStartsHandler(BasicBlock block)
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

            IEnumerable<(BranchWithInfo successorWithBranch, BranchWithInfo? preadjustSuccessorWithBranch)> GetSuccessorsWithAdjustedBranches(BasicBlock basicBlock)
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
                    return branch.WithEmptyRegions(destination);
                }
                else
                {
                    return branch;
                }
            }

            // Updates the successors of finally blocks.
            // Also updates the merged input data tracked for catch blocks.
            void UpdateFinallySuccessorsAndCatchInput(BranchWithInfo branch, TAnalysisData branchData, BasicBlock sourceBlock)
            {
                // Compute and update finally successors.
                if (branch.FinallyRegions.Length > 0)
                {
                    var successor = branch.With(branchValueOpt: null, controlFlowConditionKind: ControlFlowConditionKind.None);
                    for (var i = branch.FinallyRegions.Length - 1; i >= 0; i--)
                    {
                        ControlFlowRegion finallyRegion = branch.FinallyRegions[i];
                        AddFinallySuccessor(finallyRegion, successor);
                        successor = new BranchWithInfo(destination: cfg.Blocks[finallyRegion.FirstBlockOrdinal]);
                    }
                }

                // Update catch input data.
                if (branch.LeavingRegions.Length > 0)
                {
                    foreach (var region in branch.LeavingRegions)
                    {
                        // If we have any nested finally region inside this try-catch, then mark the catch region
                        // as a successor of that nested finally region.
                        // Otherwise, merge the current data directly into the catch region.

                        if (region.Kind == ControlFlowRegionKind.TryAndCatch)
                        {
                            var hasNestedFinally = false;
                            if (branch.FinallyRegions.Length > 0)
                            {
                                var catchRegion = TryGetReachableCatchRegionStartingHandler(region, sourceBlock);
                                if (catchRegion != null)
                                {
                                    for (var i = branch.FinallyRegions.Length - 1; i >= 0; i--)
                                    {
                                        ControlFlowRegion finallyRegion = branch.FinallyRegions[i];
                                        if (finallyRegion.LastBlockOrdinal < catchRegion.FirstBlockOrdinal)
                                        {
                                            var successor = new BranchWithInfo(destination: cfg.Blocks[catchRegion.FirstBlockOrdinal]);
                                            AddFinallySuccessor(finallyRegion, successor);
                                            hasNestedFinally = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!hasNestedFinally)
                            {
                                // No nested finally regions inside this try-catch.
                                // Merge the current data directly into the catch region.
                                var catchRegion = MergeIntoCatchInputData(region, branchData, sourceBlock);
                                if (catchRegion != null)
                                {
                                    // We also need to enqueue the catch block into the worklist as there is no direct branch into catch.
                                    worklist.Add(catchRegion.FirstBlockOrdinal);
                                }
                            }
                        }
                    }
                }
            }

            void AddFinallySuccessor(ControlFlowRegion finallyRegion, BranchWithInfo successor)
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

        private TAnalysisData GetClonedAnalysisDataOrEmptyData(TAnalysisData? initialAnalysisDataOpt)
        {
            if (initialAnalysisDataOpt != null)
            {
                return AnalysisDomain.Clone(initialAnalysisDataOpt);
            }

            return OperationVisitor.GetEmptyAnalysisData();
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static TAnalysisData Flow(
            DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor,
            BasicBlock block,
            TAnalysisData data)
        {
            data = operationVisitor.OnStartBlockAnalysis(block, data);

            foreach (var statement in block.Operations)
            {
                data = operationVisitor.Flow(statement, block, data);
            }

            data = operationVisitor.OnEndBlockAnalysis(block, data);

            return data;
        }

        public static TAnalysisData FlowBranch(
            DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> operationVisitor,
            ControlFlowBranch branch,
            TAnalysisData data)
        {
            (data, _) = operationVisitor.FlowBranch(branch.Source, new BranchWithInfo(branch), data);
            return data;
        }
#pragma warning restore CA1000 // Do not declare static members on generic types

        protected abstract TAnalysisResult ToResult(TAnalysisContext analysisContext, DataFlowAnalysisResult<TBlockAnalysisResult, TAbstractAnalysisValue> dataFlowAnalysisResult);
        protected abstract TBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, TAnalysisData blockAnalysisData);

        private void UpdateInput(DataFlowAnalysisResultBuilder<TAnalysisData> builder, BasicBlock block, TAnalysisData newInput)
        {
            Debug.Assert(builder[block] == null || AnalysisDomain.Compare(builder[block]!, newInput) <= 0, "Non-monotonic update");
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

        private static void ComputeLoopRangeMap(ControlFlowGraph cfg, PooledDictionary<int, int> loopRangeMap)
        {
            for (int i = cfg.Blocks.Length - 1; i > 0; i--)
            {
                var block = cfg.Blocks[i];
                HandleBranch(block.FallThroughSuccessor);
                HandleBranch(block.ConditionalSuccessor);
            }

            void HandleBranch(ControlFlowBranch branch)
            {
                if (branch.IsBackEdge() && !loopRangeMap.ContainsKey(branch.Destination.Ordinal))
                {
                    var maxSuccessorOrdinal = Math.Max(branch.Destination.GetMaxSuccessorOrdinal(), branch.Source.Ordinal);

                    if (branch.FinallyRegions.Length > 0)
                    {
                        maxSuccessorOrdinal = Math.Max(maxSuccessorOrdinal, branch.FinallyRegions[branch.FinallyRegions.Length - 1].LastBlockOrdinal);
                    }

                    loopRangeMap.Add(branch.Destination.Ordinal, maxSuccessorOrdinal);
                }
            }
        }
    }
}