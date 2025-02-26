// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            CopyAnalysisResult? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralTaintedDataAnalysisData? interproceduralAnalysisData,
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
                  copyAnalysisResult,
                  pointsToAnalysisResult,
                  valueContentAnalysisResult,
                  tryGetOrComputeAnalysisResult,
                  parentControlFlowGraph,
                  interproceduralAnalysisData,
                  interproceduralAnalysisPredicate: null)
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
            CopyAnalysisResult? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult?> tryGetOrComputeAnalysisResult,
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
                copyAnalysisResult,
                pointsToAnalysisResult,
                valueContentAnalysisResult,
                tryGetOrComputeAnalysisResult,
                parentControlFlowGraph: null,
                interproceduralAnalysisData: null,
                taintedSourceInfos: taintedSourceInfos,
                taintedSanitizerInfos: taintedSanitizerInfos,
                taintedSinkInfos: taintedSinkInfos);
        }

        public override TaintedDataAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>? copyAnalysisResult,
            DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>? valueContentAnalysisResult,
            InterproceduralTaintedDataAnalysisData? interproceduralAnalysisData)
        {
            return new TaintedDataAnalysisContext(
                this.ValueDomain,
                this.WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                this.AnalyzerOptions,
                this.InterproceduralAnalysisConfiguration,
                this.PessimisticAnalysis,
                copyAnalysisResult,
                pointsToAnalysisResult,
                valueContentAnalysisResult,
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

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
            hashCode.Add(SourceInfos.GetHashCode());
            hashCode.Add(SanitizerInfos.GetHashCode());
            hashCode.Add(SinkInfos.GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue> obj)
        {
            var other = (TaintedDataAnalysisContext)obj;
            return SourceInfos.GetHashCode() == other.SourceInfos.GetHashCode()
                && SanitizerInfos.GetHashCode() == other.SanitizerInfos.GetHashCode()
                && SinkInfos.GetHashCode() == other.SinkInfos.GetHashCode();
        }
    }
}
