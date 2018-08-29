// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="PointsToAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class PointsToAnalysisContext : AbstractDataFlowAnalysisContext<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToAbstractValue>
    {
        public PointsToAnalysisContext(
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            Func<PointsToAnalysisContext, PointsToAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            PointsToAnalysisData currentAnalysisDataAtCalleeOpt = default(PointsToAnalysisData),
            ImmutableArray<PointsToAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<PointsToAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, pessimisticAnalysis,
                  predicateAnalysis: true, copyAnalysisResultOpt: copyAnalysisResultOpt, pointsToAnalysisResultOpt: null,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
        }

        public override int GetHashCode(int hashCode) => hashCode;
    }
}
