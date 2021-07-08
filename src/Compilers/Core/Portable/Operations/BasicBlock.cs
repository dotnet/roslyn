// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents a basic block in a <see cref="ControlFlowGraph"/> with a sequence of <see cref="Operations"/>.
    /// Once a basic block is entered, all operations in it are always executed.
    /// Optional <see cref="BranchValue"/>, if non-null, is evaluated after the <see cref="Operations"/>.
    /// Control flow leaves the basic block by taking either the <see cref="ConditionalSuccessor"/> branch or
    /// the <see cref="FallThroughSuccessor"/> branch.
    /// </summary>
    public sealed class BasicBlock
    {
#if DEBUG
        private bool _successorsAreSealed;
        private bool _predecessorsAreSealed;
#endif

        private ControlFlowBranch? _lazySuccessor;
        private ControlFlowBranch? _lazyConditionalSuccessor;
        private ImmutableArray<ControlFlowBranch> _lazyPredecessors;

        internal BasicBlock(
            BasicBlockKind kind,
            ImmutableArray<IOperation> operations,
            IOperation? branchValue,
            ControlFlowConditionKind conditionKind,
            int ordinal,
            bool isReachable,
            ControlFlowRegion region)
        {
            Kind = kind;
            Operations = operations;
            BranchValue = branchValue;
            ConditionKind = conditionKind;
            Ordinal = ordinal;
            IsReachable = isReachable;
            EnclosingRegion = region;
        }

        /// <summary>
        /// Basic block kind (entry, block, or exit).
        /// </summary>
        public BasicBlockKind Kind { get; }

        /// <summary>
        /// Sequence of operations in the basic block.
        /// </summary>
        public ImmutableArray<IOperation> Operations { get; }

        /// <summary>
        /// Optional branch value, which if non-null, is evaluated after <see cref="Operations"/>.
        /// For conditional branches, this value is used to represent the condition which determines if
        /// <see cref="ConditionalSuccessor"/> is taken or not.
        /// For non-conditional branches, this value is used to represent the return or throw value associated
        /// with the <see cref="FallThroughSuccessor"/>.
        /// </summary>
        public IOperation? BranchValue { get; }

        /// <summary>
        /// Indicates the condition kind for the branch out of the basic block.
        /// </summary>
        public ControlFlowConditionKind ConditionKind { get; }

        /// <summary>
        /// Optional fall through branch executed at the end of the basic block.
        /// This branch is null for exit block, and non-null for all other basic blocks.
        /// </summary>
        public ControlFlowBranch? FallThroughSuccessor
        {
            get
            {
#if DEBUG
                Debug.Assert(_successorsAreSealed);
#endif

                return _lazySuccessor;
            }
        }

        /// <summary>
        /// Optional conditional branch out of the basic block.
        /// If non-null, this branch may be taken at the end of the basic block based
        /// on the <see cref="ConditionKind"/> and <see cref="BranchValue"/>.
        /// </summary>
        public ControlFlowBranch? ConditionalSuccessor
        {
            get
            {
#if DEBUG
                Debug.Assert(_successorsAreSealed);
#endif

                return _lazyConditionalSuccessor;
            }
        }

        /// <summary>
        /// List of basic blocks which have a control flow branch (<see cref="FallThroughSuccessor"/> or <see cref="ConditionalSuccessor"/>)
        /// into this basic block.
        /// </summary>
        public ImmutableArray<ControlFlowBranch> Predecessors
        {
            get
            {
#if DEBUG
                Debug.Assert(_predecessorsAreSealed);
#endif
                return _lazyPredecessors;
            }
        }

        /// <summary>
        /// Unique ordinal for each basic block in a <see cref="ControlFlowGraph"/>,
        /// which can be used to index into <see cref="ControlFlowGraph.Blocks"/> array.
        /// </summary>
        public int Ordinal { get; }

        /// <summary>
        /// Indicates if control flow can reach this basic block from the entry block of the graph.
        /// </summary>
        public bool IsReachable { get; }

        /// <summary>
        /// Enclosing region.
        /// </summary>
        public ControlFlowRegion EnclosingRegion { get; }

        internal void SetSuccessors(ControlFlowBranch? successor, ControlFlowBranch? conditionalSuccessor)
        {
#if DEBUG
            Debug.Assert(!_successorsAreSealed);
            Debug.Assert(_lazySuccessor == null);
            Debug.Assert(_lazyConditionalSuccessor == null);
#endif

            _lazySuccessor = successor;
            _lazyConditionalSuccessor = conditionalSuccessor;

#if DEBUG
            _successorsAreSealed = true;
#endif
        }

        internal void SetPredecessors(ImmutableArray<ControlFlowBranch> predecessors)
        {
#if DEBUG
            Debug.Assert(!_predecessorsAreSealed);
            Debug.Assert(_lazyPredecessors.IsDefault);
            Debug.Assert(!predecessors.IsDefault);
#endif

            _lazyPredecessors = predecessors;

#if DEBUG
            _predecessorsAreSealed = true;
#endif
        }
    }
}
