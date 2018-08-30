// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    /// <summary>
    /// Result from execution of <see cref="ValueContentAnalysis"/> on a basic block.
    /// It stores data values for each <see cref="AnalysisEntity"/> at the start and end of the basic block.
    /// </summary>
    internal class ValueContentBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ValueContentBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<ValueContentAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue>.Empty;
            IsReachable = blockAnalysisData.Input?.IsReachableBlockData ?? true;
        }

        public ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue> InputData { get; }

        public ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue> OutputData { get; }
        public bool IsReachable { get; }
    }
}
