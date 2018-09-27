// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    /// <summary>
    /// Result from execution of <see cref="BinaryFormatterAnalysis"/> on a basic block.
    /// </summary>
    internal class BinaryFormatterBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ImmutableDictionary<AnalysisEntity, BinaryFormatterAbstractValue> InputData { get; }
        public ImmutableDictionary<AnalysisEntity, BinaryFormatterAbstractValue> OutputData { get; }
        public bool IsReachable { get; }

        public BinaryFormatterBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<BinaryFormatterAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, BinaryFormatterAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, BinaryFormatterAbstractValue>.Empty;
            IsReachable = blockAnalysisData.Input?.IsReachableBlockData ?? true;
        }
    }
}
