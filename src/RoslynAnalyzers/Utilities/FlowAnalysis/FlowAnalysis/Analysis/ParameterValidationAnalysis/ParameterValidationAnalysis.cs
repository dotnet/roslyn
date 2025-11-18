// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;
    using ParameterValidationAnalysisDomain = MapAbstractDomain<AbstractLocation, ParameterValidationAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="ParameterValidationAbstractValue"/> of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class ParameterValidationAnalysis : ForwardDataFlowAnalysis<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue>
    {
        public static readonly ParameterValidationAnalysisDomain ParameterValidationAnalysisDomainInstance = new(ParameterValidationAbstractValueDomain.Default);

        private ParameterValidationAnalysis(ParameterValidationAnalysisDomain analysisDomain, ParameterValidationDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static ImmutableDictionary<IParameterSymbol, SyntaxNode> GetOrComputeHazardousParameterUsages(
            IBlockOperation topmostBlock,
            Compilation compilation,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            PointsToAnalysisKind defaultPointsToAnalysisKind = PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            uint defaultMaxInterproceduralMethodCallChain = 1,
            bool pessimisticAnalysis = false)
        {
            Debug.Assert(!analyzerOptions.IsConfiguredToSkipAnalysis(rule, owningSymbol, compilation));

            var cfg = topmostBlock.GetEnclosingControlFlowGraph();
            if (cfg == null)
            {
                return ImmutableDictionary<IParameterSymbol, SyntaxNode>.Empty;
            }

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                   analyzerOptions, rule, cfg, compilation, interproceduralAnalysisKind, defaultMaxInterproceduralMethodCallChain);
            var performCopyAnalysis = analyzerOptions.GetCopyAnalysisOption(rule, topmostBlock.Syntax.SyntaxTree, compilation, defaultValue: false);
            var nullCheckValidationMethods = analyzerOptions.GetNullCheckValidationMethodsOption(rule, topmostBlock.Syntax.SyntaxTree, compilation);
            var pointsToAnalysisKind = analyzerOptions.GetPointsToAnalysisKindOption(rule, topmostBlock.Syntax.SyntaxTree, compilation, defaultPointsToAnalysisKind);
            return GetOrComputeHazardousParameterUsages(cfg, compilation, owningSymbol, analyzerOptions,
                nullCheckValidationMethods, pointsToAnalysisKind, interproceduralAnalysisConfig, performCopyAnalysis, pessimisticAnalysis);
        }

        private static ImmutableDictionary<IParameterSymbol, SyntaxNode> GetOrComputeHazardousParameterUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            SymbolNamesWithValueOption<Unit> nullCheckValidationMethods,
            PointsToAnalysisKind pointsToAnalysisKind,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool performCopyAnalysis,
            bool pessimisticAnalysis)
        {
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                pointsToAnalysisKind, interproceduralAnalysisConfig, interproceduralAnalysisPredicate: null, pessimisticAnalysis, performCopyAnalysis);
            if (pointsToAnalysisResult != null)
            {
                var result = TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                    nullCheckValidationMethods, interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResult);
                if (result != null)
                {
                    return result.HazardousParameterUsages;
                }
            }

            return ImmutableDictionary<IParameterSymbol, SyntaxNode>.Empty;
        }

        private static ParameterValidationAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            SymbolNamesWithValueOption<Unit> nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult)
        {
            var analysisContext = ParameterValidationAnalysisContext.Create(ParameterValidationAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions, nullCheckValidationMethods, interproceduralAnalysisConfig,
                pessimisticAnalysis, pointsToAnalysisResult, TryGetOrComputeResultForAnalysisContext);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ParameterValidationAnalysisResult? TryGetOrComputeResultForAnalysisContext(ParameterValidationAnalysisContext analysisContext)
        {
            var operationVisitor = new ParameterValidationDataFlowOperationVisitor(analysisContext);
            var analysis = new ParameterValidationAnalysis(ParameterValidationAnalysisDomainInstance, operationVisitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override ParameterValidationAnalysisResult ToResult(
            ParameterValidationAnalysisContext analysisContext,
            DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue> dataFlowAnalysisResult)
        {
            analysisContext = analysisContext.WithTrackHazardousParameterUsages();
            var newOperationVisitor = new ParameterValidationDataFlowOperationVisitor(analysisContext);

            foreach (var block in analysisContext.ControlFlowGraph.Blocks)
            {
                var data = new ParameterValidationAnalysisData(dataFlowAnalysisResult[block].Data);
                data = Flow(newOperationVisitor, block, data);

                if (block.FallThroughSuccessor != null)
                {
                    var fallThroughData = block.ConditionalSuccessor != null ? AnalysisDomain.Clone(data) : data;
                    _ = FlowBranch(newOperationVisitor, block.FallThroughSuccessor, fallThroughData);
                }

                if (block.ConditionalSuccessor != null)
                {
                    _ = FlowBranch(newOperationVisitor, block.ConditionalSuccessor, data);
                }
            }

            return new ParameterValidationAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.HazardousParameterUsages);
        }

        protected override ParameterValidationBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, ParameterValidationAnalysisData blockAnalysisData) => new(basicBlock, blockAnalysisData);

    }
}
