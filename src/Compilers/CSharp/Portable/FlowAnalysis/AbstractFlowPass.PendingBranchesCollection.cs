// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AbstractFlowPass<TLocalState, TLocalFunctionState>
    {
        internal sealed class PendingBranchesCollection
        {
            private ArrayBuilder<PendingBranch> _unlabeledBranches;
            private PooledDictionary<int, ArrayBuilder<PendingBranch>>? _labeledBranches;

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
                    ImmutableArray.CreateRange(AsEnumerable());
            }

            internal ArrayBuilder<PendingBranch>? GetAndRemoveBranches(int? labelId)
            {
                ArrayBuilder<PendingBranch>? result;
                if (labelId is null)
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
                else if (_labeledBranches is { } && _labeledBranches.TryGetValue(labelId.GetValueOrDefault(), out result))
                {
                    _labeledBranches.Remove(labelId.GetValueOrDefault());
                }
                else
                {
                    result = null;
                }
                return result;
            }

            internal void Add(int? labelId, PendingBranch branch)
            {
                if (labelId is null)
                {
                    _unlabeledBranches.Add(branch);
                }
                else
                {
                    var branches = GetOrAddBranchesToLabel(labelId.GetValueOrDefault());
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

            private ArrayBuilder<PendingBranch> GetOrAddBranchesToLabel(int labelId)
            {
                if (_labeledBranches is null)
                {
                    _labeledBranches = PooledDictionary<int, ArrayBuilder<PendingBranch>>.GetInstance();
                }
                if (!_labeledBranches.TryGetValue(labelId, out var branches))
                {
                    branches = ArrayBuilder<PendingBranch>.GetInstance();
                    _labeledBranches.Add(labelId, branches);
                }
                return branches;
            }

            internal IEnumerable<PendingBranch> AsEnumerable()
            {
                return _labeledBranches is null ?
                    _unlabeledBranches :
                    asEnumerableCore();

                IEnumerable<PendingBranch> asEnumerableCore()
                {
                    var labels = ArrayBuilder<int>.GetInstance();
                    labels.AddRange(_labeledBranches.Keys);
                    labels.Sort((x, y) => x - y);
                    foreach (var branch in _unlabeledBranches)
                    {
                        yield return branch;
                    }
                    foreach (var label in labels)
                    {
                        var branches = _labeledBranches[label];
                        foreach (var branch in branches)
                        {
                            yield return branch;
                        }
                    }
                    labels.Free();
                }
            }
        }
    }
}
