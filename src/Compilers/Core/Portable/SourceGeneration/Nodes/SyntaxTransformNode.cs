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
    internal interface ISyntaxTransformNode
    {
        ISyntaxTransformBuilder GetBuilder(DriverStateTable previousTable);
    }

    internal interface ISyntaxTransformBuilder
    {
        bool Filter(SyntaxNode node);

        void AddFilterFromPreviousTable(SemanticModel? model, EntryState state);

        void AddFilterEntries(ImmutableArray<SyntaxNode> nodes, SemanticModel model);

        void AddInputs(DriverStateTable.Builder builder);
    }

    internal sealed class SyntaxTransformNode<T> : IIncrementalGeneratorNode<T>, ISyntaxTransformNode
    {
        private readonly Func<GeneratorSyntaxContext, T> _func;
        private readonly IEqualityComparer<T> _comparer;
        private readonly Func<SyntaxNode, bool> _filterFunc;
        private readonly InputNode<SyntaxNode> _filterNode = new InputNode<SyntaxNode>();

        internal SyntaxTransformNode(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc, IEqualityComparer<T>? comparer = null)
        {
            _func = transformFunc;
            _filterFunc = filterFunc;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        // this is an input node, so we don't perform any updates
        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken) => previousTable;

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new SyntaxTransformNode<T>(_filterFunc, _func, comparer);

        public ISyntaxTransformBuilder GetBuilder(DriverStateTable previousStateTable) => new Builder(this, previousStateTable.GetStateTable(this._filterNode), previousStateTable.GetStateTable(this));

        private sealed class Builder : ISyntaxTransformBuilder
        {
            private readonly SyntaxTransformNode<T> _owner;

            private readonly NodeStateTable<T> _previousTransformTable;
            private readonly NodeStateTable<T>.Builder _transformTable;

            private readonly NodeStateTable<SyntaxNode> _previousFilterTable;
            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            public Builder(SyntaxTransformNode<T> owner, NodeStateTable<SyntaxNode> previousFilter, NodeStateTable<T> previousTransform)
            {
                _owner = owner;

                _previousFilterTable = previousFilter;
                _filterTable = new NodeStateTable<SyntaxNode>.Builder();

                _previousTransformTable = previousTransform;
                _transformTable = new NodeStateTable<T>.Builder();
            }

            public bool Filter(SyntaxNode syntaxNode) => _owner._filterFunc(syntaxNode);

            public void AddFilterFromPreviousTable(SemanticModel? model, EntryState state)
            {
                Debug.Assert(model is object || state == EntryState.Removed);
                var nodes = _filterTable.AddEntriesFromPreviousTable(_previousFilterTable, state);
                UpdateTransformTable(nodes, model, state);
            }

            public void AddFilterEntries(ImmutableArray<SyntaxNode> nodes, SemanticModel model)
            {
                _filterTable.AddEntries(nodes, EntryState.Added);
                UpdateTransformTable(nodes, model, EntryState.Added);
            }

            private void UpdateTransformTable(ImmutableArray<SyntaxNode> nodes, SemanticModel? model, EntryState state)
            {
                foreach (var node in nodes)
                {
                    if (state == EntryState.Removed)
                    {
                        _transformTable.AddEntriesFromPreviousTable(_previousTransformTable, EntryState.Removed);
                    }
                    else
                    {
                        Debug.Assert(model is object);
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = ImmutableArray.Create(_owner._func(value));
                        if (_previousTransformTable.IsEmpty || state == EntryState.Added)
                        {
                            _transformTable.AddEntries(transformed, EntryState.Added);
                        }
                        else
                        {
                            _transformTable.ModifyEntriesFromPreviousTable(_previousTransformTable, transformed, _owner._comparer);
                        }
                    }
                }
            }

            public void AddInputs(DriverStateTable.Builder builder)
            {
                builder.SetTable(_owner._filterNode, _filterTable.ToImmutableAndFree());
                builder.SetTable(_owner, _transformTable.ToImmutableAndFree());
            }
        }
    }
}
