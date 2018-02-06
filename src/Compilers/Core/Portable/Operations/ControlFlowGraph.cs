// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public sealed class ControlFlowGraph
    {
        internal ControlFlowGraph(ImmutableArray<BasicBlock> blocks)
        {
            Blocks = blocks;
        }

        /// <summary>
        /// PROTOTYPE(dataflow): Add documentation
        /// </summary>
        public ImmutableArray<BasicBlock> Blocks { get; }
    }
}
