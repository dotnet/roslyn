// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;
    using InterproceduralParameterValidationAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, ParameterValidationAbstractValue>, ParameterValidationAnalysisContext, ParameterValidationAbstractValue>;
    using ParameterValidationAnalysisDomain = MapAbstractDomain<AbstractLocation, ParameterValidationAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue>;

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
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            bool pessimisticAnalysis = true)
        {
            Debug.Assert(topmostBlock != null);

            var cfg = topmostBlock.GetEnclosingControlFlowGraph();
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(
                cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis);
            var result = GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResult);
            return result.HazardousParameterUsages;
        }

        public static ParameterValidationAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult)
        {
            Debug.Assert(pointsToAnalysisResult != null);

            var analysisContext = ParameterValidationAnalysisContext.Create(ParameterValidationAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ParameterValidationAnalysisResult GetOrComputeResultForAnalysisContext(ParameterValidationAnalysisContext analysisContext)
        {
            var operationVisitor = new ParameterValidationDataFlowOperationVisitor(analysisContext);
            var analysis = new ParameterValidationAnalysis(ParameterValidationAnalysisDomainInstance, operationVisitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override ParameterValidationAnalysisResult ToResult(
            ParameterValidationAnalysisContext analysisContext,
            DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue> dataFlowAnalysisResult)
        {
            analysisContext = analysisContext.WithTrackHazardousParameterUsages();
            var newOperationVisitor = new ParameterValidationDataFlowOperationVisitor(analysisContext);
            var resultBuilder = new DataFlowAnalysisResultBuilder<ParameterValidationAnalysisData>();
            foreach (var block in analysisContext.ControlFlowGraph.Blocks)
            {
                var data = ParameterValidationAnalysisDomainInstance.Clone(dataFlowAnalysisResult[block].InputData);
                data = Flow(newOperationVisitor, block, data);
            }

            return new ParameterValidationAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.HazardousParameterUsages);
        }

        internal override ParameterValidationBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<ParameterValidationAnalysisData> blockAnalysisData) => new ParameterValidationBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
