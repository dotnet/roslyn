// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

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
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> getOrComputeAnalysisResult,
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
                  interproceduralAnalysisKind,
                  pessimisticAnalysis,
                  predicateAnalysis: false, 
                  copyAnalysisResultOpt: null, 
                  pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraph,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
            this.SourceInfos = taintedSourceInfos ?? throw new ArgumentNullException(nameof(taintedSourceInfos));
            this.SanitizerInfos = taintedSanitizerInfos ?? throw new ArgumentNullException(nameof(taintedSanitizerInfos));
            this.SinkInfos = taintedSinkInfos ?? throw new ArgumentNullException(nameof(taintedSinkInfos));
        }

        public static TaintedDataAnalysisContext Create(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> getOrComputeAnalysisResult,
            TaintedDataSymbolMap<SourceInfo> taintedSourceInfos,
            TaintedDataSymbolMap<SanitizerInfo> taintedSanitizerInfos,
            TaintedDataSymbolMap<SinkInfo> taintedSinkInfos)
        {
            return new TaintedDataAnalysisContext(
                valueDomain,
                wellKnownTypeProvider,
                controlFlowGraph,
                owningSymbol,
                interproceduralAnalysisKind,
                pessimisticAnalysis,
                pointsToAnalysisResultOpt,
                getOrComputeAnalysisResult,
                null,
                null,
                taintedSourceInfos,
                taintedSanitizerInfos,
                taintedSinkInfos);
        }

        public override TaintedDataAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg, 
            IOperation operation,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt, 
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
            InterproceduralTaintedDataAnalysisData interproceduralAnalysisData)
        {
            return new TaintedDataAnalysisContext(
                this.ValueDomain,
                this.WellKnownTypeProvider,
                invokedCfg,
                invokedMethod,
                this.InterproceduralAnalysisKind,
                this.PessimisticAnalysis,
                pointsToAnalysisResultOpt,
                this.GetOrComputeAnalysisResult,
                this.ControlFlowGraph,
                interproceduralAnalysisData,
                this.SourceInfos,
                this.SanitizerInfos,
                this.SinkInfos);
        }

        public TaintedDataSymbolMap<SourceInfo> SourceInfos { get; }

        public TaintedDataSymbolMap<SanitizerInfo> SanitizerInfos { get; }

        public TaintedDataSymbolMap<SinkInfo> SinkInfos { get; }

        protected override int GetHashCode(int hashCode)
        {
            return HashUtilities.Combine(this.SourceInfos.GetHashCode(),
                HashUtilities.Combine(this.SanitizerInfos.GetHashCode(),
                HashUtilities.Combine(this.SinkInfos.GetHashCode(),
                hashCode)));
        }
    }
}
