// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities;
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

        internal static readonly DisposeAnalysisDomain DisposeAnalysisDomainInstance = new(DisposeAbstractValueDomain.Default);

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
            PointsToAnalysisKind defaultPointsToAnalysisKind,
            bool trackInstanceFields,
            bool exceptionPathsAnalysis,
            CancellationToken cancellationToken,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            bool performCopyAnalysisIfNotUserConfigured = false,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null,
            bool defaultDisposeOwnershipTransferAtConstructor = false,
            bool defaultDisposeOwnershipTransferAtMethodCall = false)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            Debug.Assert(!analyzerOptions.IsConfiguredToSkipAnalysis(rule, owningSymbol, wellKnownTypeProvider.Compilation));

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind);
            var disposeOwnershipTransferAtConstructor = analyzerOptions.GetDisposeOwnershipTransferAtConstructorOption(
                rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultValue: defaultDisposeOwnershipTransferAtConstructor);
            var disposeOwnershipTransferAtMethodCall = analyzerOptions.GetDisposeOwnershipTransferAtMethodCall(
                rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultValue: defaultDisposeOwnershipTransferAtMethodCall);
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicate,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor,
                disposeOwnershipTransferAtMethodCall, trackInstanceFields, exceptionPathsAnalysis,
                pointsToAnalysisKind: analyzerOptions.GetPointsToAnalysisKindOption(rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultPointsToAnalysisKind),
                performCopyAnalysis: analyzerOptions.GetCopyAnalysisOption(rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultValue: performCopyAnalysisIfNotUserConfigured),
                isConfiguredToSkipAnalysis: (ISymbol symbol) => analyzerOptions.IsConfiguredToSkipAnalysis(rule, symbol, owningSymbol, wellKnownTypeProvider.Compilation),
                out pointsToAnalysisResult);
        }

        private static DisposeAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields,
            bool exceptionPathsAnalysis,
            PointsToAnalysisKind pointsToAnalysisKind,
            bool performCopyAnalysis,
            Func<ISymbol, bool> isConfiguredToSkipAnalysis,
            out PointsToAnalysisResult? pointsToAnalysisResult)
        {
            Debug.Assert(wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable, out _));

            pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(
                cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, pointsToAnalysisKind, interproceduralAnalysisConfig,
                interproceduralAnalysisPredicate, PessimisticAnalysis, performCopyAnalysis, exceptionPathsAnalysis);
            if (pointsToAnalysisResult == null)
            {
                return null;
            }

            var analysisContext = DisposeAnalysisContext.Create(
                DisposeAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, interproceduralAnalysisPredicate,
                PessimisticAnalysis, exceptionPathsAnalysis, pointsToAnalysisResult, TryGetOrComputeResultForAnalysisContext,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor, disposeOwnershipTransferAtMethodCall, trackInstanceFields, isConfiguredToSkipAnalysis);
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
            => new(basicBlock, blockAnalysisData);
    }
}
