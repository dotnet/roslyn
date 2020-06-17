// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal sealed class FlightEnabledAnalysisCallbackContext
    {
        public FlightEnabledAnalysisCallbackContext(
            IMethodSymbol invokedMethod,
            ImmutableArray<IArgumentOperation> arguments,
            PointsToAnalysisResult? pointsToAnalysisResult,
            ValueContentAnalysisResult? valueContentAnalysisResult)
        {
            InvokedMethod = invokedMethod;
            Arguments = arguments;
            PointsToAnalysisResult = pointsToAnalysisResult;
            ValueContentAnalysisResult = valueContentAnalysisResult;
        }

        public IMethodSymbol InvokedMethod { get; }
        public ImmutableArray<IArgumentOperation> Arguments { get; }
        public PointsToAnalysisResult? PointsToAnalysisResult { get; }
        public ValueContentAnalysisResult? ValueContentAnalysisResult { get; }
    }
}
