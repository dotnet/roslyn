// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    internal class TaintedDataBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue> InputData { get; }
        public ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue> OutputData { get; }
        public bool IsReachable { get; }

        public TaintedDataBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue>.Empty;
            IsReachable = blockAnalysisData.Input?.IsReachableBlockData ?? true;
        }
    }
}
