// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisDomain = MapAbstractDomain<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track set of global <see cref="IAbstractAnalysisValue"/>s enabled on each control flow path at each <see cref="IOperation"/> in the <see cref="ControlFlowGraph"/>.
    /// </summary>
    internal sealed partial class GlobalFlowStateAnalysis : ForwardDataFlowAnalysis<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        internal static readonly GlobalFlowStateAnalysisDomain GlobalFlowStateAnalysisDomainInstance = new(GlobalFlowStateAnalysisValueSetDomain.Instance);

        private GlobalFlowStateAnalysis(GlobalFlowStateAnalysisDomain analysisDomain, GlobalFlowStateValueSetFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        /// <summary>
        /// Performs global flow state analysis and returns the analysis result.
        /// </summary>
        /// <param name="cfg">Control flow graph to analyze.</param>
        /// <param name="owningSymbol">Owning symbol for the analyzed <paramref name="cfg"/>.</param>
        /// <param name="createOperationVisitor">Delegate to create a <see cref="GlobalFlowStateValueSetFlowOperationVisitor"/> that performs the core analysis.</param>
        /// <param name="wellKnownTypeProvider">Well-known type provider for the compilation.</param>
        /// <param name="analyzerOptions">Analyzer options for analysis</param>
        /// <param name="rule"><see cref="DiagnosticDescriptor"/> for fetching any rule specific analyzer option values from <paramref name="analyzerOptions"/>.</param>
        /// <param name="performValueContentAnalysis">Flag to indicate if <see cref="ValueContentAnalysis.ValueContentAnalysis"/> should be performed.</param>
        /// <param name="pessimisticAnalysis">
        /// This boolean field determines if we should perform an optimistic OR a pessimistic analysis.
        /// For example, invoking a lambda method for which we do not know the target method being invoked can change/invalidate the current global flow state.
        /// An optimistic points to analysis assumes that the global flow state doesn't change for such scenarios.
        /// A pessimistic points to analysis resets the global flow state to an unknown state for such scenarios.
        /// </param>
        /// <param name="valueContentAnalysisResult">Optional value content analysis result, if <paramref name="performValueContentAnalysis"/> is true</param>
        /// <param name="interproceduralAnalysisKind"><see cref="InterproceduralAnalysisKind"/> for the analysis.</param>
        /// <param name="interproceduralAnalysisPredicate">Optional predicate for interprocedural analysis.</param>
        /// <param name="additionalSupportedValueTypes">Additional value types for which the caller wants to track stored values during value content analysis.</param>
        /// <param name="getValueContentValueForAdditionalSupportedValueTypeOperation">
        /// Optional delegate to compute values for <paramref name="additionalSupportedValueTypes"/>.
        /// Must be non-null if <paramref name="additionalSupportedValueTypes"/> is non-empty.
        /// </param>
        ///
        /// <returns>Global flow state analysis result, or null if analysis did not succeed.</returns>
        public static GlobalFlowStateAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateValueSetFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            bool performValueContentAnalysis,
            bool pessimisticAnalysis,
            out ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate = null,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes = default,
            Func<IOperation, ValueContentAbstractValue>? getValueContentValueForAdditionalSupportedValueTypeOperation = null)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, wellKnownTypeProvider.Compilation, interproceduralAnalysisKind);
            var pointsToAnalysisKind = analyzerOptions.GetPointsToAnalysisKindOption(rule, owningSymbol, wellKnownTypeProvider.Compilation,
                defaultValue: PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties);
            return TryGetOrComputeResult(cfg, owningSymbol, createOperationVisitor, wellKnownTypeProvider, analyzerOptions,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicate, pointsToAnalysisKind, pessimisticAnalysis,
                performValueContentAnalysis, out valueContentAnalysisResult,
                additionalSupportedValueTypes, getValueContentValueForAdditionalSupportedValueTypeOperation);
        }

        private static GlobalFlowStateAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateValueSetFlowOperationVisitor> createOperationVisitor,
            WellKnownTypeProvider wellKnownTypeProvider,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate,
            PointsToAnalysisKind pointsToAnalysisKind,
            bool pessimisticAnalysis,
            bool performValueContentAnalysis,
            out ValueContentAnalysisResult? valueContentAnalysisResult,
            ImmutableArray<INamedTypeSymbol> additionalSupportedValueTypes = default,
            Func<IOperation, ValueContentAbstractValue>? getValueContentValueForAdditionalSupportedValueTypeOperation = null)
        {
            RoslynDebug.Assert(owningSymbol != null);

            PointsToAnalysisResult? pointsToAnalysisResult = null;
            valueContentAnalysisResult = performValueContentAnalysis ?
                ValueContentAnalysis.ValueContentAnalysis.TryGetOrComputeResult(
                    cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                    pointsToAnalysisKind, interproceduralAnalysisConfig, out _,
                    out pointsToAnalysisResult, pessimisticAnalysis,
                    performCopyAnalysis: false, interproceduralAnalysisPredicate,
                    additionalSupportedValueTypes, getValueContentValueForAdditionalSupportedValueTypeOperation) :
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
            Func<GlobalFlowStateAnalysisContext, GlobalFlowStateValueSetFlowOperationVisitor> createOperationVisitor)
        {
            var operationVisitor = createOperationVisitor(analysisContext);
            var analysis = new GlobalFlowStateAnalysis(GlobalFlowStateAnalysisDomainInstance, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        protected override GlobalFlowStateAnalysisResult ToResult(GlobalFlowStateAnalysisContext analysisContext, GlobalFlowStateAnalysisResult dataFlowAnalysisResult)
        {
            // Update the operation state map with global values map
            // These are the values that analyzers care about.
            var operationVisitor = (GlobalFlowStateValueSetFlowOperationVisitor)OperationVisitor;
            return dataFlowAnalysisResult.With(operationVisitor.GetGlobalValuesMap());
        }

        protected override GlobalFlowStateBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, GlobalFlowStateAnalysisData blockAnalysisData)
            => new(basicBlock, blockAnalysisData);
    }
}
