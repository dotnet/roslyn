// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralPointsToAnalysisData = InterproceduralAnalysisData<PointsToAnalysisData, PointsToAnalysisContext, PointsToAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="PointsToAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class PointsToAnalysisContext : AbstractDataFlowAnalysisContext<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToAbstractValue>
    {
        private PointsToAnalysisContext(
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            Func<PointsToAnalysisContext, PointsToAnalysisResult> tryGetOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralPointsToAnalysisData interproceduralAnalysisDataOpt,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: true, exceptionPathsAnalysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt: null, valueContentAnalysisResultOpt: null,
                  tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt, interproceduralAnalysisDataOpt, interproceduralAnalysisPredicateOpt)
        {
        }

        internal static PointsToAnalysisContext Create(
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            Func<PointsToAnalysisContext, PointsToAnalysisResult> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt)
        {
            return new PointsToAnalysisContext(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig,
                pessimisticAnalysis, exceptionPathsAnalysis, copyAnalysisResultOpt, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null, interproceduralAnalysisPredicateOpt);
        }

        public override PointsToAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedControlFlowGraph,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            InterproceduralPointsToAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt == null);
            Debug.Assert(valueContentAnalysisResultOpt == null);

            return new PointsToAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, ExceptionPathsAnalysis, copyAnalysisResultOpt, TryGetOrComputeAnalysisResult, ControlFlowGraph, interproceduralAnalysisData,
                InterproceduralAnalysisPredicateOpt);
        }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
        }
    }
}
