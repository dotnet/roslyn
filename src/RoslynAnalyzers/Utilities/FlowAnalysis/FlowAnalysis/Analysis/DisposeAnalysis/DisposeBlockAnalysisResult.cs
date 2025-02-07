// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="DisposeAnalysis"/> on a basic block.
    /// It store dispose values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    public class DisposeBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        internal DisposeBlockAnalysisResult(BasicBlock basicBlock, DisposeAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, DisposeAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, DisposeAbstractValue> Data { get; }
    }
}
