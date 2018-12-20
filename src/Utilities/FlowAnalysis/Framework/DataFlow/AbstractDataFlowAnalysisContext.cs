// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
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
        {
            if (localFunction.Equals(OwningSymbol))
            {
                return ControlFlowGraph;
            }

            if (ControlFlowGraph.LocalFunctions.Contains(localFunction))
            {
                return ControlFlowGraph.GetLocalFunctionControlFlowGraph(localFunction);
            }

            if (ParentControlFlowGraphOpt != null && InterproceduralAnalysisDataOpt != null)
            {
                var parentAnalysisContext = InterproceduralAnalysisDataOpt.MethodsBeingAnalyzed.FirstOrDefault(context => context.ControlFlowGraph == ParentControlFlowGraphOpt);
                return parentAnalysisContext?.GetLocalFunctionControlFlowGraph(localFunction);
            }

            Debug.Fail($"Unable to find control flow graph for {localFunction.ToDisplayString()}");
            return null;
        }

        public ControlFlowGraph GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation lambda)
        {
            // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1812
            // Remove the below workaround.
            try
            {
                return ControlFlowGraph.GetAnonymousFunctionControlFlowGraph(lambda);
            }
            catch (ArgumentOutOfRangeException)
            {
                if (ParentControlFlowGraphOpt != null && InterproceduralAnalysisDataOpt != null)
                {
                    var parentAnalysisContext = InterproceduralAnalysisDataOpt.MethodsBeingAnalyzed.FirstOrDefault(context => context.ControlFlowGraph == ParentControlFlowGraphOpt);
                    return parentAnalysisContext?.GetAnonymousFunctionControlFlowGraph(lambda);
                }

                Debug.Fail($"Unable to find control flow graph for {lambda.Symbol.ToDisplayString()}");
                return null;
            }
        }

        protected abstract void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder);

        protected sealed override void ComputeHashCodeParts(ArrayBuilder<int> builder)
        {
            builder.Add(ValueDomain.GetHashCode());
            builder.Add(OwningSymbol.GetHashCode());
            builder.Add(ControlFlowGraph.OriginalOperation.GetHashCode());
            builder.Add(InterproceduralAnalysisKind.GetHashCode());
            builder.Add(PessimisticAnalysis.GetHashCode());
            builder.Add(PredicateAnalysis.GetHashCode());
            builder.Add(CopyAnalysisResultOpt.GetHashCodeOrDefault());
            builder.Add(PointsToAnalysisResultOpt.GetHashCodeOrDefault());
            builder.Add(InterproceduralAnalysisDataOpt.GetHashCodeOrDefault());
            ComputeHashCodePartsSpecific(builder);
        }
    }
}
