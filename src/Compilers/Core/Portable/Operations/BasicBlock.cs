// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public sealed class BasicBlock
    {
#if DEBUG
        private bool _successorsAreSealed;
        private bool _predecessorsAreSealed;
#endif

        private ControlFlowBranch _lazySuccessor;
        private ControlFlowBranch _lazyConditionalSuccessor;
        private ImmutableArray<ControlFlowBranch> _lazyPredecessors;

        internal BasicBlock(
            BasicBlockKind kind,
            ImmutableArray<IOperation> operations,
            IOperation branchValue,
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

        public BasicBlockKind Kind { get; }

        public ImmutableArray<IOperation> Operations { get; }

        public IOperation BranchValue { get; }

        public ControlFlowConditionKind ConditionKind { get; }
        public ControlFlowBranch FallThroughSuccessor
        {
            get
            {
#if DEBUG
                Debug.Assert(_successorsAreSealed);
#endif

                return _lazySuccessor;
            }
        }

        public ControlFlowBranch ConditionalSuccessor
        {
            get
            {
#if DEBUG
                Debug.Assert(_successorsAreSealed);
#endif

                return _lazyConditionalSuccessor;
            }
        }

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

        public int Ordinal { get; }

        public bool IsReachable { get; }

        /// <summary>
        /// Enclosing region
        /// </summary>
        public ControlFlowRegion EnclosingRegion { get; }

        internal void SetSuccessors(ControlFlowBranch successor, ControlFlowBranch conditionalSuccessor)
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
