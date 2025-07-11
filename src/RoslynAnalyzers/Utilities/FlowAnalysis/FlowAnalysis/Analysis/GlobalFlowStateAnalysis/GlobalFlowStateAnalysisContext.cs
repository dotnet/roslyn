// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;
    using InterproceduralGlobalFlowStateAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisValueSet>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="GlobalFlowStateAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class GlobalFlowStateAnalysisContext : AbstractDataFlowAnalysisContext<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        private GlobalFlowStateAnalysisContext(
            AbstractValueDomain<GlobalFlowStateAnalysisValueSet> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralGlobalFlowStateAnalysisData? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis: false,
                  copyAnalysisResult: null,
                  pointsToAnalysisResult,
                  valueContentAnalysisResult,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph,
                  interproceduralAnalysisData,
                  interproceduralAnalysisPredicate)
        {
        }

        internal static GlobalFlowStateAnalysisContext Create(
            AbstractValueDomain<GlobalFlowStateAnalysisValueSet> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult?> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
        {
            return new GlobalFlowStateAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResult,
                valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph: null,
                interproceduralAnalysisData: null, interproceduralAnalysisPredicate);
        }

        public override GlobalFlowStateAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralGlobalFlowStateAnalysisData? interproceduralAnalysisData)
        {
            RoslynDebug.Assert(copyAnalysisResult == null);
            return new GlobalFlowStateAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg,
                invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                pointsToAnalysisResult, valueContentAnalysisResult, TryGetOrComputeAnalysisResult,
                ControlFlowGraph, interproceduralAnalysisData, InterproceduralAnalysisPredicate);
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateAnalysisValueSet> obj)
        {
            return true;
        }
    }
}
