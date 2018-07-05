// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;

    internal partial class ParameterValidationAnalysis : ForwardDataFlowAnalysis<ParameterValidationAnalysisData, ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue>
    {
        private sealed class ParameterValidationResultWithHazardousUsages
        {
            public ParameterValidationResultWithHazardousUsages(
                DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue> parameterValidationAnalysisResult,
                ImmutableDictionary<IParameterSymbol, SyntaxNode> hazardousParameterUsages)
            {
                ParameterValidationAnalysisResult = parameterValidationAnalysisResult;
                HazardousParameterUsages = hazardousParameterUsages;
            }

            public DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue> ParameterValidationAnalysisResult { get; }
            public ImmutableDictionary<IParameterSymbol, SyntaxNode> HazardousParameterUsages { get; }
        }
    }
}
