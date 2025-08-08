// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralValueContentAnalysisData = InterproceduralAnalysisData<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ValueContentAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class ValueContentAnalysisContext : AbstractDataFlowAnalysisContext<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentAbstractValue>
    {
        private ValueContentAnalysisContext(
            AbstractValueDomain<ValueContentAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            CopyAnalysisResult? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<ValueContentAnalysisContext, ValueContentAnalysisResult?> tryGetOrComputeAnalysisResult,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes,
            Func<IOperation, ValueContentAbstractValue>? getValueForAdditionalSupportedValueTypeOperation,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralValueContentAnalysisData? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig,
                  pessimisticAnalysis, predicateAnalysis: true, exceptionPathsAnalysis: false, copyAnalysisResult,
                  pointsToAnalysisResult, valueContentAnalysisResult: null, tryGetOrComputeAnalysisResult, parentControlFlowGraph,
                  interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
            AdditionalSupportedValueTypes = additionalSupportedValueTypes.IsDefault ? ImmutableArray<INamedTypeSymbol>.Empty : additionalSupportedValueTypes;
            GetValueForAdditionalSupportedValueTypeOperation = getValueForAdditionalSupportedValueTypeOperation;
        }

        public ImmutableArray<INamedTypeSymbol> AdditionalSupportedValueTypes { get; }
        public Func<IOperation, ValueContentAbstractValue>? GetValueForAdditionalSupportedValueTypeOperation { get; }

        internal static ValueContentAnalysisContext Create(
            AbstractValueDomain<ValueContentAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            CopyAnalysisResult? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<ValueContentAnalysisContext, ValueContentAnalysisResult?> tryGetOrComputeAnalysisResult,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes,
            Func<IOperation, ValueContentAbstractValue>? getValueContentValueForAdditionalSupportedValueTypeOperation,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
        {
            return new ValueContentAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions,
                interproceduralAnalysisConfig, pessimisticAnalysis, copyAnalysisResult, pointsToAnalysisResult,
                tryGetOrComputeAnalysisResult, additionalSupportedValueTypes, getValueContentValueForAdditionalSupportedValueTypeOperation,
                parentControlFlowGraph: null, interproceduralAnalysisData: null, interproceduralAnalysisPredicate);
        }

        public override ValueContentAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralValueContentAnalysisData? interproceduralAnalysisData)
        {
            Debug.Assert(valueContentAnalysisResult == null);

            return new ValueContentAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, copyAnalysisResult, pointsToAnalysisResult, TryGetOrComputeAnalysisResult,
                AdditionalSupportedValueTypes, GetValueForAdditionalSupportedValueTypeOperation, ControlFlowGraph, interproceduralAnalysisData,
                InterproceduralAnalysisPredicate);
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(AdditionalSupportedValueTypes));
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentAbstractValue> obj)
        {
            var other = (ValueContentAnalysisContext)obj;
            return HashUtilities.Combine(AdditionalSupportedValueTypes) == HashUtilities.Combine(other.AdditionalSupportedValueTypes);
        }
    }
}
