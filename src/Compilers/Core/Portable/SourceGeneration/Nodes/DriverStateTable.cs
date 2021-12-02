// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class DriverStateTable
    {
        private readonly ImmutableSegmentedDictionary<object, IStateTable> _tables;

        internal static DriverStateTable Empty { get; } = new DriverStateTable(ImmutableSegmentedDictionary<object, IStateTable>.Empty);

        private DriverStateTable(ImmutableSegmentedDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        public NodeStateTable<T> GetStateTableOrEmpty<T>(object input)
        {
            if (_tables.TryGetValue(input, out var result))
            {
                return (NodeStateTable<T>)result;
            }
            return NodeStateTable<T>.Empty;
        }

        public sealed class Builder
        {
            private readonly ImmutableSegmentedDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableSegmentedDictionary.CreateBuilder<object, IStateTable>();
            private readonly ImmutableArray<ISyntaxInputNode> _syntaxInputNodes;
            private readonly ImmutableDictionary<ISyntaxInputNode, Exception>.Builder _syntaxExceptions = ImmutableDictionary.CreateBuilder<ISyntaxInputNode, Exception>();
            private readonly DriverStateTable _previousTable;
            private readonly CancellationToken _cancellationToken;

            internal GeneratorDriverState DriverState { get; }

            public Compilation Compilation { get; }

            public Builder(Compilation compilation, GeneratorDriverState driverState, ImmutableArray<ISyntaxInputNode> syntaxInputNodes, CancellationToken cancellationToken = default)
            {
                Compilation = compilation;
                DriverState = driverState;
                _previousTable = driverState.StateTable;
                _syntaxInputNodes = syntaxInputNodes;
                _cancellationToken = cancellationToken;
            }

            public IStateTable GetSyntaxInputTable(ISyntaxInputNode syntaxInputNode)
            {
                Debug.Assert(_syntaxInputNodes.Contains(syntaxInputNode));

                // when we don't have a value for this node, we update all the syntax inputs at once
                if (!_tableBuilder.ContainsKey(syntaxInputNode))
                {
                    // CONSIDER: when the compilation is the same as previous, the syntax trees must also be the same.
                    // if we have a previous state table for a node, we can just short circuit knowing that it is up to date
                    var compilationIsCached = GetLatestStateTableForNode(SharedInputNodes.Compilation).IsCached;

                    // get a builder for each input node
                    var builders = ArrayBuilder<ISyntaxInputBuilder>.GetInstance(_syntaxInputNodes.Length);
                    foreach (var node in _syntaxInputNodes)
                    {
                        if (compilationIsCached && _previousTable._tables.TryGetValue(node, out var previousStateTable))
                        {
                            _tableBuilder.Add(node, previousStateTable);
                        }
                        else
                        {
                            builders.Add(node.GetBuilder(_previousTable));
                        }
                    }

                    if (builders.Count == 0)
                    {
                        // bring over the previously cached syntax tree inputs
                        _tableBuilder[SharedInputNodes.SyntaxTrees] = _previousTable._tables[SharedInputNodes.SyntaxTrees];
                    }
                    else
                    {
                        // update each tree for the builders, sharing the semantic model
                        foreach ((var tree, var state) in GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees))
                        {
                            var root = new Lazy<SyntaxNode>(() => tree.GetRoot(_cancellationToken));
                            var model = state != EntryState.Removed ? Compilation.GetSemanticModel(tree) : null;
                            for (int i = 0; i < builders.Count; i++)
                            {
                                try
                                {
                                    _cancellationToken.ThrowIfCancellationRequested();
                                    builders[i].VisitTree(root, state, model, _cancellationToken);
                                }
                                catch (UserFunctionException ufe)
                                {
                                    // we're evaluating this node ahead of time, so we can't just throw the exception
                                    // instead we'll hold onto it, and throw the exception when a downstream node actually
                                    // attempts to read the value
                                    _syntaxExceptions[builders[i].SyntaxInputNode] = ufe;
                                    builders.RemoveAt(i);
                                    i--;
                                }
                            }
                        }

                        // save the updated inputs
                        foreach (var builder in builders)
                        {
                            builder.SaveStateAndFree(_tableBuilder);
                            Debug.Assert(_tableBuilder.ContainsKey(builder.SyntaxInputNode));
                        }
                    }
                    builders.Free();
                }

                // if we don't have an entry for this node, it must have thrown an exception
                if (!_tableBuilder.ContainsKey(syntaxInputNode))
                {
                    throw _syntaxExceptions[syntaxInputNode];
                }

                return _tableBuilder[syntaxInputNode];
            }

            public NodeStateTable<T> GetLatestStateTableForNode<T>(IIncrementalGeneratorNode<T> source)
            {
                // if we've already evaluated a node during this build, we can just return the existing result
                if (_tableBuilder.ContainsKey(source))
                {
                    return (NodeStateTable<T>)_tableBuilder[source];
                }

                // get the previous table, if there was one for this node
                NodeStateTable<T> previousTable = _previousTable.GetStateTableOrEmpty<T>(source);

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
                    _tableBuilder[key] = _tableBuilder[key].AsCached();
                }

                return new DriverStateTable(_tableBuilder.ToImmutable());
            }
        }
    }
}
