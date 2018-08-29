// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="CopyAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class CopyAnalysisContext : AbstractDataFlowAnalysisContext<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue>
    {
        public CopyAnalysisContext(
            AbstractValueDomain<CopyAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<CopyAnalysisContext, CopyAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            CopyAnalysisData currentAnalysisDataAtCalleeOpt = default(CopyAnalysisData),
            ImmutableArray<CopyAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<CopyAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, pessimisticAnalysis,
                  predicateAnalysis: true, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
        }

        public override int GetHashCode(int hashCode) => hashCode;
    }
}
