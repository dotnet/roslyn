// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using TaintedDataAnalysisResult = Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>;

    using Analyzer.Utilities.Extensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
    using Microsoft.CodeAnalysis.Operations;
    using System;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private static readonly TaintedDataAnalysisDomain TaintedDataAnalysisDomainInstance = new TaintedDataAnalysisDomain(CoreTaintedDataAnalysisDataDomain.Instance);

        private TaintedDataAnalysis(TaintedDataOperationVisitor operationVisitor)
            : base(TaintedDataAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> GetOrComputeResult(ControlFlowGraph cfg, Compilation compilation, ISymbol containingMethod)
        {
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            return GetOrComputeResult(cfg, containingMethod, wellKnownTypeProvider);
        }

        public static DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> GetOrComputeResult(
            ControlFlowGraph cfg,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider)
        {
            var pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider);
            var copyAnalysisResult = CopyAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, pointsToAnalysisResultOpt: pointsToAnalysisResult);
            // Do another analysis pass to improve the results from PointsTo and Copy analysis.
            pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(cfg, owningSymbol, wellKnownTypeProvider, copyAnalysisResult);

            TaintedDataAnalysisContext analysisContext = new TaintedDataAnalysisContext(
                TaintedDataAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                owningSymbol,
                true /* pessimisticAnalysis */,
                pointsToAnalysisResult,
                GetOrComputeResultForAnalysisContext);

            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static TaintedDataAnalysisResult GetOrComputeResultForAnalysisContext(TaintedDataAnalysisContext analysisContext)
        {
            TaintedDataOperationVisitor visitor = new TaintedDataOperationVisitor(analysisContext);
            TaintedDataAnalysis analysis = new TaintedDataAnalysis(visitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override TaintedDataAnalysisResult ToResult(TaintedDataAnalysisContext analysisContext, TaintedDataAnalysisResult analysisResult)
        {
            return analysisResult;
        }

        internal override TaintedDataBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
