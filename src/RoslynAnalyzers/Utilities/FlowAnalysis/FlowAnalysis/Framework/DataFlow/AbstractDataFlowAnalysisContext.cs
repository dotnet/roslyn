// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        where TAnalysisData : AbstractAnalysisData
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
            hashCode.Add(CopyAnalysisResult.GetHashCodeOrDefault());
            hashCode.Add(PointsToAnalysisResult.GetHashCodeOrDefault());
            hashCode.Add(ValueContentAnalysisResult.GetHashCodeOrDefault());
            hashCode.Add(InterproceduralAnalysisData.GetHashCodeOrDefault());
            hashCode.Add(InterproceduralAnalysisPredicate.GetHashCodeOrDefault());
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
                && CopyAnalysisResult.GetHashCodeOrDefault() == other.CopyAnalysisResult.GetHashCodeOrDefault()
                && PointsToAnalysisResult.GetHashCodeOrDefault() == other.PointsToAnalysisResult.GetHashCodeOrDefault()
                && ValueContentAnalysisResult.GetHashCodeOrDefault() == other.ValueContentAnalysisResult.GetHashCodeOrDefault()
                && InterproceduralAnalysisData.GetHashCodeOrDefault() == other.InterproceduralAnalysisData.GetHashCodeOrDefault()
                && InterproceduralAnalysisPredicate.GetHashCodeOrDefault() == other.InterproceduralAnalysisPredicate.GetHashCodeOrDefault()
                && ComputeEqualsByHashCodeParts(other);
        }
    }
}
