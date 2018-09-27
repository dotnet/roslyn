// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    internal partial class BinaryFormatterAnalysis : ForwardDataFlowAnalysis<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult, BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue>
    {
        private static readonly BinaryFormatterAnalysisDomain BinaryFormatterAnalysisDomainInstance = new BinaryFormatterAnalysisDomain(CoreBinaryFormatterAnalysisDataDomain.Instance);

        private BinaryFormatterAnalysis(BinaryFormatterOperationVisitor operationVisitor)
            : base(BinaryFormatterAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static BinaryFormatterAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod)
        {
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            PointsToAnalysisResult pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(
                cfg,
                containingMethod,
                wellKnownTypeProvider,
                InterproceduralAnalysisKind.ContextSensitive,
                true,
                true);

            BinaryFormatterAnalysisContext analysisContext = BinaryFormatterAnalysisContext.Create(
                BinaryFormatterAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                containingMethod,
                InterproceduralAnalysisKind.ContextSensitive,
                false /* pessimisticAnalysis */,
                pointsToAnalysisResult,
                GetOrComputeResultForAnalysisContext);

            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static BinaryFormatterAnalysisResult GetOrComputeResultForAnalysisContext(BinaryFormatterAnalysisContext analysisContext)
        {
            BinaryFormatterOperationVisitor visitor = new BinaryFormatterOperationVisitor(analysisContext);
            BinaryFormatterAnalysis analysis = new BinaryFormatterAnalysis(visitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override BinaryFormatterAnalysisResult ToResult(
            BinaryFormatterAnalysisContext analysisContext,
            DataFlowAnalysisResult<BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue> dataFlowAnalysisResult)
        {
            analysisContext = analysisContext.WithTrackHazardousUsages();
            BinaryFormatterOperationVisitor newOperationVisitor = new BinaryFormatterOperationVisitor(analysisContext);
            foreach (var block in analysisContext.ControlFlowGraph.Blocks)
            {
                var data = BinaryFormatterAnalysisDomainInstance.Clone(dataFlowAnalysisResult[block].InputData);
                data = Flow(newOperationVisitor, block, data);
            }

            return new BinaryFormatterAnalysisResult(dataFlowAnalysisResult, newOperationVisitor.GetHazardousUsages());
        }


        internal override BinaryFormatterBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<BinaryFormatterAnalysisData> blockAnalysisData)
        {
            return new BinaryFormatterBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
