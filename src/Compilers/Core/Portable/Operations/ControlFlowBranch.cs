// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public sealed class ControlFlowBranch
    {
        internal ControlFlowBranch(
            BasicBlock source,
            BasicBlock destination,
            ControlFlowBranchSemantics semantics,
            bool isConditionalSuccessor,
            ImmutableArray<ControlFlowRegion> leavingRegions,
            ImmutableArray<ControlFlowRegion> enteringRegions,
            ImmutableArray<ControlFlowRegion> finallyRegions)
        {
            Source = source;
            Destination = destination;
            Semantics = semantics;
            IsConditionalSuccessor = isConditionalSuccessor;
            LeavingRegions = leavingRegions;
            EnteringRegions = enteringRegions;
            FinallyRegions = finallyRegions;
        }

        public BasicBlock Source { get; }

        public BasicBlock Destination { get; }

        public ControlFlowBranchSemantics Semantics { get; }

        public bool IsConditionalSuccessor { get; }

        /// <summary>
        /// What regions are exited (from inner most to outer most) if this branch is taken.
        /// </summary>
        public ImmutableArray<ControlFlowRegion> LeavingRegions { get; }

        /// <summary>
        /// What regions are entered (from outer most to inner most) if this branch is taken.
        /// </summary>
        public ImmutableArray<ControlFlowRegion> EnteringRegions { get; }

        /// <summary>
        /// The finally regions the control goes through if the branch is taken
        /// </summary>
        public ImmutableArray<ControlFlowRegion> FinallyRegions { get; }
    }
}
