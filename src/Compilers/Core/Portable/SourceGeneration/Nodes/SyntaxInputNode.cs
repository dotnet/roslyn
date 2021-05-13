// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxInputNode
    {
        ISyntaxInputBuilder GetBuilder(DriverStateTable table);
    }

    internal interface ISyntaxInputBuilder
    {
        void VisitTree(SyntaxNode root, EntryState state, SemanticModel? model);

        void SaveStateAndFree(ImmutableDictionary<object, IStateTable>.Builder tables);
    }

    internal sealed class SyntaxInputNode<T> : IIncrementalGeneratorNode<T>, ISyntaxInputNode
    {
        private readonly Func<GeneratorSyntaxContext, T> _func;
        private readonly IEqualityComparer<T> _comparer;
        private readonly Func<SyntaxNode, bool> _filterFunc;

        internal SyntaxInputNode(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc, IEqualityComparer<T>? comparer = null)
        {
            _func = transformFunc;
            _filterFunc = filterFunc;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public object FilterKey { get; } = new object();

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            return (NodeStateTable<T>)graphState.GetSyntaxInputTable(this);
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxInputNode<T>(_filterFunc, _func, comparer);

        public ISyntaxInputBuilder GetBuilder(DriverStateTable table) => new Builder(this, table);

        private sealed class Builder : ISyntaxInputBuilder
        {
            private readonly SyntaxInputNode<T> _owner;

            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            private readonly NodeStateTable<T>.Builder _transformTable;

            public Builder(SyntaxInputNode<T> owner, DriverStateTable table)
            {
                _owner = owner;
                _filterTable = table.GetStateTableOrEmpty<SyntaxNode>(_owner.FilterKey).ToBuilder();
                _transformTable = table.GetStateTableOrEmpty<T>(_owner).ToBuilder();
            }

            public void SaveStateAndFree(ImmutableDictionary<object, IStateTable>.Builder tables)
            {
                tables[_owner.FilterKey] = _filterTable.ToImmutableAndFree();
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

                    // now, using the obtained nodes, run the transform on the regular node
                    foreach (var node in nodes)
                    {
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = ImmutableArray.Create(_owner._func(value));

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
