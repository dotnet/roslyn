// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    /// <summary>
    /// Result from execution of <see cref="StringContentAnalysis"/> on a basic block.
    /// It stores string content values for each <see cref="AnalysisEntity"/> at the start and end of the basic block.
    /// </summary>
    internal class StringContentBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public StringContentBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<StringContentAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, StringContentAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, StringContentAbstractValue>.Empty;
            IsReachable = blockAnalysisData.Input?.IsReachableBlockData ?? true;
        }

        public ImmutableDictionary<AnalysisEntity, StringContentAbstractValue> InputData { get; }

        public ImmutableDictionary<AnalysisEntity, StringContentAbstractValue> OutputData { get; }
        public bool IsReachable { get; }
    }
}
