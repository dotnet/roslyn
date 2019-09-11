// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ValueContentAnalysisContext, ValueContentAnalysisResult> tryGetOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralValueContentAnalysisData interproceduralAnalysisDataOpt,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig,
                  pessimisticAnalysis, predicateAnalysis: true, exceptionPathsAnalysis: false, copyAnalysisResultOpt,
                  pointsToAnalysisResultOpt, valueContentAnalysisResultOpt: null, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt, interproceduralAnalysisPredicateOpt)
        {
        }

        internal static ValueContentAnalysisContext Create(
            AbstractValueDomain<ValueContentAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ValueContentAnalysisContext, ValueContentAnalysisResult> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
        {
            return new ValueContentAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions,
                interproceduralAnalysisConfig, pessimisticAnalysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt,
                tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt: null, interproceduralAnalysisDataOpt: null, interproceduralAnalysisPredicateOpt);
        }

        public override ValueContentAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedControlFlowGraph,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            InterproceduralValueContentAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(valueContentAnalysisResultOpt == null);

            return new ValueContentAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt, TryGetOrComputeAnalysisResult, ControlFlowGraph, interproceduralAnalysisData,
                InterproceduralAnalysisPredicateOpt);
        }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
        }
    }
}
