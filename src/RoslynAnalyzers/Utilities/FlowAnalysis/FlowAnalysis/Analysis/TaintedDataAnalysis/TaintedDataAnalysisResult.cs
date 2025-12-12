// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Analysis result from execution of <see cref="TaintedDataAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class TaintedDataAnalysisResult : DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        public TaintedDataAnalysisResult(
            DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> dataFlowAnalysisResult,
            ImmutableArray<TaintedDataSourceSink> taintedDataSourceSinks)
            : base(dataFlowAnalysisResult)
        {
            this.TaintedDataSourceSinks = taintedDataSourceSinks;
        }

        public ImmutableArray<TaintedDataSourceSink> TaintedDataSourceSinks { get; }
    }
}
