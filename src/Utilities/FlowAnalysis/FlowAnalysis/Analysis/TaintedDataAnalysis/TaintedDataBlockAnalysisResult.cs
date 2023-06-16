// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    /// <summary>
    /// Result from execution of <see cref="TaintedDataAnalysis"/> on a basic block.
    /// </summary>
    internal class TaintedDataBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue> Data { get; }

        public TaintedDataBlockAnalysisResult(BasicBlock basicBlock, TaintedDataAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, TaintedDataAbstractValue>.Empty;
        }
    }
}
