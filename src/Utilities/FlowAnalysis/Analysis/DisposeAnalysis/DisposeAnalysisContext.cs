// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;
    using InterproceduralDisposeAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, DisposeAbstractValue>, DisposeAnalysisContext, DisposeAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="DisposeAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class DisposeAnalysisContext : AbstractDataFlowAnalysisContext<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeAbstractValue>
    {
        private DisposeAnalysisContext(
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> getOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralDisposeAnalysisData interproceduralAnalysisDataOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis,
                  predicateAnalysis: false,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
            DisposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
            TrackInstanceFields = trackInstanceFields;
        }

        public static DisposeAnalysisContext Create(
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> getOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields)
        {
            return new DisposeAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph,
                owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis,
                pointsToAnalysisResultOpt, getOrComputeAnalysisResult,
                disposeOwnershipTransferLikelyTypes, trackInstanceFields,
                parentControlFlowGraphOpt: null, interproceduralAnalysisDataOpt: null);
        }

        public override DisposeAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedControlFlowGraph,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralDisposeAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);

            return new DisposeAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, InterproceduralAnalysisKind, PessimisticAnalysis,
                pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, DisposeOwnershipTransferLikelyTypes, TrackInstanceFields, ControlFlowGraph, interproceduralAnalysisData);
        }

        public ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes { get; }
        public bool TrackInstanceFields { get; }

        protected override int GetHashCode(int hashCode)
            => HashUtilities.Combine(TrackInstanceFields.GetHashCode(),
               HashUtilities.Combine(DisposeOwnershipTransferLikelyTypes, hashCode));
    }
}
