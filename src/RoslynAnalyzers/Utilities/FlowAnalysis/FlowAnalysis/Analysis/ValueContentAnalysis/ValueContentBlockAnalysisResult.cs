// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    /// <summary>
    /// Result from execution of <see cref="ValueContentAnalysis"/> on a basic block.
    /// It stores data values for each <see cref="AnalysisEntity"/> at the start and end of the basic block.
    /// </summary>
    public class ValueContentBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        internal ValueContentBlockAnalysisResult(BasicBlock basicBlock, ValueContentAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue>.Empty;
            IsReachable = blockAnalysisData?.IsReachableBlockData ?? true;
        }

        public ImmutableDictionary<AnalysisEntity, ValueContentAbstractValue> Data { get; }
        public bool IsReachable { get; }
    }
}
