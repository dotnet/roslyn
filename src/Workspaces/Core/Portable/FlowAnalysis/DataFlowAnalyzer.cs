﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Analyzer to execute custom dataflow analysis on a control flow graph.
    /// </summary>
    /// <typeparam name="TBlockAnalysisData">Custom data tracked for each basic block with values at start of the block.</typeparam>
    internal abstract class DataFlowAnalyzer<TBlockAnalysisData> : IDisposable
    {
        /// <summary>
        /// Gets current analysis data for the given basic block.
        /// </summary>
        public abstract TBlockAnalysisData GetCurrentAnalysisData(BasicBlock basicBlock);

        /// <summary>
        /// Gets empty analysis data for first analysis pass on a basic block.
        /// </summary>
        public abstract TBlockAnalysisData GetEmptyAnalysisData();

        /// <summary>
        /// Updates the current analysis data for the given basic block.
        /// </summary>
        public abstract void SetCurrentAnalysisData(BasicBlock basicBlock, TBlockAnalysisData data);

        /// <summary>
        /// Analyze the given basic block and return the block analysis data at the end of the block for its successors.
        /// </summary>
        public abstract TBlockAnalysisData AnalyzeBlock(BasicBlock basicBlock, CancellationToken cancellationToken);

        /// <summary>
        /// Analyze the non-conditional fallthrough successor branch for the given basic block
        /// and return the block analysis data for the branch destination.
        /// </summary>
        public abstract TBlockAnalysisData AnalyzeNonConditionalBranch(BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken);

        /// <summary>
        /// Analyze the given conditional branch for the given basic block and return the
        /// block analysis data for the branch destinations for the fallthrough and
        /// conditional successor branches.
        /// </summary>
        public abstract (TBlockAnalysisData fallThroughSuccessorData, TBlockAnalysisData conditionalSuccessorData) AnalyzeConditionalBranch(
            BasicBlock basicBlock, TBlockAnalysisData currentAnalysisData, CancellationToken cancellationToken);

        /// <summary>
        /// Merge the given block analysis data instances to produce the resultant merge data.
        /// </summary>
        public abstract TBlockAnalysisData Merge(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2, CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if both the given block analysis data instances should be considered equivalent by analysis.
        /// </summary>
        public abstract bool IsEqual(TBlockAnalysisData analysisData1, TBlockAnalysisData analysisData2);

        /// <summary>
        /// Flag indicating if the dataflow analysis should run on unreachable blocks.
        /// </summary>
        public abstract bool AnalyzeUnreachableBlocks { get; }

        public virtual void Dispose()
        {
        }
    }
}
