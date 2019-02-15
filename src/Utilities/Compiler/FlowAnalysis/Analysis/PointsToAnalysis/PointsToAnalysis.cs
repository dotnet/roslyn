// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

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
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken);
            return GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider,
                out var _, interproceduralAnalysisConfig, pessimisticAnalysis, performCopyAnalysis);
        }

        public static PointsToAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true)
        {
            return GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider,
                out var _, interproceduralAnalysisConfig, pessimisticAnalysis, performCopyAnalysis);
        }

        public static PointsToAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            out CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true)
        {
            copyAnalysisResultOpt = performCopyAnalysis ?
                CopyAnalysis.CopyAnalysis.GetOrComputeResult(
                    cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisConfig, pessimisticAnalysis) :
                null;
            var analysisContext = PointsToAnalysisContext.Create(PointsToAbstractValueDomain.Default, wellKnownTypeProvider, cfg,
                owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis, copyAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static PointsToAnalysisResult GetOrComputeResultForAnalysisContext(PointsToAnalysisContext analysisContext)
        {
            using (var trackedEntitiesBuilder = new TrackedEntitiesBuilder())
            {
                var defaultPointsToValueGenerator = new DefaultPointsToValueGenerator(trackedEntitiesBuilder);
                var analysisDomain = new PointsToAnalysisDomain(defaultPointsToValueGenerator);
                var operationVisitor = new PointsToDataFlowOperationVisitor(trackedEntitiesBuilder, defaultPointsToValueGenerator, analysisDomain, analysisContext);
                var pointsToAnalysis = new PointsToAnalysis(analysisDomain, operationVisitor);
                return pointsToAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
            }
        }

        public static bool ShouldBeTracked(ITypeSymbol typeSymbol) => typeSymbol.IsReferenceTypeOrNullableValueType();

        public static bool ShouldBeTracked(AnalysisEntity analysisEntity)
            => ShouldBeTracked(analysisEntity.Type) || analysisEntity.IsLValueFlowCaptureEntity || analysisEntity.IsThisOrMeInstance;

        [Conditional("DEBUG")]
        public static void AssertValidPointsToAnalysisData(PointsToAnalysisData data)
        {
            data.AssertValidPointsToAnalysisData();
        }

        internal override PointsToAnalysisResult ToResult(PointsToAnalysisContext analysisContext, PointsToAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;
        internal override PointsToBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PointsToAnalysisData blockAnalysisData)
            => new PointsToBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
