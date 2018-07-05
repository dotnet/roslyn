// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="DisposeAnalysis"/> on a basic block.
    /// It store dispose values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class DisposeBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public DisposeBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<DisposeAnalysisData> blockAnalysisData)
            : base (basicBlock)
        {
            InputData = blockAnalysisData.Input?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, DisposeAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, DisposeAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, DisposeAbstractValue> InputData { get; }
        public ImmutableDictionary<AbstractLocation, DisposeAbstractValue> OutputData { get; }
    }
}
