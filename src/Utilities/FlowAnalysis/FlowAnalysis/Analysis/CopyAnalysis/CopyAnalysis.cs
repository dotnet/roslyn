// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="AnalysisEntity"/> instances that share the same value.
    /// </summary>
    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        private CopyAnalysis(CopyDataFlowOperationVisitor operationVisitor)
            : base(operationVisitor.AnalysisDomain, operationVisitor)
        {
        }

        public static CopyAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true,
            bool exceptionPathsAnalysis = false)
        {
            var pointsToAnalysisResultOpt = performPointsToAnalysis ?
                PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicateOpt, pessimisticAnalysis, performCopyAnalysis: false, exceptionPathsAnalysis) :
                null;
            var analysisContext = CopyAnalysisContext.Create(CopyAbstractValueDomain.Default, wellKnownTypeProvider,
                cfg, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, exceptionPathsAnalysis, pointsToAnalysisResultOpt,
                TryGetOrComputeResultForAnalysisContext, interproceduralAnalysisPredicateOpt);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static CopyAnalysisResult TryGetOrComputeResultForAnalysisContext(CopyAnalysisContext analysisContext)
        {
            var operationVisitor = new CopyDataFlowOperationVisitor(analysisContext);
            var copyAnalysis = new CopyAnalysis(operationVisitor);
            return copyAnalysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override CopyAnalysisResult ToResult(CopyAnalysisContext analysisContext, CopyAnalysisResult dataFlowAnalysisResult) => dataFlowAnalysisResult;
        protected override CopyBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, CopyAnalysisData blockAnalysisData)
            => new CopyBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
