// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track value content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    internal partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        private ValueContentAnalysis(ValueContentDataFlowOperationVisitor operationVisitor)
            : base(ValueContentAnalysisDomain.Instance, operationVisitor)
        {
        }

        public static ValueContentAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue> copyAnalysisResultOpt,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt = null,
            bool pessimisticAnalsysis = true)
        {
            var analysisContext = new ValueContentAnalysisContext(ValueContentAbstractValueDomain.Default, wellKnownTypeProvider,
                cfg, owningSymbol, pessimisticAnalsysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ValueContentAnalysisResult GetOrComputeResultForAnalysisContext(ValueContentAnalysisContext analysisContext)
        {
            var operationVisitor = new ValueContentDataFlowOperationVisitor(analysisContext);
            var nullAnalysis = new ValueContentAnalysis(operationVisitor);
            return nullAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        internal override ValueContentAnalysisResult ToResult(ValueContentAnalysisContext analysisContext, ValueContentAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        internal override ValueContentBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<ValueContentAnalysisData> blockAnalysisData)
            => new ValueContentBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
