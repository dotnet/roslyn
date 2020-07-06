// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralGlobalFlowStateAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;
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
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraphOpt,
            InterproceduralGlobalFlowStateAnalysisData? interproceduralAnalysisDataOpt,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph,
                  owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis: false,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResultOpt,
                  valueContentAnalysisResultOpt,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt,
                  interproceduralAnalysisPredicateOpt)
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
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult?> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt)
        {
            return new GlobalFlowStateAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResultOpt,
                valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null, interproceduralAnalysisPredicateOpt);
        }

        public override GlobalFlowStateAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            CopyAnalysisResult? copyAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            InterproceduralGlobalFlowStateAnalysisData? interproceduralAnalysisData)
        {
            RoslynDebug.Assert(copyAnalysisResultOpt == null);
            return new GlobalFlowStateAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg,
                invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                pointsToAnalysisResultOpt, valueContentAnalysisResultOpt, TryGetOrComputeAnalysisResult,
                ControlFlowGraph, interproceduralAnalysisData, InterproceduralAnalysisPredicateOpt);
        }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
        }
    }
}
