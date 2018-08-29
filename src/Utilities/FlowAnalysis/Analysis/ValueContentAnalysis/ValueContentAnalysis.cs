// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    /// <summary>
    /// Dataflow analysis to track value content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    internal partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        private ValueContentAnalysis(ValueContentDataFlowOperationVisitor operationVisitor)
            : base(ValueContentAnalysisDomain.Instance, operationVisitor)
        {
        }

        public static DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue> copyAnalysisResultOpt,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt = null,
            bool pessimisticAnalsysis = true)
        {
            var operationVisitor = new ValueContentDataFlowOperationVisitor(ValueContentAbstractValueDomain.Default, owningSymbol,
                wellKnownTypeProvider, cfg, pessimisticAnalsysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt);
            var nullAnalysis = new ValueContentAnalysis(operationVisitor);
            return nullAnalysis.GetOrComputeResultCore(cfg, cacheResult: false);
        }

        internal override ValueContentBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<ValueContentAnalysisData> blockAnalysisData) => new ValueContentBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
