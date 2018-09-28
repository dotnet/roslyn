// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using BinaryFormatterAnalysisData = IDictionary<AbstractLocation, BinaryFormatterAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, BinaryFormatterAbstractValue>, BinaryFormatterAnalysisContext, BinaryFormatterAbstractValue>;
    using BinaryFormatterAnalysisDomain = MapAbstractDomain<AbstractLocation, BinaryFormatterAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Dataflow analysis to track <see cref="BinaryFormatterAbstractValue"/> of <see cref="AbstractLocation"/>/<see cref="IOperation"/> instances.
    /// </summary>
    internal partial class BinaryFormatterAnalysis : ForwardDataFlowAnalysis<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult, BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue>
    {
        public static readonly BinaryFormatterAnalysisDomain BinaryFormatterAnalysisDomainInstance = new BinaryFormatterAnalysisDomain(BinaryFormatterAbstractValueDomain.Default);

        private BinaryFormatterAnalysis(BinaryFormatterAnalysisDomain analysisDomain, BinaryFormatterDataFlowOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        public static ImmutableDictionary<IOperation, BinaryFormatterAbstractValue> GetOrComputeHazardousParameterUsages(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.ContextSensitive,
            bool pessimisticAnalysis = false)
        {
            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(
                cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis);
            var result = GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResult);
            return result.HazardousUsages;
        }

        public static BinaryFormatterAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult)
        {
            Debug.Assert(pointsToAnalysisResult != null);

            var analysisContext = BinaryFormatterAnalysisContext.Create(BinaryFormatterAbstractValueDomain.Default,
                wellKnownTypeProvider, cfg, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis, pointsToAnalysisResult, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static BinaryFormatterAnalysisResult GetOrComputeResultForAnalysisContext(BinaryFormatterAnalysisContext analysisContext)
        {
            var operationVisitor = new BinaryFormatterDataFlowOperationVisitor(analysisContext);
            var analysis = new BinaryFormatterAnalysis(BinaryFormatterAnalysisDomainInstance, operationVisitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override BinaryFormatterAnalysisResult ToResult(
            BinaryFormatterAnalysisContext analysisContext,
            DataFlowAnalysisResult<BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue> dataFlowAnalysisResult)
        {
            analysisContext = analysisContext.WithTrackHazardousParameterUsages();
            var newOperationVisitor = new BinaryFormatterDataFlowOperationVisitor(analysisContext);
            var resultBuilder = new DataFlowAnalysisResultBuilder<BinaryFormatterAnalysisData>();
            foreach (var block in analysisContext.ControlFlowGraph.Blocks)
            {
                var data = BinaryFormatterAnalysisDomainInstance.Clone(dataFlowAnalysisResult[block].InputData);
                data = Flow(newOperationVisitor, block, data);
            }

            return new BinaryFormatterAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.HazardousUsages);
        }

        internal override BinaryFormatterBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<BinaryFormatterAnalysisData> blockAnalysisData) => new BinaryFormatterBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
