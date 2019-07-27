// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track locations pointed to by <see cref="AnalysisEntity"/> and <see cref="IOperation"/> instances.
    /// </summary>
    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        internal static readonly AbstractValueDomain<PointsToAbstractValue> PointsToAbstractValueDomainInstance = PointsToAbstractValueDomain.Default;

        private PointsToAnalysis(PointsToAnalysisDomain analysisDomain, PointsToDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static PointsToAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true,
            bool exceptionPathsAnalysis = false)
        {
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                out var _, interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt,
                pessimisticAnalysis, performCopyAnalysis, exceptionPathsAnalysis);
        }

        public static PointsToAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            out CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = true,
            bool exceptionPathsAnalysis = false)
        {
            copyAnalysisResultOpt = performCopyAnalysis ?
                CopyAnalysis.CopyAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicateOpt, pessimisticAnalysis, performPointsToAnalysis: true, exceptionPathsAnalysis) :
                null;
            var analysisContext = PointsToAnalysisContext.Create(PointsToAbstractValueDomain.Default, wellKnownTypeProvider, cfg,
                owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, exceptionPathsAnalysis, copyAnalysisResultOpt,
                TryGetOrComputeResultForAnalysisContext, interproceduralAnalysisPredicateOpt);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static PointsToAnalysisResult TryGetOrComputeResultForAnalysisContext(PointsToAnalysisContext analysisContext)
        {
            using (var trackedEntitiesBuilder = new TrackedEntitiesBuilder())
            {
                var defaultPointsToValueGenerator = new DefaultPointsToValueGenerator(trackedEntitiesBuilder);
                var analysisDomain = new PointsToAnalysisDomain(defaultPointsToValueGenerator);
                var operationVisitor = new PointsToDataFlowOperationVisitor(trackedEntitiesBuilder, defaultPointsToValueGenerator, analysisDomain, analysisContext);
                var pointsToAnalysis = new PointsToAnalysis(analysisDomain, operationVisitor);
                return pointsToAnalysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
            }
        }

        internal static bool ShouldBeTracked(ITypeSymbol typeSymbol) => typeSymbol.IsReferenceTypeOrNullableValueType() ||
            typeSymbol is ITypeParameterSymbol typeParameter && !typeParameter.IsValueType;

        internal static bool ShouldBeTracked(AnalysisEntity analysisEntity)
            => ShouldBeTracked(analysisEntity.Type) || analysisEntity.IsLValueFlowCaptureEntity || analysisEntity.IsThisOrMeInstance;

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisData(PointsToAnalysisData data)
        {
            data.AssertValidPointsToAnalysisData();
        }

        protected override PointsToAnalysisResult ToResult(PointsToAnalysisContext analysisContext, DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> dataFlowAnalysisResult)
        {
            var operationVisitor = ((PointsToDataFlowOperationVisitor)OperationVisitor);
            return new PointsToAnalysisResult(
                dataFlowAnalysisResult,
                operationVisitor.GetEscapedLocationsThroughOperationsMap(),
                operationVisitor.GetEscapedLocationsThroughReturnValuesMap(),
                operationVisitor.GetEscapedLocationsThroughEntitiesMap());
        }
        protected override PointsToBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PointsToAnalysisData blockAnalysisData)
            => new PointsToBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}