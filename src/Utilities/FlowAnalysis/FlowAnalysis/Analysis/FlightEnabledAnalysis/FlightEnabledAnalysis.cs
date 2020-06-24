// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using FlightEnabledAnalysisDomain = MapAbstractDomain<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisResult = DataFlowAnalysisResult<FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track FlightEnabled state.
    /// </summary>
    internal partial class FlightEnabledAnalysis : ForwardDataFlowAnalysis<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>
    {
        internal static readonly FlightEnabledAnalysisDomain FlightEnabledAnalysisDomainInstance = new FlightEnabledAnalysisDomain(FlightEnabledAbstractValueDomain.Instance);

        private FlightEnabledAnalysis(FlightEnabledAnalysisDomain analysisDomain, FlightEnabledDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static FlightEnabledAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<FlightEnabledAnalysisContext, FlightEnabledDataFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool performPointsToAnalysis,
            bool performValueContentAnalysis,
            CancellationToken cancellationToken,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            out ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null)
        {
            RoslynDebug.Assert(!performValueContentAnalysis || performPointsToAnalysis);

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, owningSymbol, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind, cancellationToken);
            return TryGetOrComputeResult(cfg, owningSymbol, createOperationVisitor, wellKnownTypeProvider, analyzerOptions,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicate, pessimisticAnalysis,
                performPointsToAnalysis, performValueContentAnalysis, out pointsToAnalysisResult, out valueContentAnalysisResult);
        }

        private static FlightEnabledAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<FlightEnabledAnalysisContext, FlightEnabledDataFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            bool pessimisticAnalysis,
            bool performPointsToAnalysis,
            bool performValueContentAnalysis,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            out ValueContentAnalysisResult? valueContentAnalysisResult)
        {
            RoslynDebug.Assert(!performValueContentAnalysis || performPointsToAnalysis);
            RoslynDebug.Assert(cfg != null);
            RoslynDebug.Assert(owningSymbol != null);

            pointsToAnalysisResult = performPointsToAnalysis ?
                PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(
                    cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicate, pessimisticAnalysis, performCopyAnalysis: false) :
                null;
            valueContentAnalysisResult = performValueContentAnalysis ?
                ValueContentAnalysis.ValueContentAnalysis.TryGetOrComputeResult(
                    cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, interproceduralAnalysisConfig, out _,
                    out pointsToAnalysisResult, pessimisticAnalysis, performPointsToAnalysis, performCopyAnalysis: false, interproceduralAnalysisPredicate) :
                null;

            var analysisContext = FlightEnabledAnalysisContext.Create(
                FlightEnabledAbstractValueDomain.Instance, wellKnownTypeProvider, cfg, owningSymbol,
                analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResult,
                valueContentAnalysisResult, c => TryGetOrComputeResultForAnalysisContext(c, createOperationVisitor), interproceduralAnalysisPredicate);
            return TryGetOrComputeResultForAnalysisContext(analysisContext, createOperationVisitor);
        }

        private static FlightEnabledAnalysisResult? TryGetOrComputeResultForAnalysisContext(
            FlightEnabledAnalysisContext flightEnabledAnalysisContext,
            Func<FlightEnabledAnalysisContext, FlightEnabledDataFlowOperationVisitor> createOperationVisitor)
        {
            var operationVisitor = createOperationVisitor(flightEnabledAnalysisContext);
            var flightEnabledAnalysis = new FlightEnabledAnalysis(FlightEnabledAnalysisDomainInstance, operationVisitor);
            return flightEnabledAnalysis.TryGetOrComputeResultCore(flightEnabledAnalysisContext, cacheResult: false);
        }

        protected override FlightEnabledAnalysisResult ToResult(FlightEnabledAnalysisContext analysisContext, FlightEnabledAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        protected override FlightEnabledBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, FlightEnabledAnalysisData data)
            => new FlightEnabledBlockAnalysisResult(basicBlock, data);
    }
}
