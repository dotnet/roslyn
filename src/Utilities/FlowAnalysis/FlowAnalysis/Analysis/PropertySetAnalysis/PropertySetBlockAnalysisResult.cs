// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="PropertySetAnalysis"/> on a basic block.
    /// It stores <see cref="PropertySetAbstractValue"/>s for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class PropertySetBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public PropertySetBlockAnalysisResult(BasicBlock basicBlock, PropertySetAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, PropertySetAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, PropertySetAbstractValue> Data { get; }
    }
}
