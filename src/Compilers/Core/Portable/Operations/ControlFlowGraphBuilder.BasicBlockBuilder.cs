// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        internal sealed class BasicBlockBuilder
        {
            public int Ordinal;
            public readonly BasicBlockKind Kind;
            private ArrayBuilder<IOperation>? _statements;

            // The most common case is that we have one, or two predecessors.
            // Let's avoid allocating a HashSet for these cases.
            private BasicBlockBuilder? _predecessor1;
            private BasicBlockBuilder? _predecessor2;
            private PooledHashSet<BasicBlockBuilder>? _predecessors;

            public IOperation? BranchValue;
            public ControlFlowConditionKind ConditionKind;
            public Branch Conditional;
            public Branch FallThrough;

            public bool IsReachable;
            public ControlFlowRegion? Region;

            public BasicBlockBuilder(BasicBlockKind kind)
            {
                Kind = kind;
                Ordinal = -1;
                IsReachable = false;
            }

            [MemberNotNullWhen(true, nameof(StatementsOpt))]
#pragma warning disable CS8775
            public bool HasStatements => _statements?.Count > 0;
#pragma warning restore

            public ArrayBuilder<IOperation>? StatementsOpt => _statements;

            public void AddStatement(IOperation operation)
            {
                Debug.Assert(operation != null);

                if (_statements == null)
                {
                    _statements = ArrayBuilder<IOperation>.GetInstance();
                }

                _statements.Add(operation);
            }

            public void MoveStatementsFrom(BasicBlockBuilder other)
            {
                if (other._statements == null)
                {
                    return;
                }
                else if (_statements == null)
                {
                    _statements = other._statements;
                    other._statements = null;
                }
                else
                {
                    _statements.AddRange(other._statements);
                    other._statements.Clear();
                }
            }

            public BasicBlock ToImmutable()
            {
                Debug.Assert(Region != null);
                var block = new BasicBlock(Kind,
                                           _statements?.ToImmutableAndFree() ?? ImmutableArray<IOperation>.Empty,
                                           BranchValue,
                                           ConditionKind,
                                           Ordinal,
                                           IsReachable,
                                           Region);
                _statements = null;
                return block;
            }

            public bool HasPredecessors
            {
                get
                {
                    if (_predecessors != null)
                    {
                        Debug.Assert(_predecessor1 == null);
                        Debug.Assert(_predecessor2 == null);
                        return _predecessors.Count > 0;
                    }
                    else
                    {
                        return _predecessor1 != null || _predecessor2 != null;
                    }
                }
            }

            [MemberNotNullWhen(true, nameof(BranchValue))]
            public bool HasCondition
            {
                get
                {
                    bool result = ConditionKind != ControlFlowConditionKind.None;
                    Debug.Assert(!result || BranchValue != null);
                    return result;
                }
            }

            public BasicBlockBuilder? GetSingletonPredecessorOrDefault()
            {
                if (_predecessors != null)
                {
                    Debug.Assert(_predecessor1 == null);
                    Debug.Assert(_predecessor2 == null);
                    return _predecessors.AsSingleton();
                }
                else if (_predecessor2 == null)
                {
                    return _predecessor1;
                }
                else if (_predecessor1 == null)
                {
                    return _predecessor2;
                }
                else
                {
                    return null;
                }
            }

            public void AddPredecessor(BasicBlockBuilder predecessor)
            {
                Debug.Assert(predecessor != null);

                if (_predecessors != null)
                {
                    Debug.Assert(_predecessor1 == null);
                    Debug.Assert(_predecessor2 == null);
                    _predecessors.Add(predecessor);
                }
                else if (_predecessor1 == predecessor)
                {
                    return;
                }
                else if (_predecessor2 == predecessor)
                {
                    return;
                }
                else if (_predecessor1 == null)
                {
                    _predecessor1 = predecessor;
                }
                else if (_predecessor2 == null)
                {
                    _predecessor2 = predecessor;
                }
                else
                {
                    _predecessors = PooledHashSet<BasicBlockBuilder>.GetInstance();
                    _predecessors.Add(_predecessor1);
                    _predecessors.Add(_predecessor2);
                    _predecessors.Add(predecessor);
                    Debug.Assert(_predecessors.Count == 3);
                    _predecessor1 = null;
                    _predecessor2 = null;
                }
            }

            public void RemovePredecessor(BasicBlockBuilder predecessor)
            {
                Debug.Assert(predecessor != null);

                if (_predecessors != null)
                {
                    Debug.Assert(_predecessor1 == null);
                    Debug.Assert(_predecessor2 == null);
                    _predecessors.Remove(predecessor);
                }
                else if (_predecessor1 == predecessor)
                {
                    _predecessor1 = null;
                }
                else if (_predecessor2 == predecessor)
                {
                    _predecessor2 = null;
                }
            }

            public void GetPredecessors(ArrayBuilder<BasicBlockBuilder> builder)
            {
                if (_predecessors != null)
                {
                    Debug.Assert(_predecessor1 == null);
                    Debug.Assert(_predecessor2 == null);

                    foreach (BasicBlockBuilder predecessor in _predecessors)
                    {
                        builder.Add(predecessor);
                    }

                    return;
                }

                if (_predecessor1 != null)
                {
                    builder.Add(_predecessor1);
                }

                if (_predecessor2 != null)
                {
                    builder.Add(_predecessor2);
                }
            }

            public ImmutableArray<ControlFlowBranch> ConvertPredecessorsToBranches(ArrayBuilder<BasicBlock> blocks)
            {
                if (!HasPredecessors)
                {
                    _predecessors?.Free();
                    _predecessors = null;
                    return ImmutableArray<ControlFlowBranch>.Empty;
                }

                BasicBlock block = blocks[Ordinal];

                var branches = ArrayBuilder<ControlFlowBranch>.GetInstance(_predecessors?.Count ?? 2);

                if (_predecessors != null)
                {
                    Debug.Assert(_predecessor1 == null);
                    Debug.Assert(_predecessor2 == null);

                    foreach (BasicBlockBuilder predecessorBlockBuilder in _predecessors)
                    {
                        addBranches(predecessorBlockBuilder);
                    }

                    _predecessors.Free();
                    _predecessors = null;
                }
                else
                {
                    if (_predecessor1 != null)
                    {
                        addBranches(_predecessor1);
                        _predecessor1 = null;
                    }

                    if (_predecessor2 != null)
                    {
                        addBranches(_predecessor2);
                        _predecessor2 = null;
                    }
                }

                // Order predecessors by source ordinal and conditional first to ensure deterministic predecessor ordering.
                branches.Sort((x, y) =>
                {
                    int result = x.Source.Ordinal - y.Source.Ordinal;
                    if (result == 0 && x.IsConditionalSuccessor != y.IsConditionalSuccessor)
                    {
                        if (x.IsConditionalSuccessor)
                        {
                            result = -1;
                        }
                        else
                        {
                            result = 1;
                        }
                    }

                    return result;
                });

                return branches.ToImmutableAndFree();

                void addBranches(BasicBlockBuilder predecessorBlockBuilder)
                {
                    BasicBlock predecessor = blocks[predecessorBlockBuilder.Ordinal];
                    Debug.Assert(predecessor.FallThroughSuccessor != null);
                    if (predecessor.FallThroughSuccessor.Destination == block)
                    {
                        branches.Add(predecessor.FallThroughSuccessor);
                    }

                    if (predecessor.ConditionalSuccessor?.Destination == block)
                    {
                        branches.Add(predecessor.ConditionalSuccessor);
                    }
                }
            }

            public void Free()
            {
                Ordinal = -1;
                _statements?.Free();
                _statements = null;
                _predecessors?.Free();
                _predecessors = null;
                _predecessor1 = null;
                _predecessor2 = null;
            }

            internal struct Branch
            {
                public ControlFlowBranchSemantics Kind { get; set; }
                public BasicBlockBuilder? Destination { get; set; }
            }
        }
    }
}
