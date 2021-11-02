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
        protected sealed class PendingBranchesCollection : IEnumerable<PendingBranch>
        {
            private ArrayBuilder<PendingBranch> _branches;
            private PooledDictionary<LabelSymbol, ArrayBuilder<PendingBranch>>? _branchesToLabel;

            internal PendingBranchesCollection()
            {
                _branches = ArrayBuilder<PendingBranch>.GetInstance();
            }

            internal void Free()
            {
                _branches.Free();
                _branches = null!;
                FreeBranchesToLabel();
            }

            internal void Clear()
            {
                _branches.Clear();
                FreeBranchesToLabel();
            }

            private void FreeBranchesToLabel()
            {
                if (_branchesToLabel is { })
                {
                    foreach (var branches in _branchesToLabel.Values)
                    {
                        branches.Free();
                    }
                    _branchesToLabel.Free();
                    _branchesToLabel = null;
                }
            }

            internal ImmutableArray<PendingBranch> ToImmutable()
            {
                return _branchesToLabel is null ?
                    _branches.ToImmutable() :
                    ImmutableArray.CreateRange(this);
            }

            internal ArrayBuilder<PendingBranch> GetAndRemoveBranches(LabelSymbol? label)
            {
                ArrayBuilder<PendingBranch>? result;
                if (label is null)
                {
                    result = _branches;
                    _branches = ArrayBuilder<PendingBranch>.GetInstance();
                }
                else if (_branchesToLabel is { } && _branchesToLabel.TryGetValue(label, out result))
                {
                    _branchesToLabel.Remove(label);
                }
                else
                {
                    result = ArrayBuilder<PendingBranch>.GetInstance();
                }
                return result;
            }

            internal void Add(PendingBranch branch)
            {
                var label = branch.Label;
                if (label is null)
                {
                    _branches.Add(branch);
                }
                else
                {
                    var branches = GetOrAddBranchesToLabel(label);
                    branches.Add(branch);
                }
            }

            internal void AddRange(PendingBranchesCollection collection)
            {
                _branches.AddRange(collection._branches);
                if (collection._branchesToLabel is { })
                {
                    foreach (var pair in collection._branchesToLabel)
                    {
                        var branches = GetOrAddBranchesToLabel(pair.Key);
                        branches.AddRange(pair.Value);
                    }
                }
            }

            private ArrayBuilder<PendingBranch> GetOrAddBranchesToLabel(LabelSymbol label)
            {
                if (_branchesToLabel is null)
                {
                    _branchesToLabel = PooledDictionary<LabelSymbol, ArrayBuilder<PendingBranch>>.GetInstance();
                }
                if (!_branchesToLabel.TryGetValue(label, out var branches))
                {
                    branches = ArrayBuilder<PendingBranch>.GetInstance();
                    _branchesToLabel.Add(label, branches);
                }
                return branches;
            }

            public IEnumerator<PendingBranch> GetEnumerator()
            {
                return _branchesToLabel is null ?
                    ((IEnumerable<PendingBranch>)_branches).GetEnumerator() :
                    getEnumeratorCore();

                IEnumerator<PendingBranch> getEnumeratorCore()
                {
                    foreach (var branch in _branches)
                    {
                        yield return branch;
                    }
                    foreach (var branches in _branchesToLabel.Values)
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
