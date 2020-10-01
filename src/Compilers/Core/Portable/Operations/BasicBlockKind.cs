// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// <see cref="BasicBlock"/> kind.
    /// </summary>
    public enum BasicBlockKind
    {
        /// <summary>
        /// Indicates an entry block for a <see cref="ControlFlowGraph"/>,
        /// which is always the first block in <see cref="ControlFlowGraph.Blocks"/>.
        /// </summary>
        Entry,

        /// <summary>
        /// Indicates an exit block for a <see cref="ControlFlowGraph"/>,
        /// which is always the last block in <see cref="ControlFlowGraph.Blocks"/>.
        /// </summary>
        Exit,

        /// <summary>
        /// Indicates an intermediate block for a <see cref="ControlFlowGraph"/>.
        /// </summary>
        Block
    }
}

