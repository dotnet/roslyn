// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;

    /// <summary>
    /// Result from execution of <see cref="GlobalFlowStateAnalysis"/> on a basic block.
    /// It store GlobalFlowState value at the start and end of the basic block.
    /// </summary>
    internal sealed class GlobalFlowStateBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        internal GlobalFlowStateBlockAnalysisResult(BasicBlock basicBlock, GlobalFlowStateAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, GlobalFlowStateAnalysisValueSet>.Empty;
        }

        public ImmutableDictionary<AnalysisEntity, GlobalFlowStateAnalysisValueSet> Data { get; }
    }
}
