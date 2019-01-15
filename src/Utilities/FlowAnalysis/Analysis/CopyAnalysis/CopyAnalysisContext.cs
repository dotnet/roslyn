// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralCopyAnalysisData = InterproceduralAnalysisData<CopyAnalysisData, CopyAnalysisContext, CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="CopyAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class CopyAnalysisContext : AbstractDataFlowAnalysisContext<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue>
    {
        private CopyAnalysisContext(
            AbstractValueDomain<CopyAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<CopyAnalysisContext, CopyAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralCopyAnalysisData interproceduralAnalysisDataOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: true, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
        }

        public static CopyAnalysisContext Create(
            AbstractValueDomain<CopyAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<CopyAnalysisContext, CopyAnalysisResult> getOrComputeAnalysisResult)
        {
            return new CopyAnalysisContext(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                  interproceduralAnalysisConfig, pessimisticAnalysis, pointsToAnalysisResultOpt, getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: null, interproceduralAnalysisDataOpt: null);
        }

        public override CopyAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedControlFlowGraph,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralCopyAnalysisData interproceduralAnalysisData)
        {
            return new CopyAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedControlFlowGraph, invokedMethod, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ControlFlowGraph, interproceduralAnalysisData);
        }

        protected override void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder)
        {
        }
    }
}
