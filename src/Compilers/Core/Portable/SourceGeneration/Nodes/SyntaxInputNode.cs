// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SyntaxInputNode<T> : IIncrementalGeneratorNode<T>, ISyntaxInputNode
    {
        private readonly Func<GeneratorSyntaxContext, T> _transformFunc;
        private readonly Func<SyntaxNode, bool> _filterFunc;
        private readonly IEqualityComparer<T> _comparer;
        private readonly object _filterKey = new object();

        internal SyntaxInputNode(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc, IEqualityComparer<T>? comparer = null)
        {
            _transformFunc = transformFunc;
            _filterFunc = filterFunc;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.GetSyntaxInputTable(this);
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxInputNode<T>(_filterFunc, _transformFunc, comparer);

        public ISyntaxInputBuilder GetBuilder(DriverStateTable table) => new Builder(this, table);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxInputNode<T> _owner;

            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            private readonly NodeStateTable<T>.Builder _transformTable;

            public Builder(SyntaxInputNode<T> owner, DriverStateTable table)
            {
                _owner = owner;
                _filterTable = table.GetStateTableOrEmpty<SyntaxNode>(_owner._filterKey).ToBuilder();
                _transformTable = table.GetStateTableOrEmpty<T>(_owner).ToBuilder();
            }

            public void SaveStateAndFree(ImmutableDictionary<object, IStateTable>.Builder tables)
            {
                tables[_owner._filterKey] = _filterTable.ToImmutableAndFree();
                tables[_owner] = _transformTable.ToImmutableAndFree();
            }

            public void VisitTree(SyntaxNode root, EntryState state, SemanticModel? model)
            {
                if (state == EntryState.Removed)
                {
                    // mark both syntax *and* transform nodes removed
                    _filterTable.RemoveEntries();
                    _transformTable.RemoveEntries();
                }
                else
                {
                    Debug.Assert(model is object);

                    // get the syntax nodes from cache, or a syntax walk using the filter
                    ImmutableArray<SyntaxNode> nodes;
                    if (state == EntryState.Cached && _filterTable.TryUseCachedEntries())
                    {
                        nodes = _filterTable.GetLastEntries();
                    }
                    else
                    {
                        nodes = IncrementalGeneratorSyntaxWalker.GetFilteredNodes(root, _owner._filterFunc);
                        _filterTable.AddEntries(nodes, EntryState.Added);
                    }

                    // now, using the obtained syntax nodes, run the transform
                    foreach (var node in nodes)
                    {
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = ImmutableArray.Create(_owner._transformFunc(value));

                        if (state == EntryState.Added || !_transformTable.TryModifyEntries(transformed, _owner._comparer))
                        {
                            _transformTable.AddEntries(transformed, EntryState.Added);
                        }
                    }
                }
            }
        }
    }
}
