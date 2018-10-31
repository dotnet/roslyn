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
            ImmutableDictionary<ITypeSymbol, SanitizerInfo> taintedSanitizerInfos,
            ImmutableDictionary<ITypeSymbol, SinkInfo> taintedConcreteSinkInfos,
            ImmutableDictionary<ITypeSymbol, SinkInfo> taintedInterfaceSinkInfos)
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
            this.TaintedSourceInfos = taintedSourceInfos ?? throw new ArgumentNullException(nameof(taintedSourceInfos));
            this.TaintedSanitizerInfos = taintedSanitizerInfos ?? throw new ArgumentNullException(nameof(taintedSanitizerInfos));
            this.TaintedConcreteSinkInfos = taintedConcreteSinkInfos ?? throw new ArgumentNullException(nameof(taintedConcreteSinkInfos));
            this.TaintedInterfaceSinkInfos = taintedInterfaceSinkInfos ?? throw new ArgumentNullException(nameof(taintedInterfaceSinkInfos));
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
            ImmutableDictionary<ITypeSymbol, SanitizerInfo> taintedSanitizerInfos,
            ImmutableDictionary<ITypeSymbol, SinkInfo> taintedConcreteSinkInfos,
            ImmutableDictionary<ITypeSymbol, SinkInfo> taintedInterfaceSinkInfos)
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
                taintedConcreteSinkInfos,
                taintedInterfaceSinkInfos);
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
                this.TaintedSourceInfos,
                this.TaintedSanitizerInfos,
                this.TaintedConcreteSinkInfos,
                this.TaintedInterfaceSinkInfos);
        }

        public TaintedDataSymbolMap<SourceInfo> TaintedSourceInfos { get; }

        public ImmutableDictionary<ITypeSymbol, SanitizerInfo> TaintedSanitizerInfos { get; }

        public ImmutableDictionary<ITypeSymbol, SinkInfo> TaintedConcreteSinkInfos { get; }

        public ImmutableDictionary<ITypeSymbol, SinkInfo> TaintedInterfaceSinkInfos { get; }

        protected override int GetHashCode(int hashCode)
        {
            return HashUtilities.Combine(this.TaintedSourceInfos.GetHashCode(),
                HashUtilities.Combine(this.TaintedSanitizerInfos,
                HashUtilities.Combine(this.TaintedConcreteSinkInfos,
                HashUtilities.Combine(this.TaintedInterfaceSinkInfos,
                hashCode))));
        }
    }
}
