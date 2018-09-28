// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents kind of conditional branch from a <see cref="BasicBlock"/>.
    /// </summary>
    public enum ControlFlowConditionKind
    {
        /// <summary>
        /// Indicates no conditional branch from a <see cref="BasicBlock"/>.
        /// Associated <see cref="BasicBlock.ConditionalSuccessor"/> is null.
        /// </summary>
        None,

        /// <summary>
        /// Indicates a conditional branch from a <see cref="BasicBlock"/>,
        /// with a non-null <see cref="BasicBlock.BranchValue"/> and <see cref="BasicBlock.ConditionalSuccessor"/>.
        /// If <see cref="BasicBlock.BranchValue"/> evaluates to <code>false</code>,
        /// then the branch <see cref="BasicBlock.ConditionalSuccessor"/> is taken.
        /// </summary>
        WhenFalse,

        /// <summary>
        /// Indicates a conditional branch from a <see cref="BasicBlock"/>,
        /// with a non-null <see cref="BasicBlock.BranchValue"/> and <see cref="BasicBlock.ConditionalSuccessor"/>.
        /// If <see cref="BasicBlock.BranchValue"/> evaluates to <code>true</code>,
        /// then the branch <see cref="BasicBlock.ConditionalSuccessor"/> is taken.
        /// </summary>
        WhenTrue
    }
}

