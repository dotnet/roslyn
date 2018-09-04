// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;
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
            bool pessimisticAnalysis = true)
        {
            Debug.Assert(topmostBlock != null);

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var cfg = topmostBlock.GetEnclosingControlFlowGraph();
            var pointsToAnalysisResult = ComputePointsToAnalysisResultForParameterValidationAnalysis(cfg, owningSymbol, wellKnownTypeProvider);
            var analysisContext = new ParameterValidationAnalysisContext(ParameterValidationAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, pessimisticAnalysis, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext);
            var result = GetOrComputeResultForAnalysisContext(analysisContext);
            return result.HazardousParameterUsages;
        }

        public static PointsToAnalysisResult ComputePointsToAnalysisResultForParameterValidationAnalysis(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider)
        {
            var pointsToAnalysisResult = PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider);
            var copyAnalysisResult = CopyAnalysis.CopyAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, pointsToAnalysisResultOpt: pointsToAnalysisResult);
            // Do another analysis pass to improve the results from PointsTo and Copy analysis.
            return PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, copyAnalysisResult);
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
