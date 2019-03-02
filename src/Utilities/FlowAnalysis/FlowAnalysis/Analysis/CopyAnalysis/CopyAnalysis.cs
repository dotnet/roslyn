// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
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

        public static CopyAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken);
            return GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider,
                interproceduralAnalysisConfig, pessimisticAnalysis, performPointsToAnalysis);
        }

        public static CopyAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true,
            bool exceptionPathsAnalysis = false)
        {
            var pointsToAnalysisResultOpt = performPointsToAnalysis ?
                PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(
                    cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisConfig, pessimisticAnalysis, performCopyAnalysis: false, exceptionPathsAnalysis) :
                null;
            var analysisContext = CopyAnalysisContext.Create(CopyAbstractValueDomain.Default, wellKnownTypeProvider,
                cfg, owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis, exceptionPathsAnalysis, pointsToAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static CopyAnalysisResult GetOrComputeResultForAnalysisContext(CopyAnalysisContext analysisContext)
        {
            var operationVisitor = new CopyDataFlowOperationVisitor(analysisContext);
            var copyAnalysis = new CopyAnalysis(operationVisitor);
            return copyAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override CopyAnalysisResult ToResult(CopyAnalysisContext analysisContext, CopyAnalysisResult dataFlowAnalysisResult) => dataFlowAnalysisResult;
        protected override CopyBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, CopyAnalysisData blockAnalysisData)
            => new CopyBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
