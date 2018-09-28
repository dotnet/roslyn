// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
#if WORKSPACE
    using TBasicBlock = BasicBlock;
#else
    using TBasicBlock = BasicBlockBuilder;
#endif

    /// <summary>
    /// Analyzer to execute custom dataflow analysis on a control flow graph.
    /// </summary>
    /// <typeparam name="TBlockAnalysisData">Custom data tracked for each basic block with values at start of the block.</typeparam>
    internal interface IDataFlowAnalyzer<TBlockAnalysisData> : IDisposable
    {
        /// <summary>
        /// Gets empty analysis data for first analysis pass on a basic block.
        /// </summary>
        TBlockAnalysisData GetEmptyAnalysisData();

        /// <summary>
        /// Gets current analysis data for the given basic block.
        /// </summary>
        TBlockAnalysisData GetCurrentAnalysisData(TBasicBlock basicBlock);

        /// <summary>
        /// Updates the current analysis data for the given basic block.
        /// </summary>
        void SetCurrentAnalysisData(TBasicBlock basicBlock, TBlockAnalysisData data);

        /// <summary>
        /// Analyze the given basic block and return the block analysis data at the end of the block for it's succesors.
        /// </summary>
        TBlockAnalysisData AnalyzeBlock(TBasicBlock basicBlock, CancellationToken cancellationToken);

        /// <summary>
        /// Analyze the non-conditional fallthrough successor branch for the given basic block
        /// and return the block analysis data for the branch destination.
        /// </summary>
        TBlockAnalysisData AnalyzeNonConditionalBranch(TBasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken);

        /// <summary>
        /// Analyze the given conditional branch for the given basic block and return the
        /// block analysis data for the branch destinations for the fallthrough and
        /// conditional successor branches.
        /// </summary>
        (TBlockAnalysisData fallThroughSuccessorData, TBlockAnalysisData conditionalSuccessorData) AnalyzeConditionalBranch(
            TBasicBlock basicBlock,
            TBlockAnalysisData currentAnalysisData,
            CancellationToken cancellationToken);

        /// <summary>
        /// Merge the given block analysis data instances to produce the resultant merge data.
        /// </summary>
        TBlockAnalysisData Merge(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2, CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if both the given block analysis data instances should be considered equivalent by analysis.
        /// </summary>
        bool IsEqual(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2);

        /// <summary>
        /// Flag indicating if the dataflow analysis should run on unreachable blocks.
        /// </summary>
        bool AnalyzeUnreachableBlocks { get; }
    }
}

