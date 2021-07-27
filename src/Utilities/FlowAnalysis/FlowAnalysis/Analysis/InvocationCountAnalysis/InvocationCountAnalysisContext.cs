// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAbstractValue>;

    internal class InvocationCountAnalysisContext : AbstractDataFlowAnalysisContext<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountAbstractValue>
    {
        public InvocationCountAnalysisContext(
            AbstractValueDomain<InvocationCountAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            bool exceptionPathsAnalysis,
            Func<InvocationCountAnalysisContext, InvocationCountAnalysisResult?> tryGetOrComputeAnalysisResult,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult = null,
            PointsToAnalysisResult? pointsToAnalysisResult = null,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult = null,
            ControlFlowGraph? parentControlFlowGraph = null,
            InterproceduralAnalysisData<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAbstractValue>? interproceduralAnalysisData = null,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null) : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, predicateAnalysis, exceptionPathsAnalysis, copyAnalysisResult, pointsToAnalysisResult, valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
        }

        public override InvocationCountAnalysisContext ForkForInterproceduralAnalysis(IMethodSymbol invokedMethod, ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult, DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult, InterproceduralAnalysisData<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAbstractValue>? interproceduralAnalysisData)
        {
            // Do not support inter-process analysis
            throw new NotImplementedException();
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<InvocationCountAnalysisData, InvocationCountAnalysisContext, InvocationCountAnalysisResult, InvocationCountAbstractValue> obj)
        {
            return true;
        }
    }
}