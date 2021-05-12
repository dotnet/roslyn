// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class DriverStateTable
    {
        // PROTOTYPE(source-generators): should we make a non generic node interface that we can use as the key
        //                               instead of just object?
        private readonly ImmutableDictionary<object, IStateTable> _tables;

        internal static DriverStateTable Empty { get; } = new DriverStateTable(ImmutableDictionary<object, IStateTable>.Empty);

        private DriverStateTable(ImmutableDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        internal NodeStateTable<T> GetStateTable<T>(IIncrementalGeneratorNode<T> input) => _tables.ContainsKey(input) ? (NodeStateTable<T>)_tables[input] : NodeStateTable<T>.Empty;

        internal DriverStateTable SetStateTable<T>(IIncrementalGeneratorNode<T> input, NodeStateTable<T> table) => new DriverStateTable(_tables.SetItem(input, table));

        public sealed class Builder
        {
            private readonly ImmutableDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableDictionary.CreateBuilder<object, IStateTable>();

            private readonly ImmutableDictionary<object, object>.Builder _inputBuilder = ImmutableDictionary.CreateBuilder<object, object>();

            private readonly ArrayBuilder<ISyntaxTransformNode> _syntaxNodes = ArrayBuilder<ISyntaxTransformNode>.GetInstance();

            private readonly DriverStateTable _previousTable;

            private readonly CancellationToken _cancellationToken;

            public Builder(DriverStateTable previousTable, CancellationToken cancellationToken = default)
            {
                _previousTable = previousTable;
                _cancellationToken = cancellationToken;
            }

            public void AddInput<T>(InputNode<T> source, T value)
            {
                _inputBuilder[source] = ImmutableArray.Create(value);
            }

            public void AddInput<T>(InputNode<T> source, IEnumerable<T> value)
            {
                _inputBuilder[source] = value.ToImmutableArray();
            }

            public void AddSyntaxNodes(ImmutableArray<ISyntaxTransformNode> nodes)
            {
                _syntaxNodes.AddRange(nodes);
            }

            public ImmutableArray<T> GetInputValue<T>(InputNode<T> source)
            {
                return (ImmutableArray<T>)_inputBuilder[source];
            }

            public NodeStateTable<T> GetSyntaxValue<T>(SyntaxTransformNode<T> syntaxTransform)
            {
                // if cached already, just return the cached value
                if (!_tableBuilder.ContainsKey(syntaxTransform))
                {
                    // else, we need to walk over the syntax trees
                    var compilation = GetInputValue(SharedInputNodes.Compilation).First();

                    var builders = ArrayBuilder<ISyntaxTransformBuilder>.GetInstance(_syntaxNodes.Count);
                    foreach (var node in _syntaxNodes)
                    {
                        builders.Add(node.GetBuilder(this._previousTable));
                    }

                    foreach ((var tree, var state) in GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees))
                    {
                        var root = tree.GetRoot(_cancellationToken);
                        var model = state != EntryState.Removed ? compilation.GetSemanticModel(tree) : null;
                        foreach (var builder in builders)
                        {
                            builder.VisitTree(root, state, model);
                        }
                    }

                    foreach (var builder in builders)
                    {
                        builder.SaveInputsAndFree(this._tableBuilder);
                    }
                }

                return (NodeStateTable<T>)_tableBuilder[syntaxTransform];
            }

            public NodeStateTable<T> GetLatestStateTableForNode<T>(IIncrementalGeneratorNode<T> source)
            {
                // if we've already evaluated a node during this build, we can just return the existing result
                if (_tableBuilder.ContainsKey(source))
                {
                    return (NodeStateTable<T>)_tableBuilder[source];
                }

                // get the previous table, if there was one for this node
                NodeStateTable<T> previousTable = _previousTable.GetStateTable(source);

                // request the node update its state based on the current driver table and store the new result
                var newTable = source.UpdateStateTable(this, previousTable, _cancellationToken);
                _tableBuilder[source] = newTable;
                return newTable;
            }

            public DriverStateTable ToImmutable()
            {
                // we can compact the tables at this point, as we'll no longer be using them to determine current state
                var keys = _tableBuilder.Keys.ToArray();
                foreach (var key in keys)
                {
                    _tableBuilder[key] = _tableBuilder[key].Compact();
                }

                return new DriverStateTable(_tableBuilder.ToImmutable());
            }
        }
    }
}
