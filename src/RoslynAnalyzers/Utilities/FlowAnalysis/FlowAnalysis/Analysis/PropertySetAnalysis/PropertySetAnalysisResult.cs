// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Analysis result from execution of <see cref="PropertySetAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class PropertySetAnalysisResult : DataFlowAnalysisResult<PropertySetBlockAnalysisResult, PropertySetAbstractValue>
    {
        public PropertySetAnalysisResult(
            DataFlowAnalysisResult<PropertySetBlockAnalysisResult, PropertySetAbstractValue> propertySetAnalysisResult,
            ImmutableDictionary<(Location Location, IMethodSymbol? Method), HazardousUsageEvaluationResult> hazardousUsages,
            ImmutableHashSet<IMethodSymbol> visitedLocalFunctions,
            ImmutableHashSet<IFlowAnonymousFunctionOperation> visitedLambdas)
            : base(propertySetAnalysisResult)
        {
            this.HazardousUsages = hazardousUsages;
            this.VisitedLocalFunctions = visitedLocalFunctions;
            this.VisitedLambdas = visitedLambdas;
        }

        // Method == null => return / initialization
        public ImmutableDictionary<(Location Location, IMethodSymbol? Method), HazardousUsageEvaluationResult> HazardousUsages { get; }

        public ImmutableHashSet<IMethodSymbol> VisitedLocalFunctions { get; }

        public ImmutableHashSet<IFlowAnonymousFunctionOperation> VisitedLambdas { get; }
    }
}
