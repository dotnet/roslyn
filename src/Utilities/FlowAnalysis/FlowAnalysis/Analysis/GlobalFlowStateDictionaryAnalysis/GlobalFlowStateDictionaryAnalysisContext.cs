// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.GlobalFlowStateDictionaryAnalysis
{
    using GlobalFlowStateDictionaryAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateDictionaryAnalysisValue>;
    using GlobalFlowStateDictionaryAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateDictionaryBlockAnalysisResult, GlobalFlowStateDictionaryAnalysisValue>;

    internal class GlobalFlowStateDictionaryAnalysisContext : AbstractDataFlowAnalysisContext<
        GlobalFlowStateDictionaryAnalysisData,
        GlobalFlowStateDictionaryAnalysisContext,
        GlobalFlowStateDictionaryAnalysisResult,
        GlobalFlowStateDictionaryAnalysisValue>
    {
        public GlobalFlowStateDictionaryAnalysisContext(
            AbstractValueDomain<GlobalFlowStateDictionaryAnalysisValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            bool exceptionPathsAnalysis,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult,
            Func<GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph = null,
            InterproceduralAnalysisData<GlobalFlowStateDictionaryAnalysisData, GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryAnalysisValue>? interproceduralAnalysisData = null,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null) : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, predicateAnalysis, exceptionPathsAnalysis, copyAnalysisResult, pointsToAnalysisResult, valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
        }

        public override GlobalFlowStateDictionaryAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult,
            InterproceduralAnalysisData<GlobalFlowStateDictionaryAnalysisData, GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryAnalysisValue>? interproceduralAnalysisData)
            => new(
                ValueDomain,
                WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                AnalyzerOptions,
                InterproceduralAnalysisConfiguration,
                PessimisticAnalysis,
                PredicateAnalysis,
                ExceptionPathsAnalysis,
                copyAnalysisResult,
                pointsToAnalysisResult,
                valueContentAnalysisResult,
                TryGetOrComputeAnalysisResult,
                ControlFlowGraph,
                interproceduralAnalysisData,
                InterproceduralAnalysisPredicate);

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<GlobalFlowStateDictionaryAnalysisData, GlobalFlowStateDictionaryAnalysisContext, GlobalFlowStateDictionaryAnalysisResult, GlobalFlowStateDictionaryAnalysisValue> obj)
            => true;

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
        }
    }
}
