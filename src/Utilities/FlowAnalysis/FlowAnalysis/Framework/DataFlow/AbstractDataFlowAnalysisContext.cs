// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Base type for analysis contexts for execution of <see cref="DataFlowAnalysis"/> on a control flow graph.
    /// </summary>
    public abstract class AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : CacheBasedEquatable<TAnalysisContext>, IDataFlowAnalysisContext
        where TAnalysisContext : class, IDataFlowAnalysisContext
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
        protected AbstractDataFlowAnalysisContext(
            AbstractValueDomain<TAbstractAnalysisValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            bool exceptionPathsAnalysis,
            CopyAnalysisResult? copyAnalysisResultOpt,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            Func<TAnalysisContext, TAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraphOpt,
            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>? interproceduralAnalysisDataOpt,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicateOpt)
        {
            Debug.Assert(owningSymbol.Kind is SymbolKind.Method or
                SymbolKind.Field or
                SymbolKind.Property or
                SymbolKind.Event);
            Debug.Assert(Equals(owningSymbol.OriginalDefinition, owningSymbol));
            Debug.Assert(pointsToAnalysisResultOpt == null ||
                pointsToAnalysisResultOpt.ControlFlowGraph == controlFlowGraph);
            Debug.Assert(copyAnalysisResultOpt == null ||
                copyAnalysisResultOpt.ControlFlowGraph == controlFlowGraph);
            Debug.Assert(valueContentAnalysisResultOpt == null ||
                valueContentAnalysisResultOpt.ControlFlowGraph == controlFlowGraph);

            ValueDomain = valueDomain;
            WellKnownTypeProvider = wellKnownTypeProvider;
            ControlFlowGraph = controlFlowGraph;
            ParentControlFlowGraphOpt = parentControlFlowGraphOpt;
            OwningSymbol = owningSymbol;
            AnalyzerOptions = analyzerOptions;
            InterproceduralAnalysisConfiguration = interproceduralAnalysisConfig;
            PessimisticAnalysis = pessimisticAnalysis;
            PredicateAnalysis = predicateAnalysis;
            ExceptionPathsAnalysis = exceptionPathsAnalysis;
            CopyAnalysisResultOpt = copyAnalysisResultOpt;
            PointsToAnalysisResultOpt = pointsToAnalysisResultOpt;
            ValueContentAnalysisResultOpt = valueContentAnalysisResultOpt;
            TryGetOrComputeAnalysisResult = tryGetOrComputeAnalysisResult;
            InterproceduralAnalysisDataOpt = interproceduralAnalysisDataOpt;
            InterproceduralAnalysisPredicateOpt = interproceduralAnalysisPredicateOpt;
        }

        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain { get; }
        public WellKnownTypeProvider WellKnownTypeProvider { get; }
        public ControlFlowGraph ControlFlowGraph { get; }
        public ISymbol OwningSymbol { get; }
        public AnalyzerOptions AnalyzerOptions { get; }
        public InterproceduralAnalysisConfiguration InterproceduralAnalysisConfiguration { get; }
        public bool PessimisticAnalysis { get; }
        public bool PredicateAnalysis { get; }
        public bool ExceptionPathsAnalysis { get; }
        public CopyAnalysisResult? CopyAnalysisResultOpt { get; }
        public PointsToAnalysisResult? PointsToAnalysisResultOpt { get; }
        public ValueContentAnalysisResult? ValueContentAnalysisResultOpt { get; }

        public Func<TAnalysisContext, TAnalysisResult?> TryGetOrComputeAnalysisResult { get; }
        protected ControlFlowGraph? ParentControlFlowGraphOpt { get; }

        // Optional data for context sensitive analysis.
        public InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>? InterproceduralAnalysisDataOpt { get; }
        public InterproceduralAnalysisPredicate? InterproceduralAnalysisPredicateOpt { get; }

        public abstract TAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult? pointsToAnalysisResultOpt,
            CopyAnalysisResult? copyAnalysisResultOpt,
            ValueContentAnalysisResult? valueContentAnalysisResultOpt,
            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>? interproceduralAnalysisData);

        public ControlFlowGraph? GetLocalFunctionControlFlowGraph(IMethodSymbol localFunction)
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

            // Unable to find control flow graph for local function.
            // This can happen for cases where local function creation and invocations are in different interprocedural call trees.
            // See unit test "DisposeObjectsBeforeLosingScopeTests.InvocationOfLocalFunctionCachedOntoField_InterproceduralAnalysis"
            // for an example.
            // Currently, we don't support interprocedural analysis of such local function invocations.
            return null;
        }

        public ControlFlowGraph? GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation lambda)
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

                // Unable to find control flow graph for lambda.
                // This can happen for cases where lambda creation and invocations are in different interprocedural call trees.
                // See unit test "DisposeObjectsBeforeLosingScopeTests.InvocationOfLambdaCachedOntoField_InterproceduralAnalysis"
                // for an example.
                // Currently, we don't support interprocedural analysis of such lambda invocations.
                return null;
            }
        }

        protected abstract void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode);

        protected abstract bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> obj);

        protected sealed override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(ValueDomain.GetHashCode());
            hashCode.Add(OwningSymbol.GetHashCode());
            hashCode.Add(ControlFlowGraph.GetHashCode());
            hashCode.Add(AnalyzerOptions.GetHashCode());
            hashCode.Add(InterproceduralAnalysisConfiguration.GetHashCode());
            hashCode.Add(PessimisticAnalysis.GetHashCode());
            hashCode.Add(PredicateAnalysis.GetHashCode());
            hashCode.Add(ExceptionPathsAnalysis.GetHashCode());
            hashCode.Add(CopyAnalysisResultOpt.GetHashCodeOrDefault());
            hashCode.Add(PointsToAnalysisResultOpt.GetHashCodeOrDefault());
            hashCode.Add(ValueContentAnalysisResultOpt.GetHashCodeOrDefault());
            hashCode.Add(InterproceduralAnalysisDataOpt.GetHashCodeOrDefault());
            hashCode.Add(InterproceduralAnalysisPredicateOpt.GetHashCodeOrDefault());
            ComputeHashCodePartsSpecific(ref hashCode);
        }

        protected sealed override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<TAnalysisContext> obj)
        {
            var other = (AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>)obj;
            return ValueDomain.GetHashCode() == other.ValueDomain.GetHashCode()
                && OwningSymbol.GetHashCode() == other.OwningSymbol.GetHashCode()
                && ControlFlowGraph.GetHashCode() == other.ControlFlowGraph.GetHashCode()
                && AnalyzerOptions.GetHashCode() == other.AnalyzerOptions.GetHashCode()
                && InterproceduralAnalysisConfiguration.GetHashCode() == other.InterproceduralAnalysisConfiguration.GetHashCode()
                && PessimisticAnalysis.GetHashCode() == other.PessimisticAnalysis.GetHashCode()
                && PredicateAnalysis.GetHashCode() == other.PredicateAnalysis.GetHashCode()
                && ExceptionPathsAnalysis.GetHashCode() == other.ExceptionPathsAnalysis.GetHashCode()
                && CopyAnalysisResultOpt.GetHashCodeOrDefault() == other.CopyAnalysisResultOpt.GetHashCodeOrDefault()
                && PointsToAnalysisResultOpt.GetHashCodeOrDefault() == other.PointsToAnalysisResultOpt.GetHashCodeOrDefault()
                && ValueContentAnalysisResultOpt.GetHashCodeOrDefault() == other.ValueContentAnalysisResultOpt.GetHashCodeOrDefault()
                && InterproceduralAnalysisDataOpt.GetHashCodeOrDefault() == other.InterproceduralAnalysisDataOpt.GetHashCodeOrDefault()
                && InterproceduralAnalysisPredicateOpt.GetHashCodeOrDefault() == other.InterproceduralAnalysisPredicateOpt.GetHashCodeOrDefault()
                && ComputeEqualsByHashCodeParts(other);
        }
    }
}
