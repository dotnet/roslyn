// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    using InterproceduralTaintedDataAnalysisData = InterproceduralAnalysisData<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal sealed class TaintedDataAnalysisContext : AbstractDataFlowAnalysisContext<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
    {
        private TaintedDataAnalysisContext(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResult,
            ValueContentAnalysisResult valueContentAnalysisResult,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> tryGetOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraph,
            InterproceduralTaintedDataAnalysisData interproceduralAnalysisDataOpt,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos)
            : base(
                  valueDomain,
                  wellKnownTypeProvider,
                  controlFlowGraph,
                  owningSymbol,
                  analyzerOptions,
                  interproceduralAnalysisConfig,
                  pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis: false,
                  copyAnalysisResultOpt,
                  pointsToAnalysisResult,
                  valueContentAnalysisResult,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph,
                  interproceduralAnalysisDataOpt,
                  interproceduralAnalysisPredicateOpt: null)
        {
            Debug.Assert(pointsToAnalysisResult != null);

            this.SourceInfos = taintedSourceInfos ?? throw new ArgumentNullException(nameof(taintedSourceInfos));
            this.SanitizerInfos = taintedSanitizerInfos ?? throw new ArgumentNullException(nameof(taintedSanitizerInfos));
            this.SinkInfos = taintedSinkInfos ?? throw new ArgumentNullException(nameof(taintedSinkInfos));
        }

        public static TaintedDataAnalysisContext Create(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResult,
            ValueContentAnalysisResult valueContentAnalysisResult,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> tryGetOrComputeAnalysisResult,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos)
        {
            Debug.Assert(pointsToAnalysisResult != null);

            return new TaintedDataAnalysisContext(
                valueDomain,
                wellKnownTypeProvider,
                controlFlowGraph,
                owningSymbol,
                analyzerOptions,
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                copyAnalysisResultOpt,
                pointsToAnalysisResult,
                valueContentAnalysisResult,
                tryGetOrComputeAnalysisResult,
                parentControlFlowGraph: null,
                interproceduralAnalysisDataOpt: null,
                taintedSourceInfos: taintedSourceInfos,
                taintedSanitizerInfos: taintedSanitizerInfos,
                taintedSinkInfos: taintedSinkInfos);
        }

        public override TaintedDataAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue> valueContentAnalysisResultOpt,
            InterproceduralTaintedDataAnalysisData interproceduralAnalysisData)
        {
            return new TaintedDataAnalysisContext(
                this.ValueDomain,
                this.WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                this.AnalyzerOptions,
                this.InterproceduralAnalysisConfiguration,
                this.PessimisticAnalysis,
                copyAnalysisResultOpt,
                pointsToAnalysisResultOpt,
                valueContentAnalysisResultOpt,
                this.TryGetOrComputeAnalysisResult,
                this.ControlFlowGraph,
                interproceduralAnalysisData,
                this.SourceInfos,
                this.SanitizerInfos,
                this.SinkInfos);
        }

        /// <summary>
        /// Information about types for tainted data sources.
        /// </summary>
        public TaintedDataSymbolMap<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Information about types for tainted data sanitizers.
        /// </summary>
        public TaintedDataSymbolMap<SanitizerInfo> SanitizerInfos { get; }

        /// <summary>
        /// Information about types for the tainted data sinks.
        /// </summary>
        public TaintedDataSymbolMap<SinkInfo> SinkInfos { get; }

        protected override void ComputeHashCodePartsSpecific(Action<int> addPart)
        {
            addPart(SourceInfos.GetHashCode());
            addPart(SanitizerInfos.GetHashCode());
            addPart(SinkInfos.GetHashCode());
        }
    }
}
