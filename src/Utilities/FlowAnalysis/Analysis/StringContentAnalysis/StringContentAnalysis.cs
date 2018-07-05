// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    using StringContentAnalysisDomain = PredicatedAnalysisDataDomain<StringContentAnalysisData, StringContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track string content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    internal partial class StringContentAnalysis : ForwardDataFlowAnalysis<StringContentAnalysisData, StringContentBlockAnalysisResult, StringContentAbstractValue>
    {
        private static readonly StringContentAnalysisDomain s_AnalysisDomain = new StringContentAnalysisDomain(CoreAnalysisDataDomain.Instance);

        private StringContentAnalysis(StringContentDataFlowOperationVisitor operationVisitor)
            : base(s_AnalysisDomain, operationVisitor)
        {
        }

        public static DataFlowAnalysisResult<StringContentBlockAnalysisResult, StringContentAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue> copyAnalysisResultOpt,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt = null,
            bool pessimisticAnalsysis = true)
        {
            var operationVisitor = new StringContentDataFlowOperationVisitor(StringContentAbstractValueDomain.Default, owningSymbol,
                wellKnownTypeProvider, cfg, pessimisticAnalsysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt);
            var nullAnalysis = new StringContentAnalysis(operationVisitor);
            return nullAnalysis.GetOrComputeResultCore(cfg, cacheResult: false);
        }

        internal override StringContentBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<StringContentAnalysisData> blockAnalysisData) => new StringContentBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
