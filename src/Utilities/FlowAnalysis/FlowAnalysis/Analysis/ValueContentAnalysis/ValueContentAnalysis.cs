// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track value content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        private ValueContentAnalysis(ValueContentDataFlowOperationVisitor operationVisitor)
            : base(ValueContentAnalysisDomain.Instance, operationVisitor)
        {
        }

        public static ValueContentAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true)
        {
            return TryGetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, analyzerOptions, rule,
                cancellationToken, out var _, out var _, interproceduralAnalysisKind,
                pessimisticAnalysis, performPointsToAnalysis);
        }

        public static ValueContentAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken,
            out CopyAnalysisResult copyAnalysisResultOpt,
            out PointsToAnalysisResult pointsToAnalysisResultOpt,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true,
            bool performCopyAnalysisIfNotUserConfigured = true,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt = null)
        {
            Debug.Assert(!owningSymbol.IsConfiguredToSkipAnalysis(analyzerOptions, rule, wellKnownTypeProvider.Compilation, cancellationToken));

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken);
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                interproceduralAnalysisConfig, out copyAnalysisResultOpt,
                out pointsToAnalysisResultOpt, pessimisticAnalysis, performPointsToAnalysis,
                performCopyAnalysis: analyzerOptions.GetCopyAnalysisOption(rule, defaultValue: performCopyAnalysisIfNotUserConfigured, cancellationToken),
                interproceduralAnalysisPredicateOpt: interproceduralAnalysisPredicateOpt);
        }

        internal static ValueContentAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            out CopyAnalysisResult copyAnalysisResultOpt,
            out PointsToAnalysisResult pointsToAnalysisResultOpt,
            bool pessimisticAnalysis = true,
            bool performPointsToAnalysis = true,
            bool performCopyAnalysis = true,
            InterproceduralAnalysisPredicate interproceduralAnalysisPredicateOpt = null)
        {
            copyAnalysisResultOpt = null;
            pointsToAnalysisResultOpt = performPointsToAnalysis ?
                PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, out copyAnalysisResultOpt,
                    interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt, pessimisticAnalysis, performCopyAnalysis) :
                null;

            if (cfg == null)
            {
                Debug.Fail("Expected non-null CFG");
                return null;
            }

            var analysisContext = ValueContentAnalysisContext.Create(
                ValueContentAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions,
                interproceduralAnalysisConfig, pessimisticAnalysis, copyAnalysisResultOpt,
                pointsToAnalysisResultOpt, TryGetOrComputeResultForAnalysisContext, interproceduralAnalysisPredicateOpt);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ValueContentAnalysisResult TryGetOrComputeResultForAnalysisContext(ValueContentAnalysisContext analysisContext)
        {
            var operationVisitor = new ValueContentDataFlowOperationVisitor(analysisContext);
            var nullAnalysis = new ValueContentAnalysis(operationVisitor);
            return nullAnalysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override ValueContentAnalysisResult ToResult(ValueContentAnalysisContext analysisContext, ValueContentAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        protected override ValueContentBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, ValueContentAnalysisData blockAnalysisData)
            => new ValueContentBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
