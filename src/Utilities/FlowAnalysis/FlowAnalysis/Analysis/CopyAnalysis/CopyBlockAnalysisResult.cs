// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Result from execution of <see cref="CopyAnalysis"/> on a basic block.
    /// It store copy values for each <see cref="AnalysisEntity"/> at the start and end of the basic block.
    /// </summary>
    public sealed class CopyBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        internal CopyBlockAnalysisResult(BasicBlock basicBlock, CopyAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.CoreAnalysisData.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, CopyAbstractValue>.Empty;
            IsReachable = blockAnalysisData?.IsReachableBlockData ?? true;
        }

        public ImmutableDictionary<AnalysisEntity, CopyAbstractValue> Data { get; }
        public bool IsReachable { get; }
    }
}
