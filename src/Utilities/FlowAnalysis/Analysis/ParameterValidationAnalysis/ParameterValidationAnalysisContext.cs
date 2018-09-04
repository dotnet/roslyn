// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ParameterValidationAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ParameterValidationAnalysisContext : AbstractDataFlowAnalysisContext<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
    {
        public ParameterValidationAnalysisContext(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult> getOrComputeAnalysisResultForInvokedMethod,
            bool trackHazardousParameterUsages = false,
            ParameterValidationAnalysisData currentAnalysisDataAtCalleeOpt = default(ParameterValidationAnalysisData),
            ImmutableArray<ParameterValidationAbstractValue> argumentValuesFromCalleeOpt = default(ImmutableArray<ParameterValidationAbstractValue>),
            ImmutableHashSet<IMethodSymbol> methodsBeingAnalyzedOpt = null)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, pessimisticAnalysis,
                  predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt,
                  getOrComputeAnalysisResultForInvokedMethod: getOrComputeAnalysisResultForInvokedMethod,
                  currentAnalysisDataAtCalleeOpt: currentAnalysisDataAtCalleeOpt,
                  argumentValuesFromCalleeOpt: argumentValuesFromCalleeOpt,
                  methodsBeingAnalyzedOpt: methodsBeingAnalyzedOpt)
        {
            TrackHazardousParameterUsages = trackHazardousParameterUsages;
        }

        public ParameterValidationAnalysisContext WithTrackHazardousParameterUsages()
            => new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, ControlFlowGraph,
                OwningSymbol, PessimisticAnalysis, PointsToAnalysisResultOpt,
                GetOrComputeAnalysisResultForInvokedMethod,
                trackHazardousParameterUsages: true,
                currentAnalysisDataAtCalleeOpt: CurrentAnalysisDataFromCalleeOpt,
                argumentValuesFromCalleeOpt: ArgumentValuesFromCalleeOpt,
                methodsBeingAnalyzedOpt: MethodsBeingAnalyzedOpt);

        public bool TrackHazardousParameterUsages { get; }
        public override int GetHashCode(int hashCode) => HashUtilities.Combine(TrackHazardousParameterUsages.GetHashCode(), hashCode);
    }
}
