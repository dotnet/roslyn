// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;
    using DisposeAnalysisDomain = MapAbstractDomain<AbstractLocation, DisposeAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track dispose state of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        public static readonly DisposeAnalysisDomain DisposeAnalysisDomainInstance = new DisposeAnalysisDomain(DisposeAbstractValueDomain.Default);
        private DisposeAnalysis(DisposeAnalysisDomain analysisDomain, DisposeDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResult)
        {
            return GetOrComputeResultCore(cfg, owningSymbol, wellKnownTypeProvider, disposeOwnershipTransferLikelyTypes, pointsToAnalysisResult,
                trackInstanceFields: false, trackedInstanceFieldPointsToMap: out var _);
        }

        public static DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResult,
            bool trackInstanceFields,
            out ImmutableDictionary<IFieldSymbol, PointsToAnalysis.PointsToAbstractValue> trackedInstanceFieldPointsToMap)
        {
            return GetOrComputeResultCore(cfg, owningSymbol, wellKnownTypeProvider, disposeOwnershipTransferLikelyTypes, pointsToAnalysisResult,
                trackInstanceFields, out trackedInstanceFieldPointsToMap);
        }

        private static DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> GetOrComputeResultCore(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResult,
            bool trackInstanceFields,
            out ImmutableDictionary<IFieldSymbol, PointsToAnalysis.PointsToAbstractValue> trackedInstanceFieldPointsToMap)
        {
            Debug.Assert(cfg != null);
            Debug.Assert(wellKnownTypeProvider.IDisposable != null);
            Debug.Assert(owningSymbol != null);
            Debug.Assert(pointsToAnalysisResult != null);

            var operationVisitor = new DisposeDataFlowOperationVisitor(DisposeAbstractValueDomain.Default, owningSymbol, wellKnownTypeProvider,
                cfg, disposeOwnershipTransferLikelyTypes, trackInstanceFields, pointsToAnalysisResult);
            var disposeAnalysis = new DisposeAnalysis(DisposeAnalysisDomainInstance, operationVisitor);
            var result = disposeAnalysis.GetOrComputeResultCore(cfg, cacheResult: false);
            trackedInstanceFieldPointsToMap = trackInstanceFields ? operationVisitor.TrackedInstanceFieldPointsToMap : null;
            return result;
        }

        internal override DisposeBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<IDictionary<AbstractLocation, DisposeAbstractValue>> blockAnalysisData) => new DisposeBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
