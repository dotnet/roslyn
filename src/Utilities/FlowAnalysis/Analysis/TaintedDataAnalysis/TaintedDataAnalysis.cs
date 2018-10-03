// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private static readonly TaintedDataAnalysisDomain TaintedDataAnalysisDomainInstance = new TaintedDataAnalysisDomain(CoreTaintedDataAnalysisDataDomain.Instance);

        private TaintedDataAnalysis(TaintedDataOperationVisitor operationVisitor)
            : base(TaintedDataAnalysisDomainInstance, operationVisitor)
        {
        }

        internal static TaintedDataAnalysisResult GetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod,
            ImmutableDictionary<string, SourceInfo> taintedSourceInfos,
            ImmutableDictionary<string, SanitizerInfo> taintedSanitizerInfos,
            ImmutableDictionary<string, SinkInfo> taintedConcreteSinkInfos,
            ImmutableDictionary<string, SinkInfo> taintedInterfaceSinkInfos)
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
                false /* pessimisticAnalysis */,
                pointsToAnalysisResult,
                GetOrComputeResultForAnalysisContext,
                taintedSourceInfos,
                taintedSanitizerInfos,
                taintedConcreteSinkInfos,
                taintedInterfaceSinkInfos);

            return GetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static TaintedDataAnalysisResult GetOrComputeResultForAnalysisContext(TaintedDataAnalysisContext analysisContext)
        {
            TaintedDataOperationVisitor visitor = new TaintedDataOperationVisitor(analysisContext);
            TaintedDataAnalysis analysis = new TaintedDataAnalysis(visitor);
            return analysis.GetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        internal override TaintedDataAnalysisResult ToResult(
            TaintedDataAnalysisContext analysisContext,
            DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> dataFlowAnalysisResult)
        {
            // Hey Manish, is it fine to look at this.OperationVisitor here to look at its accumulated results?
            TaintedDataOperationVisitor visitor = (TaintedDataOperationVisitor) this.OperationVisitor;
            return new TaintedDataAnalysisResult(dataFlowAnalysisResult, visitor.GetTaintedDataSourceSinkEntries());
        }

        internal override TaintedDataBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, DataFlowAnalysisInfo<TaintedDataAnalysisData> blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
