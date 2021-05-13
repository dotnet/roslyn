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
        private readonly ImmutableDictionary<object, IStateTable> _tables;

        internal static DriverStateTable Empty { get; } = new DriverStateTable(ImmutableDictionary<object, IStateTable>.Empty);

        private DriverStateTable(ImmutableDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        public sealed class Builder
        {
            private readonly ImmutableDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableDictionary.CreateBuilder<object, IStateTable>();

            private readonly ImmutableDictionary<object, object>.Builder _inputBuilder = ImmutableDictionary.CreateBuilder<object, object>();

            private readonly ArrayBuilder<ISyntaxInputNode> _syntaxNodes = ArrayBuilder<ISyntaxInputNode>.GetInstance();

            private readonly DriverStateTable _previousTable;

            private readonly CancellationToken _cancellationToken;

            public Builder(DriverStateTable previousTable)
            {
                _previousTable = previousTable;
            }

            public Builder(DriverStateTable previousTable, Compilation compilation, GeneratorDriverState driverState, ImmutableArray<ISyntaxInputNode> syntaxTransformNodes, ImmutableArray<(object, object)> otherInputs, CancellationToken cancellationToken = default)
            {
                _previousTable = previousTable;
                _cancellationToken = cancellationToken;

                _inputBuilder[SharedInputNodes.Compilation] = ImmutableArray.Create(compilation);
                _inputBuilder[SharedInputNodes.SyntaxTrees] = compilation.SyntaxTrees.ToImmutableArray();
                _inputBuilder[SharedInputNodes.AdditionalTexts] = driverState.AdditionalTexts;
                _inputBuilder[SharedInputNodes.AnalyzerConfigOptions] = ImmutableArray.Create(driverState.OptionsProvider);
                _inputBuilder[SharedInputNodes.ParseOptions] = ImmutableArray.Create(driverState.ParseOptions);
                foreach ((var key, var value) in otherInputs)
                {
                    _inputBuilder[key] = value;
                }

                _syntaxNodes.AddRange(syntaxTransformNodes);
            }

            public ImmutableArray<T> GetInputValue<T>(InputNode<T> source)
            {
                return (ImmutableArray<T>)_inputBuilder[source];
            }

            public NodeStateTable<T> GetSyntaxInputTable<T>(SyntaxInputNode<T> syntaxTransform)
            {
                // when we don't have values for this node, we update all the syntax inputs at once
                if (!_tableBuilder.ContainsKey(syntaxTransform))
                {
                    var compilation = GetInputValue(SharedInputNodes.Compilation).First();

                    // get a builder for each input node
                    var builders = ArrayBuilder<ISyntaxInputBuilder>.GetInstance(_syntaxNodes.Count);
                    foreach (var node in _syntaxNodes)
                    {
                        var syntax = GetValueOrEmpty<SyntaxNode, object>(_inputBuilder, node);
                        var transform = GetValueOrEmpty<T, IStateTable>(_previousTable._tables, node);
                        builders.Add(node.GetBuilder(syntax, transform));
                    }

                    // update each tree for the builders, sharing the semantic model
                    foreach ((var tree, var state) in GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees))
                    {
                        var root = tree.GetRoot(_cancellationToken);
                        var model = state != EntryState.Removed ? compilation.GetSemanticModel(tree) : null;
                        foreach (var builder in builders)
                        {
                            builder.VisitTree(root, state, model);
                        }
                    }

                    // save the updated inputs
                    for (int i = 0; i < _syntaxNodes.Count; i++)
                    {
                        var tables = builders[i].ToImmutableAndFree();
                        _inputBuilder[_syntaxNodes[i]] = tables.nodeTable;
                        _tableBuilder[_syntaxNodes[i]] = tables.transformTable;
                    }
                    builders.Free();
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
                NodeStateTable<T> previousTable = GetValueOrEmpty<T, IStateTable>(_previousTable._tables, source);

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

            private NodeStateTable<T> GetValueOrEmpty<T, TValue>(IReadOnlyDictionary<object, TValue> lookup, object key) where TValue : notnull
            {
                if (lookup.TryGetValue(key, out var result))
                {
                    return (NodeStateTable<T>)(object)result;
                }
                return NodeStateTable<T>.Empty;
            }
        }
    }
}
