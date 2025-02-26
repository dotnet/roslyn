// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralPointsToAnalysisData = InterproceduralAnalysisData<PointsToAnalysisData, PointsToAnalysisContext, PointsToAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="PointsToAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class PointsToAnalysisContext : AbstractDataFlowAnalysisContext<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToAbstractValue>
    {
        private PointsToAnalysisContext(
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            PointsToAnalysisKind pointsToAnalysisKind,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            CopyAnalysisResult? copyAnalysisResult,
            Func<PointsToAnalysisContext, PointsToAnalysisResult?> tryGetOrComputeAnalysisResult,
            ControlFlowGraph? parentControlFlowGraph,
            InterproceduralPointsToAnalysisData? interproceduralAnalysisData,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, interproceduralAnalysisConfig, pessimisticAnalysis,
                  predicateAnalysis: true, exceptionPathsAnalysis, copyAnalysisResult, pointsToAnalysisResult: null, valueContentAnalysisResult: null,
                  tryGetOrComputeAnalysisResult, parentControlFlowGraph, interproceduralAnalysisData, interproceduralAnalysisPredicate)
        {
            Debug.Assert(pointsToAnalysisKind != PointsToAnalysisKind.None);

            PointsToAnalysisKind = pointsToAnalysisKind;
        }

        public PointsToAnalysisKind PointsToAnalysisKind { get; }

        internal static PointsToAnalysisContext Create(
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            AnalyzerOptions analyzerOptions,
            PointsToAnalysisKind pointsToAnalysisKind,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            bool exceptionPathsAnalysis,
            CopyAnalysisResult? copyAnalysisResult,
            Func<PointsToAnalysisContext, PointsToAnalysisResult?> tryGetOrComputeAnalysisResult,
            InterproceduralAnalysisPredicate? interproceduralAnalysisPredicate)
        {
            return new PointsToAnalysisContext(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, analyzerOptions, pointsToAnalysisKind, interproceduralAnalysisConfig,
                pessimisticAnalysis, exceptionPathsAnalysis, copyAnalysisResult, tryGetOrComputeAnalysisResult, parentControlFlowGraph: null,
                interproceduralAnalysisData: null, interproceduralAnalysisPredicate);
        }

        public override PointsToAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            PointsToAnalysisResult? pointsToAnalysisResult,
            CopyAnalysisResult? copyAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult,
            InterproceduralPointsToAnalysisData? interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResult == null);
            Debug.Assert(valueContentAnalysisResult == null);

            return new PointsToAnalysisContext(ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, AnalyzerOptions, PointsToAnalysisKind, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, ExceptionPathsAnalysis, copyAnalysisResult, TryGetOrComputeAnalysisResult, ControlFlowGraph, interproceduralAnalysisData,
                InterproceduralAnalysisPredicate);
        }

        protected override void ComputeHashCodePartsSpecific(ref RoslynHashCode hashCode)
        {
            hashCode.Add(((int)PointsToAnalysisKind).GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(AbstractDataFlowAnalysisContext<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToAbstractValue> obj)
        {
            var other = (PointsToAnalysisContext)obj;
            return ((int)PointsToAnalysisKind).GetHashCode() == ((int)other.PointsToAnalysisKind).GetHashCode();
        }
    }
}
