// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisDomain = MapAbstractDomain<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track set of global <see cref="IAbstractAnalysisValue"/>s enabled on each control flow path at each <see cref="IOperation"/> in the <see cref="ControlFlowGraph"/>.
    /// </summary>
    internal partial class GlobalFlowStateAnalysis : ForwardDataFlowAnalysis<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        internal static readonly GlobalFlowStateAnalysisDomain GlobalFlowStateAnalysisDomainInstance = new GlobalFlowStateAnalysisDomain(GlobalFlowStateAnalysisValueSetDomain.Instance);

        private GlobalFlowStateAnalysis(GlobalFlowStateAnalysisDomain analysisDomain, GlobalFlowStateDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static GlobalFlowStateAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateDataFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool performValueContentAnalysis,
            CancellationToken cancellationToken,
            out ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, owningSymbol, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind, cancellationToken);
            var pointsToAnalysisKind = analyzerOptions.GetPointsToAnalysisKindOption(rule, owningSymbol, wellKnownTypeProvider.Compilation,
                defaultValue: PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties, cancellationToken);
            return TryGetOrComputeResult(cfg, owningSymbol, createOperationVisitor, wellKnownTypeProvider, analyzerOptions,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicate, pointsToAnalysisKind, pessimisticAnalysis,
                performValueContentAnalysis, out valueContentAnalysisResult);
        }

        private static GlobalFlowStateAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateDataFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            PointsToAnalysisKind pointsToAnalysisKind,
            bool pessimisticAnalysis,
            bool performValueContentAnalysis,
            out ValueContentAnalysisResult? valueContentAnalysisResult)
        {
            RoslynDebug.Assert(cfg != null);
            RoslynDebug.Assert(owningSymbol != null);

            PointsToAnalysisResult? pointsToAnalysisResult = null;
            valueContentAnalysisResult = performValueContentAnalysis ?
                ValueContentAnalysis.ValueContentAnalysis.TryGetOrComputeResult(
                    cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                    pointsToAnalysisKind, interproceduralAnalysisConfig, out _,
                    out pointsToAnalysisResult, pessimisticAnalysis,
                    performCopyAnalysis: false, interproceduralAnalysisPredicate) :
                null;

            pointsToAnalysisResult ??= PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(
                cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                pointsToAnalysisKind, interproceduralAnalysisConfig, interproceduralAnalysisPredicate);

            var analysisContext = GlobalFlowStateAnalysisContext.Create(
                GlobalFlowStateAnalysisValueSetDomain.Instance, wellKnownTypeProvider, cfg, owningSymbol,
                analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResult,
                valueContentAnalysisResult, c => TryGetOrComputeResultForAnalysisContext(c, createOperationVisitor), interproceduralAnalysisPredicate);
            return TryGetOrComputeResultForAnalysisContext(analysisContext, createOperationVisitor);
        }

        private static GlobalFlowStateAnalysisResult? TryGetOrComputeResultForAnalysisContext(
            GlobalFlowStateAnalysisContext analysisContext,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateDataFlowOperationVisitor> createOperationVisitor)
        {
            var operationVisitor = createOperationVisitor(analysisContext);
            var analysis = new GlobalFlowStateAnalysis(GlobalFlowStateAnalysisDomainInstance, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        protected override GlobalFlowStateAnalysisResult ToResult(GlobalFlowStateAnalysisContext analysisContext, GlobalFlowStateAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        protected override GlobalFlowStateBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, GlobalFlowStateAnalysisData data)
            => new GlobalFlowStateBlockAnalysisResult(basicBlock, data);
    }
}
