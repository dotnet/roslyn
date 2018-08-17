// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        protected TaintedDataAnalysis(AbstractAnalysisDomain<TaintedDataAnalysisData> analysisDomain, DataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAbstractValue> operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider)
        {
            return null;
        }

        internal override TaintedDataBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
