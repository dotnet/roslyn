// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        internal sealed class BasicBlockBuilder
        {
            private readonly ArrayBuilder<IOperation> _statementsBuilder;
            private readonly HashSet<BasicBlockBuilder> _predecessorsBuilder;
            public (IOperation Condition, bool JumpIfTrue, Branch Branch) InternalConditional;
            public (IOperation Value, Branch Branch) InternalNext;
            
            public BasicBlockBuilder(BasicBlockKind kind)
            {
                Kind = kind;
                _statementsBuilder = ArrayBuilder<IOperation>.GetInstance();
                _predecessorsBuilder = new HashSet<BasicBlockBuilder>();
            }

            public IReadOnlyCollection<IOperation> Statements => _statementsBuilder;
            public ImmutableArray<IOperation> GetImmutableStatements() => _statementsBuilder.ToImmutableAndFree();
            public ISet<BasicBlockBuilder> Predecessors => _predecessorsBuilder;
            public BasicBlockKind Kind { get; }

            public int Ordinal { get; set; } = -1;

            public bool IsReachable { get; set; } = false;

            public ControlFlowRegion Region { get; set; }

            internal void AddStatement(IOperation statement)
            {
                _statementsBuilder.Add(statement);
            }

            internal void AddStatements(IEnumerable<IOperation> statements)
            {
                _statementsBuilder.AddRange(statements);
            }

            internal void RemoveStatements()
            {
                _statementsBuilder.Clear();
            }

            internal void AddPredecessor(BasicBlockBuilder block)
            {
                _predecessorsBuilder.Add(block);
            }

            internal void RemovePredecessor(BasicBlockBuilder block)
            {
                _predecessorsBuilder.Remove(block);
            }

            public struct Branch
            {
                private ImmutableArray<ControlFlowRegion> _lazyFinallyRegions;

                public ControlFlowBranchSemantics Kind { get; set; }
                public BasicBlockBuilder Destination { get; set; }

                /// <summary>
                /// What regions are exited (from inner most to outer most) if this branch is taken.
                /// </summary>
                public ImmutableArray<ControlFlowRegion> LeavingRegions { get; set; }

                /// <summary>
                /// What regions are entered (from outer most to inner most) if this branch is taken.
                /// </summary>
                public ImmutableArray<ControlFlowRegion> EnteringRegions { get; set; }

                /// <summary>
                /// The finally regions the control goes through if the branch is taken
                /// </summary>
                public ImmutableArray<ControlFlowRegion> FinallyRegions
                {
                    get
                    {
                        if (_lazyFinallyRegions.IsDefault)
                        {
                            ArrayBuilder<ControlFlowRegion> builder = null;
                            ImmutableArray<ControlFlowRegion> leavingRegions = LeavingRegions;
                            int stopAt = leavingRegions.Length - 1;
                            for (int i = 0; i < stopAt; i++)
                            {
                                if (leavingRegions[i].Kind == ControlFlowRegionKind.Try && leavingRegions[i + 1].Kind == ControlFlowRegionKind.TryAndFinally)
                                {
                                    if (builder == null)
                                    {
                                        builder = ArrayBuilder<ControlFlowRegion>.GetInstance();
                                    }

                                    builder.Add(leavingRegions[i + 1].NestedRegions.Last());
                                    Debug.Assert(builder.Last().Kind == ControlFlowRegionKind.Finally);
                                }
                            }

                            var result = builder == null ? ImmutableArray<ControlFlowRegion>.Empty : builder.ToImmutableAndFree();

                            ImmutableInterlocked.InterlockedInitialize(ref _lazyFinallyRegions, result);
                        }

                        return _lazyFinallyRegions;
                    }
                }
            }
        }
    }
}
