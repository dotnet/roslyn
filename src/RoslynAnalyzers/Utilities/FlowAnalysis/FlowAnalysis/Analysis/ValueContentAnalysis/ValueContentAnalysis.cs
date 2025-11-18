// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track value content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        internal static readonly AbstractValueDomain<ValueContentAbstractValue> ValueDomainInstance = ValueContentAbstractValueDomain.Default;

        private ValueContentAnalysis(ValueContentAnalysisDomain analysisDomain, ValueContentDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static ValueContentAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            PointsToAnalysisKind defaultPointsToAnalysisKind,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true)
        {
            return TryGetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, analyzerOptions, rule,
                defaultPointsToAnalysisKind, out var _, out var _, interproceduralAnalysisKind, pessimisticAnalysis);
        }

        public static ValueContentAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            PointsToAnalysisKind defaultPointsToAnalysisKind,
            out CopyAnalysisResult? copyAnalysisResult,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysisIfNotUserConfigured = false,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes = default,
            Func<IOperation, ValueContentAbstractValue>? getValueContentValueForAdditionalSupportedValueTypeOperation = null)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            Debug.Assert(!analyzerOptions.IsConfiguredToSkipAnalysis(rule, owningSymbol, wellKnownTypeProvider.Compilation));

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind);
            return TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                pointsToAnalysisKind: analyzerOptions.GetPointsToAnalysisKindOption(rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultPointsToAnalysisKind),
                interproceduralAnalysisConfig, out copyAnalysisResult,
                out pointsToAnalysisResult, pessimisticAnalysis,
                performCopyAnalysis: analyzerOptions.GetCopyAnalysisOption(rule, owningSymbol, wellKnownTypeProvider.Compilation, defaultValue: performCopyAnalysisIfNotUserConfigured),
                interproceduralAnalysisPredicate,
                additionalSupportedValueTypes,
                getValueContentValueForAdditionalSupportedValueTypeOperation);
        }

        internal static ValueContentAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            PointsToAnalysisKind pointsToAnalysisKind,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            out CopyAnalysisResult? copyAnalysisResult,
            out PointsToAnalysisResult? pointsToAnalysisResult,
            bool pessimisticAnalysis = true,
            bool performCopyAnalysis = false,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes = default,
            Func<IOperation, ValueContentAbstractValue>? getValueContentValueForAdditionalSupportedValueTypeOperation = null)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            copyAnalysisResult = null;
            pointsToAnalysisResult = pointsToAnalysisKind != PointsToAnalysisKind.None ?
                PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider, pointsToAnalysisKind, out copyAnalysisResult,
                    interproceduralAnalysisConfig, interproceduralAnalysisPredicate, pessimisticAnalysis, performCopyAnalysis) :
                null;

            var analysisContext = ValueContentAnalysisContext.Create(
                ValueDomainInstance, wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions,
                interproceduralAnalysisConfig, pessimisticAnalysis, copyAnalysisResult,
                pointsToAnalysisResult, TryGetOrComputeResultForAnalysisContext,
                additionalSupportedValueTypes, getValueContentValueForAdditionalSupportedValueTypeOperation,
                interproceduralAnalysisPredicate);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ValueContentAnalysisResult? TryGetOrComputeResultForAnalysisContext(ValueContentAnalysisContext analysisContext)
        {
            var analysisDomain = new ValueContentAnalysisDomain(analysisContext.PointsToAnalysisResult);
            var operationVisitor = new ValueContentDataFlowOperationVisitor(analysisDomain, analysisContext);
            var nullAnalysis = new ValueContentAnalysis(analysisDomain, operationVisitor);
            return nullAnalysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override ValueContentAnalysisResult ToResult(ValueContentAnalysisContext analysisContext, ValueContentAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        protected override ValueContentBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, ValueContentAnalysisData blockAnalysisData)
            => new(basicBlock, blockAnalysisData);
    }
}
