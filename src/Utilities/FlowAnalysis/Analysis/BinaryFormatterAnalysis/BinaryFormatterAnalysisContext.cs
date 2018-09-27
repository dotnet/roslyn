// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using BinaryFormatterAnalysisResult = DataFlowAnalysisResult<BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralBinaryFormatterAnalysisData = InterproceduralAnalysisData<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="BinaryFormatterAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class BinaryFormatterAnalysisContext : AbstractDataFlowAnalysisContext<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult, BinaryFormatterAbstractValue>
    {
        private BinaryFormatterAnalysisContext(
            AbstractValueDomain<BinaryFormatterAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralBinaryFormatterAnalysisData interproceduralAnalysisDataOpt)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis,
                  predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
        }

        public static BinaryFormatterAnalysisContext Create(
            AbstractValueDomain<BinaryFormatterAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult> getOrComputeAnalysisResult)
        {
            return new BinaryFormatterAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisKind,
                pessimisticAnalysis, pointsToAnalysisResultOpt, getOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null);
        }

        public override BinaryFormatterAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralBinaryFormatterAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);

            // Do not invoke any interprocedural analysis more than one level down.
            // We only care about analyzing validation methods.
            return new BinaryFormatterAnalysisContext(
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, InterproceduralAnalysisKind.None,
                PessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData);
        }

        public BinaryFormatterAnalysisContext WithTrackHazardousParameterUsages()
            => new BinaryFormatterAnalysisContext(
                ValueDomain, WellKnownTypeProvider, ControlFlowGraph,
                OwningSymbol, InterproceduralAnalysisKind, PessimisticAnalysis,
                PointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ParentControlFlowGraphOpt,
                InterproceduralAnalysisDataOpt);

        protected override int GetHashCode(int hashCode)
        {
            return hashCode;
        }
    }
}
