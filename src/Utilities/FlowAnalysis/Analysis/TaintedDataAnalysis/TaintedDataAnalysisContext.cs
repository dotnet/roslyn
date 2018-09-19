// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            InterproceduralTaintedDataAnalysisData interproceduralAnalysisDataOpt)
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
        }

        public static TaintedDataAnalysisContext Create(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> getOrComputeAnalysisResult)
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
                parentControlFlowGraph: null,
                interproceduralAnalysisDataOpt: null);
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
                interproceduralAnalysisData);
        }

        protected override int GetHashCode(int hashCode)
        {
            return hashCode;
        }
    }
}
