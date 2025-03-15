// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralCopyAnalysisData = InterproceduralAnalysisData<CopyAnalysisData, CopyAnalysisContext, CopyAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="CopyAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class CopyAnalysisContext : AbstractDataFlowAnalysisContext<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue>
    {
        private CopyAnalysisContext(
            AbstractValueDomain<CopyAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<CopyAnalysisContext, CopyAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralCopyAnalysisData? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: true, exceptionPathsAnalysis, copyAnalysisResult: null, pointsToAnalysisResult, valueContentAnalysisResult: null,
                  tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
        }

        internal static CopyAnalysisContext Create(
            AbstractValueDomain<CopyAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            Func<CopyAnalysisContext, CopyAnalysisResult?> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
        {
            return new CopyAnalysisContext(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions,
                  interproceduralAnalysisConfig, pessimisticAnalysis, exceptionPathsAnalysis, pointsToAnalysisResult, tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph: null, interproceduralAnalysisData: null, interproceduralAnalysisPredicate);
        }

        public override CopyAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralCopyAnalysisData? interproceduralAnalysisData)
        {
            return new CopyAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, ExceptionPathsAnalysis, pointsToAnalysisResult, TryGetOrComputeAnalysisResult, ControlFlowGraph, interproceduralAnalysisData,
                InterproceduralAnalysisPredicate);
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue> obj)
        {
            return true;
        }
    }
}
