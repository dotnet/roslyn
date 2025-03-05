// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static class ControlFlowBranchExtensions
    {
        public static bool IsBackEdge(this ControlFlowBranch controlFlowBranch)
            => controlFlowBranch?.Source != null &&
               controlFlowBranch.Destination != null &&
               controlFlowBranch.Source.Ordinal >= controlFlowBranch.Destination.Ordinal;
    }
}
