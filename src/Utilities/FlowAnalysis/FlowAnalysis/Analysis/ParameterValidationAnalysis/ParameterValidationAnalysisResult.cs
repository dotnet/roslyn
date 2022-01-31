// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    /// <summary>
    /// Analysis result from execution of <see cref="ParameterValidationAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ParameterValidationAnalysisResult : DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue>
    {
        public ParameterValidationAnalysisResult(
            DataFlowAnalysisResult<ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue> parameterValidationAnalysisResult,
            ImmutableDictionary<IParameterSymbol, SyntaxNode> hazardousParameterUsages)
            : base(parameterValidationAnalysisResult)
        {
            HazardousParameterUsages = hazardousParameterUsages;
        }

        public ImmutableDictionary<IParameterSymbol, SyntaxNode> HazardousParameterUsages { get; }
    }
}
