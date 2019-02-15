// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;
    using InterproceduralDisposeAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>, DisposeAnalysisContext, DisposeAbstractValue>;
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
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> getOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralDisposeAnalysisData interproceduralAnalysisDataOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis,
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
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> getOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields)
        {
            return new DisposeAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph,
                owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis,
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

            return new DisposeAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, DisposeOwnershipTransferLikelyTypes, TrackInstanceFields, ControlFlowGraph, interproceduralAnalysisData);
        }

        public ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes { get; }
        public bool TrackInstanceFields { get; }

        protected override void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder)
        {
            builder.Add(TrackInstanceFields.GetHashCode());
            builder.Add(HashUtilities.Combine(DisposeOwnershipTransferLikelyTypes));
        }
    }
}
