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
            private readonly DriverStateTable _previousTable;
            private readonly CancellationToken _cancellationToken;
            private readonly SyntaxStore.Builder _syntaxStore;

            internal GeneratorDriverState DriverState { get; }

            public Compilation Compilation { get; }

            public Builder(Compilation compilation, GeneratorDriverState driverState, ImmutableArray<ISyntaxInputNode> syntaxInputNodes, CancellationToken cancellationToken = default)
            {
                Compilation = compilation;
                DriverState = driverState;
                _previousTable = driverState.StateTable;
                _cancellationToken = cancellationToken;

                _syntaxStore = new SyntaxStore.Builder(Compilation, _tableBuilder, syntaxInputNodes, DriverState, _previousTable, _previousTable._tables, _cancellationToken);
            }

            public IStateTable GetSyntaxInputTable(ISyntaxInputNode syntaxInputNode)
            {
                var compilationIsCached = GetLatestStateTableForNode(SharedInputNodes.Compilation).IsCached;
                NodeStateTable<SyntaxTree> syntaxTreeState = GetLatestStateTableForNode(SharedInputNodes.SyntaxTrees);

                return _syntaxStore.GetSyntaxInputTable(syntaxInputNode, compilationIsCached, syntaxTreeState);
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

            public NodeStateTable<T>.Builder CreateTableBuilder<T>(NodeStateTable<T> previousTable, string? stepName)
            {
                return previousTable.ToBuilder(stepName, DriverState.TrackIncrementalSteps);
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
