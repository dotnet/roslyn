// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
