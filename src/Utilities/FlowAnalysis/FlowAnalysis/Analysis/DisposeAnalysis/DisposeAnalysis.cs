// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;
    using DisposeAnalysisDomain = MapAbstractDomain<AbstractLocation, DisposeAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track dispose state of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    public partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
        // reference type fields might be re-assigned to point to different objects in the called method.
        // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
        // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
        // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
        private const bool PessimisticAnalysis = false;

        internal static readonly DisposeAnalysisDomain DisposeAnalysisDomainInstance = new DisposeAnalysisDomain(DisposeAbstractValueDomain.Default);

        private DisposeAnalysis(DisposeAnalysisDomain analysisDomain, DisposeDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static DisposeAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields,
            bool exceptionPathsAnalysis,
            CancellationToken cancellationToken,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            bool performCopyAnalysisIfNotUserConfigured = false,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt = null,
            bool defaultDisposeOwnershipTransferAtConstructor = false,
            bool defaultDisposeOwnershipTransferAtMethodCall = false)
        {
            Debug.Assert(!owningSymbol.IsConfiguredToSkipAnalysis(analyzerOptions, rule, wellKnownTypeProvider.Compilation, cancellationToken));

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken);
            var disposeOwnershipTransferAtConstructor = analyzerOptions.GetDisposeOwnershipTransferAtConstructorOption(
                rule, defaultValue: defaultDisposeOwnershipTransferAtConstructor, cancellationToken);
            var disposeOwnershipTransferAtMethodCall = analyzerOptions.GetDisposeOwnershipTransferAtMethodCall(
                rule, defaultValue: defaultDisposeOwnershipTransferAtMethodCall, cancellationToken);
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor,
                disposeOwnershipTransferAtMethodCall, trackInstanceFields, exceptionPathsAnalysis,
                performCopyAnalysis: analyzerOptions.GetCopyAnalysisOption(rule, defaultValue: performCopyAnalysisIfNotUserConfigured, cancellationToken),
                out pointsToAnalysisResult);
        }

        private static DisposeAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields,
            bool exceptionPathsAnalysis,
            bool performCopyAnalysis,
            out PointsToAnalysisResult? pointsToAnalysisResult)
        {
            Debug.Assert(wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable, out _));

            pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(
                cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig,
                interproceduralAnalysisPredicateOpt, PessimisticAnalysis, performCopyAnalysis, exceptionPathsAnalysis);
            if (pointsToAnalysisResult == null)
            {
                return null;
            }

            if (cfg == null)
            {
                Debug.Fail("Expected non-null CFG");
                return null;
            }

            var analysisContext = DisposeAnalysisContext.Create(
                DisposeAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt,
                PessimisticAnalysis, exceptionPathsAnalysis, pointsToAnalysisResult, TryGetOrComputeResultForAnalysisContext,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor, disposeOwnershipTransferAtMethodCall, trackInstanceFields);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static DisposeAnalysisResult? TryGetOrComputeResultForAnalysisContext(DisposeAnalysisContext disposeAnalysisContext)
        {
            var operationVisitor = new DisposeDataFlowOperationVisitor(disposeAnalysisContext);
            var disposeAnalysis = new DisposeAnalysis(DisposeAnalysisDomainInstance, operationVisitor);
            return disposeAnalysis.TryGetOrComputeResultCore(disposeAnalysisContext, cacheResult: false);
        }

        protected override DisposeAnalysisResult ToResult(DisposeAnalysisContext analysisContext, DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> dataFlowAnalysisResult)
        {
            var operationVisitor = (DisposeDataFlowOperationVisitor)OperationVisitor;
            var trackedInstanceFieldPointsToMap = analysisContext.TrackInstanceFields ?
                operationVisitor.TrackedInstanceFieldPointsToMap :
                null;
            return new DisposeAnalysisResult(dataFlowAnalysisResult, trackedInstanceFieldPointsToMap);
        }

        protected override DisposeBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue> blockAnalysisData)
            => new DisposeBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
