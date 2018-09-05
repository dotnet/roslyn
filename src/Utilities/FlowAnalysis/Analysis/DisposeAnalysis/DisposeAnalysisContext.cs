// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="DisposeAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class DisposeAnalysisContext : AbstractDataFlowAnalysisContext<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeAbstractValue>
    {
        // "PessimisticAnalysis = false"
        // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
        // reference type fields might be re-assigned to point to different objects in the called method.
        // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
        // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
        // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.

        public DisposeAnalysisContext(
            ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
            bool trackInstanceFields,
            AbstractValueDomain<DisposeAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt,
            Func<DisposeAnalysisContext, DisposeAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            DisposeAnalysisData currentAnalysisDataAtCalleeOpt = null,
            ImmutableArray<DisposeAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<DisposeAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                  pessimisticAnalysis : false,
                  predicateAnalysis: false,
                  copyAnalysisResultOpt: null,
                  pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
            DisposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
            TrackInstanceFields = trackInstanceFields;
        }

        public ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes { get; }
        public bool TrackInstanceFields { get; }

        public override int GetHashCode(int hashCode)
        {
            hashCode = HashUtilities.Combine(hashCode,
                HashUtilities.Combine(TrackInstanceFields.GetHashCode(), DisposeOwnershipTransferLikelyTypes.Count));
            foreach (var newKey in DisposeOwnershipTransferLikelyTypes.Select(t => t.GetHashCode()).Order())
            {
                hashCode = HashUtilities.Combine(newKey, hashCode);
            }

            return hashCode;
        }
    }
}
