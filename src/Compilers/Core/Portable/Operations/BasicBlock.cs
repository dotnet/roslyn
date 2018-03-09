// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// </summary>
    public enum BasicBlockKind
    {
        Entry,
        Exit,
        Block
    }

    /// <summary>
    /// PROTOTYPE(dataflow): Add documentation
    /// PROTOTYPE(dataflow): We need to figure out how to split it into a builder and 
    ///                      a public immutable type.
    /// </summary>
    public sealed class BasicBlock
    {
        private readonly ImmutableArray<IOperation>.Builder _statements;
        private readonly ImmutableHashSet<BasicBlock>.Builder _predecessors;
        internal (IOperation Value, Branch Branch) InternalNext;
        internal (IOperation Condition, bool JumpIfTrue, Branch Branch) InternalConditional;

        public BasicBlock(BasicBlockKind kind)
        {
            Kind = kind;
            _statements = ImmutableArray.CreateBuilder<IOperation>();
            _predecessors = ImmutableHashSet.CreateBuilder<BasicBlock>();
        }

        public BasicBlockKind Kind { get; private set; }
        public ImmutableArray<IOperation> Statements => _statements.ToImmutable();

        /// <summary>
        /// PROTOTYPE(dataflow): Tuple is temporary return type, we probably should use special structure instead.
        /// </summary>
        public (IOperation Condition, bool JumpIfTrue, Branch Branch) Conditional => InternalConditional;

        /// <summary>
        /// PROTOTYPE(dataflow): During CR there was a suggestion to use different name - "Successor".
        /// </summary>
        public (IOperation Value, Branch Branch) Next => InternalNext;

        public ImmutableHashSet<BasicBlock> Predecessors => _predecessors.ToImmutable();

        public int Ordinal { get; internal set; } = -1;

        /// <summary>
        /// Enclosing region
        /// </summary>
        public ControlFlowGraph.Region Region { get; internal set; }

        internal void AddStatement(IOperation statement)
        {
            _statements.Add(statement);
        }

        internal void AddStatements(ImmutableArray<IOperation> statements)
        {
            _statements.AddRange(statements);
        }

        internal void RemoveStatements()
        {
            _statements.Clear();
        }

        internal void AddPredecessor(BasicBlock block)
        {
            _predecessors.Add(block);
        }

        internal void RemovePredecessor(BasicBlock block)
        {
            _predecessors.Remove(block);
        }

        public enum BranchKind
        {
            None,
            Regular,
            Return,
            StructuredExceptionHandling,
            ProgramTermination,
            Throw,
            ReThrow,
        }

        public struct Branch
        {
            private ImmutableArray<ControlFlowGraph.Region> _lazyFinallyRegions;

            public BranchKind Kind { get; internal set; }
            public BasicBlock Destination { get; internal set; }

            /// <summary>
            /// What regions are exited (from inner most to outer most) if this branch is taken.
            /// </summary>
            public ImmutableArray<ControlFlowGraph.Region> LeavingRegions { get; internal set; }

            /// <summary>
            /// What regions are entered (from outer most to inner most) if this branch is taken.
            /// </summary>
            public ImmutableArray<ControlFlowGraph.Region> EnteringRegions { get; internal set; }

            /// <summary>
            /// The finally regions the control goes through if the branch is taken
            /// </summary>
            public ImmutableArray<ControlFlowGraph.Region> FinallyRegions
            {
                get
                {
                    if (_lazyFinallyRegions.IsDefault)
                    {
                        ArrayBuilder<ControlFlowGraph.Region> builder = null;
                        ImmutableArray<ControlFlowGraph.Region> leavingRegions = LeavingRegions;
                        int stopAt = leavingRegions.Length - 1;
                        for (int i = 0; i < stopAt; i++)
                        {
                            if (leavingRegions[i].Kind == ControlFlowGraph.RegionKind.Try && leavingRegions[i+1].Kind == ControlFlowGraph.RegionKind.TryAndFinally)
                            {
                                if (builder == null)
                                {
                                    builder = ArrayBuilder<ControlFlowGraph.Region>.GetInstance();
                                }

                                builder.Add(leavingRegions[i + 1].Regions.Last());
                                Debug.Assert(builder.Last().Kind == ControlFlowGraph.RegionKind.Finally);
                            }
                        }

                        var result = builder == null ? ImmutableArray<ControlFlowGraph.Region>.Empty : builder.ToImmutableAndFree();

                        ImmutableInterlocked.InterlockedInitialize(ref _lazyFinallyRegions, result);
                    }

                    return _lazyFinallyRegions;
                }
            }
        }
    }
}
