// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Semantics associated with a <see cref="ControlFlowBranch"/>.
    /// </summary>
    public enum ControlFlowBranchSemantics
    {
        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> with no associated semantics.
        /// </summary>
        None,

        /// <summary>
        /// Represents a regular <see cref="ControlFlowBranch"/> from a source basic block to a non-null destination basic block.
        /// </summary>
        Regular,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> to the exit block, i.e. the destination block has <see cref="BasicBlockKind.Exit"/>.
        /// </summary>
        Return,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> with special structured exception handling semantics:
        ///   1. The source basic block is the last block of an enclosing finally or filter region.
        ///   2. The destination basic block is null.
        /// </summary>
        StructuredExceptionHandling,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> to indicate flow transfer to the end of program execution.
        /// The destination basic block is null for this branch.
        /// </summary>
        ProgramTermination,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> generated for an <see cref="IThrowOperation"/> with an explicit thrown exception.
        /// The destination basic block is null for this branch.
        /// </summary>
        Throw,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> generated for an <see cref="IThrowOperation"/> with in implicit rethrown exception.
        /// The destination basic block is null for this branch.
        /// </summary>
        Rethrow,

        /// <summary>
        /// Represents a <see cref="ControlFlowBranch"/> generated for error cases.
        /// </summary>
        Error,
    }
}
