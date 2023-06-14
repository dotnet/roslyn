// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="ParameterValidationAnalysis"/> on a basic block.
    /// It stores ParameterValidation values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class ParameterValidationBlockAnalysisResult(BasicBlock basicBlock, ParameterValidationAnalysisData blockAnalysisData) : AbstractBlockAnalysisResult(basicBlock)
    {
        public ImmutableDictionary<AbstractLocation, ParameterValidationAbstractValue> Data { get; } = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, ParameterValidationAbstractValue>.Empty;
    }
}
