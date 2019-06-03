// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using InterproceduralTaintedDataAnalysisData = InterproceduralAnalysisData<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAbstractValue>;

    internal sealed class TaintedDataAnalysisContext : AbstractDataFlowAnalysisContext<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
    {
        private TaintedDataAnalysisContext(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult,
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
                  interproceduralAnalysisConfig,
                  pessimisticAnalysis,
                  predicateAnalysis: false,
                  exceptionPathsAnalysis: false,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResult,
                  valueContentAnalysisResultOpt: null,
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
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResult,
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
                interproceduralAnalysisConfig,
                pessimisticAnalysis,
                pointsToAnalysisResult,
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
            Debug.Assert(copyAnalysisResultOpt == null);   // Just because we're not passing this argument along.
            Debug.Assert(valueContentAnalysisResultOpt == null);

            return new TaintedDataAnalysisContext(
                this.ValueDomain,
                this.WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                this.InterproceduralAnalysisConfiguration,
                this.PessimisticAnalysis,
                pointsToAnalysisResultOpt,
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

        protected override void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder)
        {
            builder.Add(SourceInfos.GetHashCode());
            builder.Add(SanitizerInfos.GetHashCode());
            builder.Add(SinkInfos.GetHashCode());
        }
    }
}
