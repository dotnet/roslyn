// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using System.Diagnostics;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        private TaintedDataAnalysis(TaintedDataAnalysisDomain analysisDomain, TaintedDataOperationVisitor operationVisitor)
            : base(analysisDomain, operationVisitor)
        {
        }

        internal static TaintedDataAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod,
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos)
        {
            var interproceduralAnalysisConfig = InterproceduralAnalysisConfiguration.Create(
                analyzerOptions, rule, cfg, compilation, InterproceduralAnalysisKind.ContextSensitive);
            return TryGetOrComputeResult(cfg, compilation, containingMethod, analyzerOptions, taintedSourceInfos,
                taintedSanitizerInfos, taintedSinkInfos, interproceduralAnalysisConfig);
        }

        private static TaintedDataAnalysisResult? TryGetOrComputeResult(
            ControlFlowGraph cfg,
            Compilation compilation,
            ISymbol containingMethod,
            AnalyzerOptions analyzerOptions,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig)
        {
            if (cfg == null)
            {
                Debug.Fail("Expected non-null CFG");
                return null;
            }

            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            ValueContentAnalysisResult? valueContentAnalysisResult = null;
            CopyAnalysisResult? copyAnalysisResult = null;
            PointsToAnalysisResult? pointsToAnalysisResult = null;
            if (taintedSourceInfos.RequiresValueContentAnalysis || taintedSanitizerInfos.RequiresValueContentAnalysis || taintedSinkInfos.RequiresValueContentAnalysis)
            {
                valueContentAnalysisResult = ValueContentAnalysis.TryGetOrComputeResult(
                    cfg,
                    containingMethod,
                    analyzerOptions,
                    wellKnownTypeProvider,
                    PointsToAnalysisKind.Complete,
                    interproceduralAnalysisConfig,
                    out copyAnalysisResult,
                    out pointsToAnalysisResult,
                    pessimisticAnalysis: true,
                    performCopyAnalysis: false);
                if (valueContentAnalysisResult == null)
                {
                    return null;
                }
            }
            else
            {
                pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                    cfg,
                    containingMethod,
                    analyzerOptions,
                    wellKnownTypeProvider,
                    PointsToAnalysisKind.Complete,
                    interproceduralAnalysisConfig,
                    interproceduralAnalysisPredicate: null,
                    pessimisticAnalysis: true,
                    performCopyAnalysis: false);
                if (pointsToAnalysisResult == null)
                {
                    return null;
                }
            }

            TaintedDataAnalysisContext analysisContext = TaintedDataAnalysisContext.Create(
                TaintedDataAbstractValueDomain.Default,
                wellKnownTypeProvider,
                cfg,
                containingMethod,
                analyzerOptions,
                interproceduralAnalysisConfig,
                pessimisticAnalysis: false,
                copyAnalysisResult: copyAnalysisResult,
                pointsToAnalysisResult: pointsToAnalysisResult,
                valueContentAnalysisResult: valueContentAnalysisResult,
                tryGetOrComputeAnalysisResult: TryGetOrComputeResultForAnalysisContext,
                taintedSourceInfos: taintedSourceInfos,
                taintedSanitizerInfos: taintedSanitizerInfos,
                taintedSinkInfos: taintedSinkInfos);

            return TryGetOrComputeResultForAnalysisContext(analysisContext);
        }

        private static TaintedDataAnalysisResult? TryGetOrComputeResultForAnalysisContext(TaintedDataAnalysisContext analysisContext)
        {
            TaintedDataAnalysisDomain analysisDomain = new TaintedDataAnalysisDomain(new CoreTaintedDataAnalysisDataDomain(analysisContext.PointsToAnalysisResult));
            TaintedDataOperationVisitor visitor = new TaintedDataOperationVisitor(analysisDomain, analysisContext);
            TaintedDataAnalysis analysis = new TaintedDataAnalysis(analysisDomain, visitor);
            return analysis.TryGetOrComputeResultCore(analysisContext, cacheResult: true);
        }

        protected override TaintedDataAnalysisResult ToResult(
            TaintedDataAnalysisContext analysisContext,
            DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue> dataFlowAnalysisResult)
        {
            TaintedDataOperationVisitor visitor = (TaintedDataOperationVisitor)this.OperationVisitor;
            return new TaintedDataAnalysisResult(dataFlowAnalysisResult, visitor.GetTaintedDataSourceSinkEntries());
        }

        protected override TaintedDataBlockAnalysisResult ToBlockResult(BasicBlock basicBlock, TaintedDataAnalysisData blockAnalysisData)
        {
            return new TaintedDataBlockAnalysisResult(basicBlock, blockAnalysisData);
        }
    }
}
