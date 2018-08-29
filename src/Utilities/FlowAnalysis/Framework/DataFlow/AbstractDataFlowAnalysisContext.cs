// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Base type for analysis contexts for execution of <see cref="DataFlowAnalysis"/> on a control flow graph.
    /// </summary>
    internal abstract class AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : IDataFlowAnalysisContext
        where TAnalysisContext: IDataFlowAnalysisContext
        where TAnalysisResult: IDataFlowAnalysisResult
    {
        public AbstractDataFlowAnalysisContext(
            AbstractValueDomain<TAbstractAnalysisValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<TAnalysisContext, TAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            TAnalysisData currentAnalysisDataAtCalleeOpt = default(TAnalysisData),
            ImmutableArray<TAbstractAnalysisValue> argumentValuesFromCalleeOpt = default(ImmutableArray<TAbstractAnalysisValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
        {
            Debug.Assert(controlFlowGraph != null);
            Debug.Assert(owningSymbol != null);
            Debug.Assert(owningSymbol.Kind == SymbolKind.Method ||
                owningSymbol.Kind == SymbolKind.Field ||
                owningSymbol.Kind == SymbolKind.Property ||
                owningSymbol.Kind == SymbolKind.Event);
            Debug.Assert(owningSymbol.OriginalDefinition == owningSymbol);
            Debug.Assert(wellKnownTypeProvider != null);

            ValueDomain = valueDomain;
            WellKnownTypeProvider = wellKnownTypeProvider;
            ControlFlowGraph = controlFlowGraph;
            OwningSymbol = owningSymbol;
            PessimisticAnalysis = pessimisticAnalysis;
            PredicateAnalysis = predicateAnalysis;
            CopyAnalysisResultOpt = copyAnalysisResultOpt;
            PointsToAnalysisResultOpt = pointsToAnalysisResultOpt;
            GetOrComputeAnalysisResultForInvokedMethod = getOrComputeAnalysisResultForInvokedMethod;
            CurrentAnalysisDataFromCalleeOpt = currentAnalysisDataAtCalleeOpt;
            ArgumentValuesFromCalleeOpt = argumentValuesFromCalleeOpt;
            MethodsBeingAnalyzedOpt = methodsBeingAnalyzedOpt;
        }

        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain { get; }
        public WellKnownTypeProvider WellKnownTypeProvider { get; }
        public ControlFlowGraph ControlFlowGraph { get; }
        public ISymbol OwningSymbol { get; }
        public bool PessimisticAnalysis { get; }
        public bool PredicateAnalysis { get; }
        public CopyAnalysisResult CopyAnalysisResultOpt { get; }
        public PointsToAnalysisResult PointsToAnalysisResultOpt { get; }
        public Func<TAnalysisContext, TAnalysisResult> GetOrComputeAnalysisResultForInvokedMethod { get; }
        
        // Optional data for context sensitive analysis.
        public TAnalysisData CurrentAnalysisDataFromCalleeOpt { get; }
        public ImmutableArray<TAbstractAnalysisValue> ArgumentValuesFromCalleeOpt { get; }
        public ImmutableHashSet<IMethodSymbol> MethodsBeingAnalyzedOpt { get; }

        public abstract int GetHashCode(int hashCode);

        public sealed override int GetHashCode()
        {
            var hashCode = HashUtilities.Combine(ValueDomain.GetHashCode(),
                HashUtilities.Combine(OwningSymbol.GetHashCode(),
                HashUtilities.Combine(ControlFlowGraph.OriginalOperation.GetHashCode(),
                HashUtilities.Combine(PessimisticAnalysis.GetHashCode(),
                HashUtilities.Combine(PredicateAnalysis.GetHashCode(),
                HashUtilities.Combine((GetOrComputeAnalysisResultForInvokedMethod != null).GetHashCode(),
                HashUtilities.Combine(CopyAnalysisResultOpt?.GetHashCode() ?? 0,
                HashUtilities.Combine(PointsToAnalysisResultOpt?.GetHashCode() ?? 0, CurrentAnalysisDataFromCalleeOpt?.GetHashCode() ?? 0))))))));

            if (!ArgumentValuesFromCalleeOpt.IsDefault)
            {
                hashCode = HashUtilities.Combine(ArgumentValuesFromCalleeOpt.Length, hashCode);
                foreach (var value in ArgumentValuesFromCalleeOpt)
                {
                    hashCode = HashUtilities.Combine(value.GetHashCode(), hashCode);
                }
            }

            if (MethodsBeingAnalyzedOpt != null)
            {
                hashCode = HashUtilities.Combine(MethodsBeingAnalyzedOpt.Count, hashCode);
                foreach (var newKey in MethodsBeingAnalyzedOpt.Select(m => m.GetHashCode()).Order())
                {
                    hashCode = HashUtilities.Combine(newKey, hashCode);
                }
            }

            return GetHashCode(hashCode);
        }
    }
}
