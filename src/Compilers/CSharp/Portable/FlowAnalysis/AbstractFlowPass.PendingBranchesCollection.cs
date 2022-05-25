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
            private PooledDictionary<LabelSymbol, ArrayBuilder<PendingBranch>>? _labeledBranches;

            internal PendingBranchesCollection()
            {
                _unlabeledBranches = ArrayBuilder<PendingBranch>.GetInstance();
            }

            internal void Free()
            {
                _unlabeledBranches.Free();
                _unlabeledBranches = null!;
                FreeLabeledBranches();
            }

            internal void Clear()
            {
                _unlabeledBranches.Clear();
                FreeLabeledBranches();
            }

            private void FreeLabeledBranches()
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

            /// <summary>
            /// Returns the unordered collection of branches.
            /// </summary>
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
                    var branches = GetOrAddLabeledBranches(label);
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
                        var branches = GetOrAddLabeledBranches(pair.Key);
                        branches.AddRange(pair.Value);
                    }
                }
            }

            private ArrayBuilder<PendingBranch> GetOrAddLabeledBranches(LabelSymbol label)
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

            /// <summary>
            /// Returns the unordered collection of branches.
            /// </summary>
            internal IEnumerable<PendingBranch> AsEnumerable()
            {
                return _labeledBranches is null ?
                    _unlabeledBranches :
                    asEnumerableCore();

                IEnumerable<PendingBranch> asEnumerableCore()
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
        }
    }
}
