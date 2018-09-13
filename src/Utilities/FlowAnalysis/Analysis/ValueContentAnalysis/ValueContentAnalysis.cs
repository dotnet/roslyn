// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using InterproceduralValueContentAnalysisData = InterproceduralAnalysisData<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyAnalysis.CopyBlockAnalysisResult, CopyAnalysis.CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue>;
    
    /// <summary>
    /// Dataflow analysis to track value content of <see cref="AnalysisEntity"/>/<see cref="IOperation"/>.
    /// </summary>
    internal partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        private ValueContentAnalysis(ValueContentDataFlowOperationVisitor operationVisitor)
            : base(ValueContentAnalysisDomain.Instance, operationVisitor)
        {
        }

        public static ValueContentAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAndCopyAnalysis = true)
        {
            return GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, out var _, out var _,
                interproceduralAnalysisKind, pessimisticAnalysis, performPointsToAndCopyAnalysis);
        }

        public static ValueContentAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            out CopyAnalysisResult copyAnalysisResultOpt,
            out PointsToAnalysisResult pointsToAnalysisResultOpt,
            InterproceduralAnalysisKind interproceduralAnalysisKind = InterproceduralAnalysisKind.None,
            bool pessimisticAnalysis = true,
            bool performPointsToAndCopyAnalysis = true)
        {
            copyAnalysisResultOpt = null;
            pointsToAnalysisResultOpt = performPointsToAndCopyAnalysis ?
                PointsToAnalysis.PointsToAnalysis.GetOrComputeResult(
                    cfg, owningSymbol, wellKnownTypeProvider, out copyAnalysisResultOpt, interproceduralAnalysisKind, pessimisticAnalysis, performPointsToAndCopyAnalysis) :
                null;
            var analysisContext = ValueContentAnalysisContext.Create(
                ValueContentAbstractValueDomain.Default, wellKnownTypeProvider, cfg, owningSymbol,
                interproceduralAnalysisKind, pessimisticAnalysis, copyAnalysisResultOpt, pointsToAnalysisResultOpt, GetOrComputeResultForAnalysisContext);
            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static ValueContentAnalysisResult GetOrComputeResultForAnalysisContext(ValueContentAnalysisContext analysisContext)
        {
            var operationVisitor = new ValueContentDataFlowOperationVisitor(analysisContext);
            var nullAnalysis = new ValueContentAnalysis(operationVisitor);
            return nullAnalysis.GetOrComputeResultCore(analysisContext, cacheResult: false);
        }

        internal override ValueContentAnalysisResult ToResult(ValueContentAnalysisContext analysisContext, ValueContentAnalysisResult dataFlowAnalysisResult)
            => dataFlowAnalysisResult;

        internal override ValueContentBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<ValueContentAnalysisData> blockAnalysisData)
            => new ValueContentBlockAnalysisResult(basicBlock, blockAnalysisData);
    }
}
