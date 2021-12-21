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
        private readonly StateTableStore _tables;

        private readonly SyntaxStore _syntaxStore;

        internal static DriverStateTable Empty { get; } = new DriverStateTable(StateTableStore.Empty, SyntaxStore.Empty);

        private DriverStateTable(StateTableStore tables, SyntaxStore syntaxStore)
        {
            _tables = tables;
            _syntaxStore = syntaxStore;
        }

        public sealed class Builder
        {
            private readonly StateTableStore.Builder _stateTableBuilder = new StateTableStore.Builder();
            private readonly DriverStateTable _previousTable;
            private readonly CancellationToken _cancellationToken;
            private readonly SyntaxStore.Builder _syntaxStore;

            internal GeneratorDriverState DriverState { get; }

            public Compilation Compilation { get; }

            public Builder(Compilation compilation, GeneratorDriverState driverState, SyntaxStore.Builder syntaxStore, CancellationToken cancellationToken = default)
            {
                Compilation = compilation;
                DriverState = driverState;
                _previousTable = driverState.StateTable;
                _cancellationToken = cancellationToken;
                _syntaxStore = syntaxStore;
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
                if (_stateTableBuilder.TryGetTable(source, out var table))
                {
                    return (NodeStateTable<T>)table;
                }

                // get the previous table, if there was one for this node
                NodeStateTable<T> previousTable = _previousTable._tables.GetStateTableOrEmpty<T>(source);

                // request the node update its state based on the current driver table and store the new result
                var newTable = source.UpdateStateTable(this, previousTable, _cancellationToken);
                _stateTableBuilder.SetTable(source, newTable);
                return newTable;
            }

            public NodeStateTable<T>.Builder CreateTableBuilder<T>(NodeStateTable<T> previousTable, string? stepName)
            {
                return previousTable.ToBuilder(stepName, DriverState.TrackIncrementalSteps);
            }

            public DriverStateTable ToImmutable()
            {
                return new DriverStateTable(_stateTableBuilder.ToImmutable(), _syntaxStore.ToImmutable());
            }
        }
    }
}
