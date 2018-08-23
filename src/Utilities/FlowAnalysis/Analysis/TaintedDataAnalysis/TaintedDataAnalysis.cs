// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Analyzer.Utilities.Extensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private static readonly TaintedDataAnalysisDomain TaintedDataAnalysisDomainInstance = new TaintedDataAnalysisDomain(CoreTaintedDataAnalysisDataDomain.Instance);

        private TaintedDataAnalysis(TaintedDataOperationVisitor operationVisitor)
            : base(TaintedDataAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static TaintedDataCfgAnalysisResult GetOrComputeResult(IBlockOperation topmostBlock, Compilation compilation, IMethodSymbol containingMethod)
        {
            ControlFlowGraph cfg = topmostBlock.GetEnclosingControlFlowGraph();
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            return GetOrComputeResult(cfg, containingMethod, wellKnownTypeProvider);
        }


        public static TaintedDataCfgAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider)
        {
            var pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider);
            var copyAnalysisResult = CopyAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, pointsToAnalysisResultOpt: pointsToAnalysisResult);
            // Do another analysis pass to improve the results from PointsTo and Copy analysis.
            pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, copyAnalysisResult);

            TaintedDataOperationVisitor visitor = new TaintedDataOperationVisitor(
                TaintedDataAbstractValueDomain.Default,
                owningSymbol,
                wellKnownTypeProvider,
                cfg,
                pointsToAnalysisResult,
                true);
            TaintedDataAnalysis analysis = new TaintedDataAnalysis(visitor);
            DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> analysisResult =
                analysis.GetOrComputeResultCore(cfg, cacheResult: true);

            return null;
        }

        internal override TaintedDataBlockAnalysisResult ToResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
