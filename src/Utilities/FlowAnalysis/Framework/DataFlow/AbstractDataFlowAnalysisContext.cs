// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities;
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
        : CacheBasedEquatable<TAnalysisContext>, IDataFlowAnalysisContext
        where TAnalysisContext: class, IDataFlowAnalysisContext
        where TAnalysisResult: IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
        protected AbstractDataFlowAnalysisContext(
            AbstractValueDomain<TAbstractAnalysisValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            CopyAnalysisResult copyAnalysisResultOpt,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<TAnalysisContext, TAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> interproceduralAnalysisDataOpt)
        {
            Debug.Assert(controlFlowGraph != null);
            Debug.Assert(owningSymbol != null);
            Debug.Assert(owningSymbol.Kind == SymbolKind.Method ||
                owningSymbol.Kind == SymbolKind.Field ||
                owningSymbol.Kind == SymbolKind.Property ||
                owningSymbol.Kind == SymbolKind.Event);
            Debug.Assert(owningSymbol.OriginalDefinition == owningSymbol);
            Debug.Assert(wellKnownTypeProvider != null);
            Debug.Assert(getOrComputeAnalysisResult != null);

            ValueDomain = valueDomain;
            WellKnownTypeProvider = wellKnownTypeProvider;
            ControlFlowGraph = controlFlowGraph;
            ParentControlFlowGraphOpt = parentControlFlowGraphOpt;
            OwningSymbol = owningSymbol;
            InterproceduralAnalysisKind = interproceduralAnalysisKind;
            PessimisticAnalysis = pessimisticAnalysis;
            PredicateAnalysis = predicateAnalysis;
            CopyAnalysisResultOpt = copyAnalysisResultOpt;
            PointsToAnalysisResultOpt = pointsToAnalysisResultOpt;
            GetOrComputeAnalysisResult = getOrComputeAnalysisResult;
            InterproceduralAnalysisDataOpt = interproceduralAnalysisDataOpt;
        }

        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain { get; }
        public WellKnownTypeProvider WellKnownTypeProvider { get; }
        public ControlFlowGraph ControlFlowGraph { get; }
        public ISymbol OwningSymbol { get; }
        public InterproceduralAnalysisKind InterproceduralAnalysisKind { get; }
        public bool PessimisticAnalysis { get; }
        public bool PredicateAnalysis { get; }
        public CopyAnalysisResult CopyAnalysisResultOpt { get; }
        public PointsToAnalysisResult PointsToAnalysisResultOpt { get; }
        public Func<TAnalysisContext, TAnalysisResult> GetOrComputeAnalysisResult { get; }
        protected ControlFlowGraph ParentControlFlowGraphOpt { get; }

        // Optional data for context sensitive analysis.
        public InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> InterproceduralAnalysisDataOpt { get; }

        public abstract TAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> interproceduralAnalysisData);

        public ControlFlowGraph GetLocalFunctionControlFlowGraph(IMethodSymbol localFunction)
            => ControlFlowGraph.LocalFunctions.Contains(localFunction) ?
                ControlFlowGraph.GetLocalFunctionControlFlowGraph(localFunction):
                ParentControlFlowGraphOpt?.GetLocalFunctionControlFlowGraph(localFunction);

        public ControlFlowGraph GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation lambda)
        {
            // TODO: File a CFG bug.
            try
            {
                return ControlFlowGraph.GetAnonymousFunctionControlFlowGraph(lambda);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ParentControlFlowGraphOpt?.GetAnonymousFunctionControlFlowGraph(lambda);
            }
        }

        protected abstract int GetHashCode(int hashCode);

        protected sealed override int ComputeHashCode()
        {
            var hashCode = HashUtilities.Combine(ValueDomain.GetHashCode(),
                HashUtilities.Combine(OwningSymbol.GetHashCode(),
                HashUtilities.Combine(ControlFlowGraph.OriginalOperation.GetHashCode(),
                HashUtilities.Combine(InterproceduralAnalysisKind.GetHashCode(),
                HashUtilities.Combine(PessimisticAnalysis.GetHashCode(),
                HashUtilities.Combine(PredicateAnalysis.GetHashCode(),
                HashUtilities.Combine(CopyAnalysisResultOpt?.GetHashCode() ?? 0,
                HashUtilities.Combine(PointsToAnalysisResultOpt?.GetHashCode() ?? 0, InterproceduralAnalysisDataOpt?.GetHashCode() ?? 0))))))));
            return GetHashCode(hashCode);
        }
    }
}
