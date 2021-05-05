// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxTransformNode
    {
        ISyntaxTransformBuilder GetBuilder(DriverStateTable previousStateTable);
    }

    internal interface ISyntaxTransformBuilder
    {
        bool Filter(SyntaxNode node);

        void AddFilterFromPreviousTable(SemanticModel model, EntryState state);

        void AddFilterEntries(ImmutableArray<SyntaxNode> nodes, SemanticModel model);

        void SetInputState(DriverStateTable.Builder builder);
    }

    internal sealed class SyntaxTransformNode<T> : InputNode<T>, ISyntaxTransformNode
    {
        private readonly Func<GeneratorSyntaxContext, ImmutableArray<T>> _func;

        private readonly Func<SyntaxNode, bool> _filterFunc;

        private readonly InputNode<SyntaxNode> _filterNode = new InputNode<SyntaxNode>();

        internal SyntaxTransformNode(Func<SyntaxNode, bool> filterFunc, Func<GeneratorSyntaxContext, T> transformFunc)
        {
            _func = (t) => ImmutableArray.Create(transformFunc(t));
            _filterFunc = filterFunc;
        }

        public ISyntaxTransformBuilder GetBuilder(DriverStateTable previousStateTable) => new Builder(this, previousStateTable.GetStateTable(this), previousStateTable.GetStateTable(this._filterNode));

        internal sealed class Builder : ISyntaxTransformBuilder
        {
            private readonly SyntaxTransformNode<T> _owner;

            private readonly NodeStateTable<T> _previousTransformTable;
            private readonly NodeStateTable<T>.Builder _transformTable;

            private readonly NodeStateTable<SyntaxNode> _previousFilterTable;
            private readonly NodeStateTable<SyntaxNode>.Builder _filterTable;

            public Builder(SyntaxTransformNode<T> owner, NodeStateTable<T> previousTransform, NodeStateTable<SyntaxNode> previousFilter)
            {
                _owner = owner;

                _previousFilterTable = previousFilter;
                _filterTable = new NodeStateTable<SyntaxNode>.Builder();

                _previousTransformTable = previousTransform;
                _transformTable = new NodeStateTable<T>.Builder();
            }

            public bool Filter(SyntaxNode syntaxNode) => _owner._filterFunc(syntaxNode);

            public void AddFilterFromPreviousTable(SemanticModel model, EntryState state)
            {
                var nodes = _filterTable.AddEntriesFromPreviousTable(_previousFilterTable, state);
                UpdateTransformTable(nodes, model, state);
            }

            public void AddFilterEntries(ImmutableArray<SyntaxNode> nodes, SemanticModel model)
            {
                _filterTable.AddEntries(nodes, EntryState.Added);
                UpdateTransformTable(nodes, model, EntryState.Added);
            }

            private void UpdateTransformTable(ImmutableArray<SyntaxNode> nodes, SemanticModel model, EntryState state)
            {
                foreach (var node in nodes)
                {
                    if (state == EntryState.Removed)
                    {
                        _transformTable.AddEntriesFromPreviousTable(_previousTransformTable, EntryState.Removed);
                    }
                    else
                    {
                        var value = new GeneratorSyntaxContext(node, model);
                        var transformed = _owner._func(value);
                        if (_previousTransformTable.IsEmpty || state == EntryState.Added)
                        {
                            _transformTable.AddEntries(transformed, EntryState.Added);
                        }
                        else
                        {
                            // PROTOTYPE(source-generators): need to be able to set a comparer here
                            _transformTable.ModifyEntriesFromPreviousTable(_previousTransformTable, transformed, EqualityComparer<T>.Default);
                        }
                    }
                }
            }

            public void SetInputState(DriverStateTable.Builder builder)
            {
                builder.SetInputState(_owner._filterNode, _filterTable.ToImmutableAndFree());
                builder.SetInputState(_owner, _transformTable.ToImmutableAndFree());
            }
        }
    }
}
