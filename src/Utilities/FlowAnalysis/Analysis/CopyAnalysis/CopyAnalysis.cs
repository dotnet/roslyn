// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisDomain = PredicatedAnalysisDataDomain<CopyAnalysisData, CopyAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="AnalysisEntity"/> instances that share the same value.
    /// </summary>
    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        private static readonly CopyAnalysisDomain s_AnalysisDomain = new CopyAnalysisDomain(CoreCopyAnalysisDataDomain.Instance);

        private CopyAnalysis(CopyDataFlowOperationVisitor operationVisitor)
            : base(s_AnalysisDomain, operationVisitor)
        {
        }

        public static CopyAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            PointsToAnalysisResult pointsToAnalysisResultOpt = null,
            bool pessimisticAnalysis = true)
        {
            var analysisContext = new CopyAnalysisContext(CopyAbstractValueDomain.Default, wellKnownTypeProvider, cfg,
                owningSymbol, pessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static CopyAnalysisResult GetOrComputeResultForAnalysisContext(CopyAnalysisContext analysisContext)
        {
            var operationVisitor = new CopyDataFlowOperationVisitor(analysisContext);
            var copyAnalysis = new CopyAnalysis(operationVisitor);
            return copyAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        [Conditional("DEBUG")]
        public static void AssertValidCopyAnalysisData(CopyAnalysisData data)
        {
            data.AssertValidCopyAnalysisData();
        }

        internal override CopyAnalysisResult ToResult(CopyAnalysisContext analysisContext, CopyAnalysisResult dataFlowAnalysisResult) => dataFlowAnalysisResult;
        internal override CopyBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<CopyAnalysisData> blockAnalysisData)
            => new CopyBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
