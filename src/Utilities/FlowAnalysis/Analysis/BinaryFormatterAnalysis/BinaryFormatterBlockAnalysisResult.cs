// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using BinaryFormatterAnalysisData = IDictionary<AbstractLocation, BinaryFormatterAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="BinaryFormatterAnalysis"/> on a basic block.
    /// It stores BinaryFormatter values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class BinaryFormatterBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public BinaryFormatterBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<BinaryFormatterAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, BinaryFormatterAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, BinaryFormatterAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, BinaryFormatterAbstractValue> InputData { get; }
        public ImmutableDictionary<AbstractLocation, BinaryFormatterAbstractValue> OutputData { get; }
    }
}
