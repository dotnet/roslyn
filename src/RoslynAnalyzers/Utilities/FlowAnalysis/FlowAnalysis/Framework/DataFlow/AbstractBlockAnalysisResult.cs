// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Result from execution of a <see cref="DataFlowAnalysis"/> on a basic block.
    /// </summary>
    public abstract class AbstractBlockAnalysisResult
    {
        protected AbstractBlockAnalysisResult(BasicBlock basicBlock)
        {
            BasicBlock = basicBlock;
        }

        public BasicBlock BasicBlock { get; }
    }
}
