// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AbstractFlowPass<TLocalState, TLocalFunctionState>
    {
        internal sealed class PendingBranchesCollection
        {
            private ArrayBuilder<PendingBranch> _unlabeledBranches;
            private int _nextLabelIndex;
            private PooledDictionary<LabelSymbol, (int Index, ArrayBuilder<PendingBranch> Branches)>? _labeledBranches;

            internal PendingBranchesCollection()
            {
                _unlabeledBranches = ArrayBuilder<PendingBranch>.GetInstance();
            }

            internal void Free()
            {
                _unlabeledBranches.Free();
                _unlabeledBranches = null!;
                FreeBranchesToLabel();
            }

            internal void Clear()
            {
                _unlabeledBranches.Clear();
                FreeBranchesToLabel();
            }

            private void FreeBranchesToLabel()
            {
                if (_labeledBranches is { })
                {
                    foreach (var pair in _labeledBranches.Values)
                    {
                        pair.Branches.Free();
                    }
                    _labeledBranches.Free();
                    _labeledBranches = null;
                }
            }

            internal ImmutableArray<PendingBranch> ToImmutable()
            {
                return _labeledBranches is null ?
                    _unlabeledBranches.ToImmutable() :
                    ImmutableArray.CreateRange(AsEnumerable());
            }

            internal ArrayBuilder<PendingBranch>? GetAndRemoveBranches(LabelSymbol? label)
            {
                ArrayBuilder<PendingBranch>? result;
                if (label is null)
                {
                    if (_unlabeledBranches.Count == 0)
                    {
                        result = null;
                    }
                    else
                    {
                        result = _unlabeledBranches;
                        _unlabeledBranches = ArrayBuilder<PendingBranch>.GetInstance();
                    }
                }
                else if (_labeledBranches is { } && _labeledBranches.TryGetValue(label, out var pair))
                {
                    result = pair.Branches;
                    _labeledBranches.Remove(label);
                }
                else
                {
                    result = null;
                }
                return result;
            }

            internal void Add(PendingBranch branch)
            {
                var label = branch.Label;
                if (label is null)
                {
                    _unlabeledBranches.Add(branch);
                }
                else
                {
                    var branches = GetOrAddBranchesToLabel(label);
                    branches.Add(branch);
                }
            }

            internal void AddRange(PendingBranchesCollection collection)
            {
                _unlabeledBranches.AddRange(collection._unlabeledBranches);
                if (collection._labeledBranches is { })
                {
                    foreach (var pair in collection._labeledBranches)
                    {
                        var branches = GetOrAddBranchesToLabel(pair.Key);
                        branches.AddRange(pair.Value.Branches);
                    }
                }
            }

            private ArrayBuilder<PendingBranch> GetOrAddBranchesToLabel(LabelSymbol label)
            {
                if (_labeledBranches is null)
                {
                    _labeledBranches = PooledDictionary<LabelSymbol, (int Index, ArrayBuilder<PendingBranch> Branches)>.GetInstance();
                }
                if (_labeledBranches.TryGetValue(label, out var pair))
                {
                    return pair.Branches;
                }
                else
                {
                    var branches = ArrayBuilder<PendingBranch>.GetInstance();
                    _labeledBranches.Add(label, (_nextLabelIndex++, branches));
                    return branches;
                }
            }

            internal IEnumerable<PendingBranch> AsEnumerable()
            {
                return _labeledBranches is null ?
                    _unlabeledBranches :
                    asEnumerableCore();

                IEnumerable<PendingBranch> asEnumerableCore()
                {
                    // Short _labeledBranches by label index.
                    var labeledBranches = ArrayBuilder<(int Index, ArrayBuilder<PendingBranch> Branches)>.GetInstance();
                    labeledBranches.AddRange(_labeledBranches.Values);
                    labeledBranches.Sort((x, y) => x.Index - y.Index);

                    foreach (var branch in _unlabeledBranches)
                    {
                        yield return branch;
                    }

                    foreach (var pair in labeledBranches)
                    {
                        foreach (var branch in pair.Branches)
                        {
                            yield return branch;
                        }
                    }

                    labeledBranches.Free();
                }
            }
        }
    }
}
