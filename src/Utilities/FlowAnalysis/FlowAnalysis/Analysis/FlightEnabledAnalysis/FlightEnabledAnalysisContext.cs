// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralFlightEnabledAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>, FlightEnabledAnalysisContext, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisResult = DataFlowAnalysisResult<FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="FlightEnabledAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class FlightEnabledAnalysisContext : AbstractDataFlowAnalysisContext<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledAbstractValue>
    {
        private FlightEnabledAnalysisContext(
            AbstractValueDomain<FlightEnabledAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            Func<FlightEnabledAnalysisContext, FlightEnabledAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraphOpt,
            InterproceduralFlightEnabledAnalysisData? interproceduralAnalysisDataOpt,
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

        internal static FlightEnabledAnalysisContext Create(
            AbstractValueDomain<FlightEnabledAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<FlightEnabledAnalysisContext, FlightEnabledAnalysisResult?> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt)
        {
            return new FlightEnabledAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResultOpt,
                valueContentAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null, interproceduralAnalysisPredicateOpt);
        }

        public override FlightEnabledAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            CopyAnalysisResult? copyAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            InterproceduralFlightEnabledAnalysisData? interproceduralAnalysisData)
        {
            RoslynDebug.Assert(copyAnalysisResultOpt == null);
            return new FlightEnabledAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg,
                invokedMethod, AnalyzerOptions, InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                pointsToAnalysisResultOpt, valueContentAnalysisResultOpt, TryGetOrComputeAnalysisResult,
                ControlFlowGraph, interproceduralAnalysisData, InterproceduralAnalysisPredicateOpt);
        }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
        }
    }
}
