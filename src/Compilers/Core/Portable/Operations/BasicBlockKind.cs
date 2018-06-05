// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// <see cref="BasicBlock"/> kind.
    /// </summary>
    public enum BasicBlockKind
    {
        /// <summary>
        /// Indicates an entry block for a <see cref="ControlFlowGraph"/>.
        /// </summary>
        Entry,

        /// <summary>
        /// Indicates an exit block for a <see cref="ControlFlowGraph"/>.
        /// </summary>
        Exit,

        /// <summary>
        /// Indicates an intermediate block for a <see cref="ControlFlowGraph"/>.
        /// </summary>
        Block
    }
}

