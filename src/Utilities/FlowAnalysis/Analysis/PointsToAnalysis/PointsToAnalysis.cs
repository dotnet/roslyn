// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track locations pointed to by <see cref="AnalysisEntity"/> and <see cref="IOperation"/> instances.
    /// </summary>
    internal partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        public static readonly AbstractValueDomain<PointsToAbstractValue> PointsToAbstractValueDomainInstance = PointsToAbstractValueDomain.Default;

        private PointsToAnalysis(PointsToAnalysisDomain analysisDomain, PointsToDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static PointsToAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true)
        {
            return GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider,
                out var _, interproceduralAnalysisKind, pessimisticAnalysis, performCopyAnalysis);
        }

        public static PointsToAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            out CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true)
        {
            copyAnalysisResultOpt = performCopyAnalysis ?
                CopyAnalysis.CopyAnalysis.GetOrComputeResult(
                    cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis) :
                null;
            var analysisContext = PointsToAnalysisContext.Create(PointsToAbstractValueDomain.Default, wellKnownTypeProvider, cfg,
                owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis, copyAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static PointsToAnalysisResult GetOrComputeResultForAnalysisContext(PointsToAnalysisContext analysisContext)
        {
            var defaultPointsToValueGenerator = new DefaultPointsToValueGenerator();
            var analysisDomain = new PointsToAnalysisDomain(defaultPointsToValueGenerator);
            var operationVisitor = new PointsToDataFlowOperationVisitor(defaultPointsToValueGenerator, analysisDomain, analysisContext);
            var pointsToAnalysis = new PointsToAnalysis(analysisDomain, operationVisitor);
            return pointsToAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        [Conditional("DEBUG")]
        public static void AssertValidPointsToAnalysisData(PointsToAnalysisData data)
        {
            data.AssertValidPointsToAnalysisData();
        }

        internal override PointsToAnalysisResult ToResult(PointsToAnalysisContext analysisContext, PointsToAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;
        internal override PointsToBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<PointsToAnalysisData> blockAnalysisData)
            => new PointsToBlockAnalysisResult(basicBlock, blockAnalysisData, ((PointsToAnalysisDomain)AnalysisDomain).DefaultPointsToValueGenerator.GetDefaultPointsToValueMap());
    }
}
