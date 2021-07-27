// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
