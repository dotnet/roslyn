// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAnalysisValue>;

    internal class InvocationCountAnalysisContext : AbstractDataFlowAnalysisContext<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountAnalysisValue>
    {
        public InvocationCountAnalysisContext(
            AbstractValueDomain<InvocationCountAnalysisValue> valueDomain,
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
            Func<InvocationCountAnalysisContext, InvocationCountAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralAnalysisData<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisValue>? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate) : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, predicateAnalysis, exceptionPathsAnalysis, copyAnalysisResult, pointsToAnalysisResult, valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
        }

        public override InvocationCountAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult,
            InterproceduralAnalysisData<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisValue>? interproceduralAnalysisData)
        {
            return new InvocationCountAnalysisContext(
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
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisResult, InvocationCountAnalysisValue> obj)
        {
            return true;
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
        }
    }
}
