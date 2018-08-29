// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;
    using DisposeAnalysisDomain = MapAbstractDomain<AbstractLocation, DisposeAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track dispose state of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
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
            PointsToAnalysisResult pointsToAnalysisResult,
            bool trackInstanceFields = false)
        {
            Debug.Assert(cfg != null);
            Debug.Assert(wellKnownTypeProvider.IDisposable != null);
            Debug.Assert(owningSymbol != null);
            Debug.Assert(pointsToAnalysisResult != null);

            var analysisContext = new DisposeAnalysisContext(disposeOwnershipTransferLikelyTypes, trackInstanceFields,
                DisposeAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext);
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
