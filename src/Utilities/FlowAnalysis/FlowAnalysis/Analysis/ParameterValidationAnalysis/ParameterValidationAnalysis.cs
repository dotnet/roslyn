// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
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
        public static readonly ParameterValidationAnalysisDomain ParameterValidationAnalysisDomainInstance = new ParameterValidationAnalysisDomain(ParameterValidationAbstractValueDomain.Default);

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
            CancellationToken cancellationToken,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            uint defaultMaxInterproceduralMethodCallChain = 1, // By default, we only want to track method calls one level down.
            bool pessimisticAnalysis = true)
        {
            Debug.Assert(!owningSymbol.IsConfiguredToSkipAnalysis(analyzerOptions, rule, compilation, cancellationToken));

            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                   analyzerOptions, rule, interproceduralAnalysisKind, cancellationToken, defaultMaxInterproceduralMethodCallChain);
            var performCopyAnalysis = analyzerOptions.GetCopyAnalysisOption(rule, defaultValue: false, cancellationToken);
            var nullCheckValidationMethods = analyzerOptions.GetNullCheckValidationMethodsOption(rule, compilation, cancellationToken);
            return GetOrComputeHazardousParameterUsages(topmostBlock, compilation, owningSymbol, analyzerOptions,
                nullCheckValidationMethods, interproceduralAnalysisConfig, performCopyAnalysis, pessimisticAnalysis);
        }

        private static ImmutableDictionary<IParameterSymbol, SyntaxNode> GetOrComputeHazardousParameterUsages(
            IBlockOperation topmostBlock,
            Compilation compilation,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            SymbolNamesOption nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool performCopyAnalysis,
            bool pessimisticAnalysis = true)
        {
            Debug.Assert(topmostBlock != null);

            var cfg = topmostBlock.GetEnclosingControlFlowGraph();
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.TryGetOrComputeResult(cfg, owningSymbol, analyzerOptions, wellKnownTypeProvider,
                interproceduralAnalysisConfig, interproceduralAnalysisPredicateOpt: null, pessimisticAnalysis, performCopyAnalysis);
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

        private static ParameterValidationAnalysisResult TryGetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            WellKnownTypeProvider wellKnownTypeProvider,
            SymbolNamesOption nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult)
        {
            Debug.Assert(pointsToAnalysisResult != null);

            var analysisContext = ParameterValidationAnalysisContext.Create(ParameterValidationAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, analyzerOptions, nullCheckValidationMethods, interproceduralAnalysisConfig,
                pessimisticAnalysis, pointsToAnalysisResult, TryGetOrComputeResultForAnalysisContext);
            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ParameterValidationAnalysisResult TryGetOrComputeResultForAnalysisContext(ParameterValidationAnalysisContext analysisContext)
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
                    _ = FlowBranch(newOperationVisitor, block.FallThroughSuccessor, data);
                }
            }

            return new ParameterValidationAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.HazardousParameterUsages);
        }

        protected override ParameterValidationBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, ParameterValidationAnalysisData blockAnalysisData) => new ParameterValidationBlockAnalysisResult(basicBlock, blockAnalysisData);

    }
}
