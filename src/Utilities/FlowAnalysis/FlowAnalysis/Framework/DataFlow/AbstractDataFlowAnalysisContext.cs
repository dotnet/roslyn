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
            CopyAnalysisResult? copyAnalysisResult,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            Func<TAnalysisContext, TAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
        {
            Debug.Assert(owningSymbol.Kind is SymbolKind.Method or
                SymbolKind.Field or
                SymbolKind.Property or
                SymbolKind.Event);
            Debug.Assert(Equals(owningSymbol.OriginalDefinition, owningSymbol));
            Debug.Assert(pointsToAnalysisResult == null ||
                pointsToAnalysisResult.ControlFlowGraph == controlFlowGraph);
            Debug.Assert(copyAnalysisResult == null ||
                copyAnalysisResult.ControlFlowGraph == controlFlowGraph);
            Debug.Assert(valueContentAnalysisResult == null ||
                valueContentAnalysisResult.ControlFlowGraph == controlFlowGraph);

            ValueDomain = valueDomain;
            WellKnownTypeProvider = wellKnownTypeProvider;
            ControlFlowGraph = controlFlowGraph;
            ParentControlFlowGraph = parentControlFlowGraph;
            OwningSymbol = owningSymbol;
            AnalyzerOptions = analyzerOptions;
            InterproceduralAnalysisConfiguration = interproceduralAnalysisConfig;
            PessimisticAnalysis = pessimisticAnalysis;
            PredicateAnalysis = predicateAnalysis;
            ExceptionPathsAnalysis = exceptionPathsAnalysis;
            CopyAnalysisResult = copyAnalysisResult;
            PointsToAnalysisResult = pointsToAnalysisResult;
            ValueContentAnalysisResult = valueContentAnalysisResult;
            TryGetOrComputeAnalysisResult = tryGetOrComputeAnalysisResult;
            InterproceduralAnalysisData = interproceduralAnalysisData;
            InterproceduralAnalysisPredicate = interproceduralAnalysisPredicate;
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
        public CopyAnalysisResult? CopyAnalysisResult { get; }
        public PointsToAnalysisResult? PointsToAnalysisResult { get; }
        public ValueContentAnalysisResult? ValueContentAnalysisResult { get; }

        public Func<TAnalysisContext, TAnalysisResult?> TryGetOrComputeAnalysisResult { get; }
        protected ControlFlowGraph? ParentControlFlowGraph { get; }

        // Optional data for context sensitive analysis.
        public InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>? InterproceduralAnalysisData { get; }
        public InterproceduralAnalysisPredicate? InterproceduralAnalysisPredicate { get; }

        public abstract TAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
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

            if (ParentControlFlowGraph != null && InterproceduralAnalysisData != null)
            {
                var parentAnalysisContext = InterproceduralAnalysisData.MethodsBeingAnalyzed.FirstOrDefault(context => context.ControlFlowGraph == ParentControlFlowGraph);
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
                if (ParentControlFlowGraph != null && InterproceduralAnalysisData != null)
                {
                    var parentAnalysisContext = InterproceduralAnalysisData.MethodsBeingAnalyzed.FirstOrDefault(context => context.ControlFlowGraph == ParentControlFlowGraph);
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

        protected abstract void ComputeHashCodePartsSpecific(Action<int> builder);

        protected sealed override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(ValueDomain.GetHashCode());
            addPart(OwningSymbol.GetHashCode());
            addPart(ControlFlowGraph.GetHashCode());
            addPart(AnalyzerOptions.GetHashCode());
            addPart(InterproceduralAnalysisConfiguration.GetHashCode());
            addPart(PessimisticAnalysis.GetHashCode());
            addPart(PredicateAnalysis.GetHashCode());
            addPart(ExceptionPathsAnalysis.GetHashCode());
            addPart(CopyAnalysisResult.GetHashCodeOrDefault());
            addPart(PointsToAnalysisResult.GetHashCodeOrDefault());
            addPart(ValueContentAnalysisResult.GetHashCodeOrDefault());
            addPart(InterproceduralAnalysisData.GetHashCodeOrDefault());
            addPart(InterproceduralAnalysisPredicate.GetHashCodeOrDefault());
            ComputeHashCodePartsSpecific(addPart);
        }
    }
}
