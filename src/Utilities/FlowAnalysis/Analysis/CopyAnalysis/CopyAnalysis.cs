// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="AnalysisEntity"/> instances that share the same value.
    /// </summary>
    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        private CopyAnalysis(CopyDataFlowOperationVisitor operationVisitor)
            : base(operationVisitor.AnalysisDomain, operationVisitor)
        {
        }

        public static CopyAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true)
        {
            var pointsToAnalysisResultOpt = performPointsToAnalysis ?
                PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(
                    cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis, performCopyAnalysis: false) :
                null;
            var analysisContext = CopyAnalysisContext.Create(CopyAbstractValueDomain.Default, wellKnownTypeProvider,
                cfg, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static CopyAnalysisResult GetOrComputeResultForAnalysisContext(CopyAnalysisContext analysisContext)
        {
            var operationVisitor = new CopyDataFlowOperationVisitor(analysisContext);
            var copyAnalysis = new CopyAnalysis(operationVisitor);
            return copyAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override CopyAnalysisResult ToResult(CopyAnalysisContext analysisContext, CopyAnalysisResult dataFlowAnalysisResult) => dataFlowAnalysisResult;
        internal override CopyBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<CopyAnalysisData> blockAnalysisData)
            => new CopyBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
