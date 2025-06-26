// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal partial class TaintedDataAnalysis : ForwardDataFlowAnalysis<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>
    {
        internal static readonly AbstractValueDomain<TaintedDataAbstractValue> ValueDomainInstance = TaintedDataAbstractValueDomain.Default;

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
                ValueDomainInstance,
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
