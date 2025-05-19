// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="ParameterValidationAnalysis"/> on a basic block.
    /// It stores ParameterValidation values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class ParameterValidationBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ParameterValidationBlockAnalysisResult(BasicBlock basicBlock, ParameterValidationAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, ParameterValidationAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, ParameterValidationAbstractValue> Data { get; }
    }
}
