// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralParameterValidationAnalysisData = InterproceduralAnalysisData<IDictionary<AbstractLocation, ParameterValidationAbstractValue>, ParameterValidationAnalysisContext, ParameterValidationAbstractValue>;
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ParameterValidationAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ParameterValidationAnalysisContext : AbstractDataFlowAnalysisContext<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
    {
        private ParameterValidationAnalysisContext(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult> getOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralParameterValidationAnalysisData interproceduralAnalysisDataOpt,
            bool trackHazardousParameterUsages)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisKind, pessimisticAnalysis,
                  predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResult: getOrComputeAnalysisResult,
                  parentControlFlowGraphOpt: parentControlFlowGraphOpt,
                  interproceduralAnalysisDataOpt: interproceduralAnalysisDataOpt)
        {
            TrackHazardousParameterUsages = trackHazardousParameterUsages;
        }

        public static ParameterValidationAnalysisContext Create(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult> getOrComputeAnalysisResult)
        {
            return new ParameterValidationAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisKind,
                pessimisticAnalysis, pointsToAnalysisResultOpt, getOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null, trackHazardousParameterUsages: false);
        }

        public override ParameterValidationAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            InterproceduralParameterValidationAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);

            // Do not invoke any interprocedural analysis more than one level down.
            // We only care about analyzing validation methods.
            return new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod, InterproceduralAnalysisKind.None,
                PessimisticAnalysis, pointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData, TrackHazardousParameterUsages);
        }

        public ParameterValidationAnalysisContext WithTrackHazardousParameterUsages()
            => new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, ControlFlowGraph,
                OwningSymbol, InterproceduralAnalysisKind, PessimisticAnalysis,
                PointsToAnalysisResultOpt, GetOrComputeAnalysisResult, ParentControlFlowGraphOpt,
                InterproceduralAnalysisDataOpt, trackHazardousParameterUsages: true);

        public bool TrackHazardousParameterUsages { get; }
        protected override int GetHashCode(int hashCode)
            => HashUtilities.Combine(TrackHazardousParameterUsages.GetHashCode(), hashCode);
    }
}
