// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;
    using InterproceduralDisposeAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>, DisposeAnalysisContext, DisposeAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="DisposeAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class DisposeAnalysisContext : AbstractDataFlowAnalysisContext<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeAbstractValue>
    {
        private DisposeAnalysisContext(
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> tryGetOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralDisposeAnalysisData interproceduralAnalysisDataOpt,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResultOpt,
                  valueContentAnalysisResultOpt: null,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt,
                  interproceduralAnalysisPredicateOpt)
        {
            DisposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
            DisposeOwnershipTransferAtConstructor = disposeOwnershipTransferAtConstructor;
            DisposeOwnershipTransferAtMethodCall = disposeOwnershipTransferAtMethodCall;
            TrackInstanceFields = trackInstanceFields;
        }

        internal static DisposeAnalysisContext Create(
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> tryGetOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields)
        {
            return new DisposeAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph,
                owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                exceptionPathsAnalysis, pointsToAnalysisResultOpt, tryGetOrComputeAnalysisResult,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor, disposeOwnershipTransferAtMethodCall,
                trackInstanceFields, parentControlFlowGraphOpt: null, interproceduralAnalysisDataOpt: null,
                interproceduralAnalysisPredicateOpt);
        }

        public override DisposeAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedControlFlowGraph,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            InterproceduralDisposeAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);
            Debug.Assert(valueContentAnalysisResultOpt == null);

            return new DisposeAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                ExceptionPathsAnalysis, pointsToAnalysisResultOpt, TryGetOrComputeAnalysisResult, DisposeOwnershipTransferLikelyTypes, DisposeOwnershipTransferAtConstructor,
                DisposeOwnershipTransferAtMethodCall, TrackInstanceFields, ControlFlowGraph, interproceduralAnalysisData, InterproceduralAnalysisPredicateOpt);
        }

        internal ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes { get; }
        internal bool DisposeOwnershipTransferAtConstructor { get; }
        internal bool DisposeOwnershipTransferAtMethodCall { get; }
        internal bool TrackInstanceFields { get; }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
            addPart(TrackInstanceFields.GetHashCode());
            addPart(DisposeOwnershipTransferAtConstructor.GetHashCode());
            addPart(DisposeOwnershipTransferAtMethodCall.GetHashCode());
            addPart(HashUtilities.Combine(DisposeOwnershipTransferLikelyTypes));
        }
    }
}
