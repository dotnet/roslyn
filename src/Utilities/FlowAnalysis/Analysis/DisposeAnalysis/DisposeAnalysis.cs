// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;
    using DisposeAnalysisDomain = MapAbstractDomain<AbstractLocation, DisposeAbstractValue>;
    using InterproceduralDisposeAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, DisposeAbstractValue>, DisposeAnalysisContext, DisposeAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track dispose state of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
        // reference type fields might be re-assigned to point to different objects in the called method.
        // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
        // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
        // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
        private const bool PessimisticAnalysis = false;

        public static readonly DisposeAnalysisDomain DisposeAnalysisDomainInstance = new DisposeAnalysisDomain(DisposeAbstractValueDomain.Default);

        private DisposeAnalysis(DisposeAnalysisDomain analysisDomain, DisposeDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static DisposeAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields,
            out PointsToAnalysisResult pointsToAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None)
        {
            Debug.Assert(cfg != null);
            Debug.Assert(wellKnownTypeProvider.IDisposable != null);
            Debug.Assert(owningSymbol != null);

            pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(
                cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, PessimisticAnalysis);
            var analysisContext = DisposeAnalysisContext.Create(
                DisposeAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol, interproceduralAnalysisKind, PessimisticAnalysis,
                pointsToAnalysisResult, GetOrComputeResultForAnalysisContext, disposeOwnershipTransferLikelyTypes, trackInstanceFields);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static DisposeAnalysisResult GetOrComputeResultForAnalysisContext(DisposeAnalysisContext disposeAnalysisContext)
        {
            var operationVisitor = new DisposeDataFlowOperationVisitor(disposeAnalysisContext);
            var disposeAnalysis = new DisposeAnalysis(DisposeAnalysisDomainInstance, operationVisitor);
            return disposeAnalysis.GetOrComputeResultCore(disposeAnalysisContext, cacheResult: false);
        }

        internal override DisposeAnalysisResult ToResult(DisposeAnalysisContext analysisContext, DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> dataFlowAnalysisResult)
        {
            var trackedInstanceFieldPointsToMap = analysisContext.TrackInstanceFields ?
                ((DisposeDataFlowOperationVisitor)OperationVisitor).TrackedInstanceFieldPointsToMap :
                null;
            return new DisposeAnalysisResult(dataFlowAnalysisResult, trackedInstanceFieldPointsToMap);
        }

        internal override DisposeBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<IDictionary<AbstractLocation, DisposeAbstractValue>> blockAnalysisData)
            => new DisposeBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
