// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AbstractFlowPass<TLocalState, TLocalFunctionState>
    {
        internal sealed class PendingBranchesCollection : IEnumerable<PendingBranch>
        {
            private ArrayBuilder<PendingBranch> _unlabeledBranches;
            private PooledDictionary<LabelSymbol, ArrayBuilder<PendingBranch>>? _labeledBranches;

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
                    foreach (var branches in _labeledBranches.Values)
                    {
                        branches.Free();
                    }
                    _labeledBranches.Free();
                    _labeledBranches = null;
                }
            }

            internal ImmutableArray<PendingBranch> ToImmutable()
            {
                return _labeledBranches is null ?
                    _unlabeledBranches.ToImmutable() :
                    ImmutableArray.CreateRange(this);
            }

            internal ArrayBuilder<PendingBranch>? GetAndRemoveBranches(LabelSymbol? label)
            {
                ArrayBuilder<PendingBranch>? result;
                if (label is null)
                {
                    result = _unlabeledBranches;
                    _unlabeledBranches = ArrayBuilder<PendingBranch>.GetInstance();
                }
                else if (_labeledBranches is { } && _labeledBranches.TryGetValue(label, out result))
                {
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
                        branches.AddRange(pair.Value);
                    }
                }
            }

            private ArrayBuilder<PendingBranch> GetOrAddBranchesToLabel(LabelSymbol label)
            {
                if (_labeledBranches is null)
                {
                    _labeledBranches = PooledDictionary<LabelSymbol, ArrayBuilder<PendingBranch>>.GetInstance();
                }
                if (!_labeledBranches.TryGetValue(label, out var branches))
                {
                    branches = ArrayBuilder<PendingBranch>.GetInstance();
                    _labeledBranches.Add(label, branches);
                }
                return branches;
            }

            public IEnumerator<PendingBranch> GetEnumerator()
            {
                return _labeledBranches is null ?
                    ((IEnumerable<PendingBranch>)_unlabeledBranches).GetEnumerator() :
                    getEnumeratorCore();

                IEnumerator<PendingBranch> getEnumeratorCore()
                {
                    foreach (var branch in _unlabeledBranches)
                    {
                        yield return branch;
                    }
                    foreach (var branches in _labeledBranches.Values)
                    {
                        foreach (var branch in branches)
                        {
                            yield return branch;
                        }
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
