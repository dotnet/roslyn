// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using TaintedDataAnalysisResult = Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>;

    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

    internal sealed class TaintedDataAnalysisContext : AbstractDataFlowAnalysisContext<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
    {
        public TaintedDataAnalysisContext(
            AbstractValueDomain<TaintedDataAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<TaintedDataAnalysisContext, TaintedDataAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            TaintedDataAnalysisData currentAnalysisDataAtCalleeOpt = default(TaintedDataAnalysisData),
            ImmutableArray<TaintedDataAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<TaintedDataAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, pessimisticAnalysis,
                  predicateAnalysis: true, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
        }

        public override int GetHashCode(int hashCode)
        {
            return hashCode;
        }
    }
}
