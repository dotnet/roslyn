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
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<DisposeAnalysisContext, DisposeAnalysisResult?> tryGetOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralDisposeAnalysisData? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            Func<ISymbol, bool> isConfiguredToSkipAnalysis)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis,
                  copyAnalysisResult: null,
                  pointsToAnalysisResult,
                  valueContentAnalysisResult: null,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph,
                  interproceduralAnalysisData,
                  interproceduralAnalysisPredicate)
        {
            DisposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
            DisposeOwnershipTransferAtConstructor = disposeOwnershipTransferAtConstructor;
            DisposeOwnershipTransferAtMethodCall = disposeOwnershipTransferAtMethodCall;
            TrackInstanceFields = trackInstanceFields;
            IsConfiguredToSkipAnalysis = isConfiguredToSkipAnalysis;
        }

        internal static DisposeAnalysisContext Create(
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<DisposeAnalysisContext, DisposeAnalysisResult?> tryGetOrComputeAnalysisResult,
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool disposeOwnershipTransferAtConstructor,
            bool disposeOwnershipTransferAtMethodCall,
            bool trackInstanceFields,
            Func<ISymbol, bool> isConfiguredToSkipAnalysis)
        {
            return new DisposeAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph,
                owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                exceptionPathsAnalysis, pointsToAnalysisResult, tryGetOrComputeAnalysisResult,
                disposeOwnershipTransferLikelyTypes, disposeOwnershipTransferAtConstructor, disposeOwnershipTransferAtMethodCall,
                trackInstanceFields, parentControlFlowGraph: null, interproceduralAnalysisData: null,
                interproceduralAnalysisPredicate, isConfiguredToSkipAnalysis);
        }

        public override DisposeAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralDisposeAnalysisData? interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResult != null);
            Debug.Assert(copyAnalysisResult == null);
            Debug.Assert(valueContentAnalysisResult == null);

            return new DisposeAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                ExceptionPathsAnalysis, pointsToAnalysisResult, TryGetOrComputeAnalysisResult, DisposeOwnershipTransferLikelyTypes, DisposeOwnershipTransferAtConstructor,
                DisposeOwnershipTransferAtMethodCall, TrackInstanceFields, ControlFlowGraph, interproceduralAnalysisData, InterproceduralAnalysisPredicate, IsConfiguredToSkipAnalysis);
        }

        internal ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes { get; }
        internal bool DisposeOwnershipTransferAtConstructor { get; }
        internal bool DisposeOwnershipTransferAtMethodCall { get; }
        internal bool TrackInstanceFields { get; }
        internal Func<ISymbol, bool> IsConfiguredToSkipAnalysis { get; }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
            hashCode.Add(TrackInstanceFields.GetHashCode());
            hashCode.Add(DisposeOwnershipTransferAtConstructor.GetHashCode());
            hashCode.Add(DisposeOwnershipTransferAtMethodCall.GetHashCode());
            hashCode.Add(HashUtilities.Combine(DisposeOwnershipTransferLikelyTypes));
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeAbstractValue> obj)
        {
            var other = (DisposeAnalysisContext)obj;
            return TrackInstanceFields.GetHashCode() == other.TrackInstanceFields.GetHashCode()
                && DisposeOwnershipTransferAtConstructor.GetHashCode() == other.DisposeOwnershipTransferAtConstructor.GetHashCode()
                && DisposeOwnershipTransferAtMethodCall.GetHashCode() == other.DisposeOwnershipTransferAtMethodCall.GetHashCode()
                && HashUtilities.Combine(DisposeOwnershipTransferLikelyTypes) == HashUtilities.Combine(other.DisposeOwnershipTransferLikelyTypes);
        }
    }
}
