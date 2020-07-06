// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public static PointsToAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            PointsToAnalysisKind pointsToAnalysisKind,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = false,
            bool exceptionPathsAnalysis = false)
        {
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                pointsToAnalysisKind, out _, interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt,
                pessimisticAnalysis, performCopyAnalysis, exceptionPathsAnalysis);
        }

        public static PointsToAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            PointsToAnalysisKind pointsToAnalysisKind,
            out CopyAnalysisResult? copyAnalysisResultOpt,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = false,
            bool exceptionPathsAnalysis = false)
        {
            if (pointsToAnalysisKind == PointsToAnalysisKind.None)
            {
                copyAnalysisResultOpt = null;
                return null;
            }

            copyAnalysisResultOpt = performCopyAnalysis ?
                CopyAnalysis.CopyAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicateOpt, pessimisticAnalysis, pointsToAnalysisKind, exceptionPathsAnalysis) :
                null;

            if (cfg == null)
            {
                Debug.Fail("Expected non-null CFG");
                return null;
            }

            var analysisContext = PointsToAnalysisContext.Create(PointsToAbstractValueDomain.Default, wellKnownTypeProvider, cfg,
                owningSymbol, analyzerOptions, pointsToAnalysisKind, interproceduralAnalysisConfig, pessimisticAnalysis, exceptionPathsAnalysis, copyAnalysisResultOpt,
                TryGetOrComputeResultForAnalysisContext, interproceduralAnalysisPredicateOpt);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static PointsToAnalysisResult? TryGetOrComputeResultForAnalysisContext(PointsToAnalysisContext analysisContext)
        {
            using var trackedEntitiesBuilder = new TrackedEntitiesBuilder(analysisContext.PointsToAnalysisKind);
            var defaultPointsToValueGenerator = new DefaultPointsToValueGenerator(trackedEntitiesBuilder);
            var analysisDomain = new PointsToAnalysisDomain(defaultPointsToValueGenerator);
            var operationVisitor = new PointsToDataFlowOperationVisitor(trackedEntitiesBuilder, defaultPointsToValueGenerator, analysisDomain, analysisContext);
            var pointsToAnalysis = new PointsToAnalysis(analysisDomain, operationVisitor);
            return pointsToAnalysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal static bool ShouldBeTracked([NotNullWhen(returnValue: true)] ITypeSymbol typeSymbol)
            => typeSymbol.IsReferenceTypeOrNullableValueType() ||
               typeSymbol?.IsRefLikeType == true ||
               typeSymbol is ITypeParameterSymbol typeParameter && !typeParameter.IsValueType;

        internal static bool ShouldBeTracked(AnalysisEntity analysisEntity, PointsToAnalysisKind pointsToAnalysisKind)
        {
            Debug.Assert(pointsToAnalysisKind != PointsToAnalysisKind.None);

            if (!ShouldBeTracked(analysisEntity.Type) &&
                !analysisEntity.IsLValueFlowCaptureEntity &&
                !analysisEntity.IsThisOrMeInstance)
            {
                return false;
            }

            return pointsToAnalysisKind == PointsToAnalysisKind.Complete || !analysisEntity.IsChildOrInstanceMemberNeedingCompletePointsToAnalysis();
        }

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisData(PointsToAnalysisData data)
        {
            data.AssertValidPointsToAnalysisData();
        }

        protected override PointsToAnalysisResult ToResult(PointsToAnalysisContext analysisContext, DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> dataFlowAnalysisResult)
        {
            var operationVisitor = (PointsToDataFlowOperationVisitor)OperationVisitor;
            return new PointsToAnalysisResult(
                dataFlowAnalysisResult,
                operationVisitor.GetEscapedLocationsThroughOperationsMap(),
                operationVisitor.GetEscapedLocationsThroughReturnValuesMap(),
                operationVisitor.GetEscapedLocationsThroughEntitiesMap(),
                operationVisitor.TrackedEntitiesBuilder);
        }
        protected override PointsToBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, PointsToAnalysisData blockAnalysisData)
            => new PointsToBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}