// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;
    using TaintedDataAnalysisResult = DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private static readonly TaintedDataAnalysisDomain TaintedDataAnalysisDomainInstance = new TaintedDataAnalysisDomain(CoreTaintedDataAnalysisDataDomain.Instance);

        private TaintedDataAnalysis(TaintedDataOperationVisitor operationVisitor)
            : base(TaintedDataAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> GetOrComputeResult(
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

            TaintedDataAnalysisContext analysisContext = TaintedDataAnalysisContext.Create(
                TaintedDataAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                containingMethod,
                InterproceduralAnalysisKind.ContextSensitive,
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
