// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ValueContentAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ValueContentAnalysisContext : AbstractDataFlowAnalysisContext<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentAbstractValue>
    {
        public ValueContentAnalysisContext(
            AbstractValueDomain<ValueContentAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ValueContentAnalysisContext, ValueContentAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            ValueContentAnalysisData currentAnalysisDataAtCalleeOpt = default(ValueContentAnalysisData),
            ImmutableArray<ValueContentAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<ValueContentAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, pessimisticAnalysis,
                  predicateAnalysis: true, copyAnalysisResultOpt: copyAnalysisResultOpt,
                  pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
        }

        public override int GetHashCode(int hashCode) => hashCode;
    }
}
