// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

        internal static DriverStateTable Empty { get; } = new DriverStateTable(StateTableStore.Empty);

        private DriverStateTable(StateTableStore tables)
        {
            _tables = tables;
        }

        public sealed class Builder
        {
            private readonly StateTableStore.Builder _stateTableBuilder = new StateTableStore.Builder();
            private readonly DriverStateTable _previousTable;
            private readonly ImmutableArray<SyntaxInputNode> _syntaxInputNodes;
            private readonly CancellationToken _cancellationToken;
            private readonly Compilation _initialCompilation;
            private Compilation? _compilation;
            private SyntaxStore.Builder? _syntaxStore;

            internal GeneratorDriverState DriverState { get; }

            internal bool IsCompilationAvailable => _compilation is not null;

            public Compilation Compilation
            {
                get
                {
                    Debug.Assert(_compilation is not null, "Compilation should only be read after the pre-compilation phase has completed; if this fires a driver-internal caller is reading it too early.");
                    return _compilation;
                }
            }

            /// <summary>
            /// The compilation options from the user-supplied compilation. Available in all
            /// phases, including pre-compilation, because options are unaffected by source
            /// generation.
            /// </summary>
            internal CompilationOptions InitialCompilationOptions => _initialCompilation.Options;

            /// <summary>
            /// The metadata references from the user-supplied compilation. Available in all
            /// phases, including pre-compilation, because references are unaffected by source
            /// generation.
            /// </summary>
            internal ImmutableArray<MetadataReference> InitialMetadataReferences => _initialCompilation.ExternalReferences;

            internal SyntaxStore.Builder SyntaxStore
            {
                get
                {
                    Debug.Assert(_syntaxStore is not null, "SyntaxStore should only be read after the pre-compilation phase has completed; if this fires a driver-internal caller is reading it too early.");
                    return _syntaxStore;
                }
            }

            public Builder(GeneratorDriverState driverState, Compilation initialCompilation, ImmutableArray<SyntaxInputNode> syntaxInputNodes, CancellationToken cancellationToken = default)
            {
                DriverState = driverState;
                _previousTable = driverState.StateTable;
                _initialCompilation = initialCompilation;
                _syntaxInputNodes = syntaxInputNodes;
                _cancellationToken = cancellationToken;
            }

            public void SetCompilation(Compilation compilation)
            {
                Debug.Assert(_compilation is null, "SetCompilation should only be called once.");
                _compilation = compilation;
                _syntaxStore = DriverState.SyntaxStore.ToBuilder(compilation, _syntaxInputNodes, DriverState.TrackIncrementalSteps, _cancellationToken);
            }

            public NodeStateTable<T> GetLatestStateTableForNode<T>(IIncrementalGeneratorNode<T> source)
            {
                // if we've already evaluated a node during this build, we can just return the existing result
                if (_stateTableBuilder.TryGetTable(source, out var table))
                {
                    return (NodeStateTable<T>)table;
                }

                // get the previous table, if there was one for this node
                NodeStateTable<T>? previousTable = _previousTable._tables.GetStateTable<T>(source);

                // request the node update its state based on the current driver table and store the new result
                var newTable = source.UpdateStateTable(this, previousTable, _cancellationToken);
                _stateTableBuilder.SetTable(source, newTable);
                return newTable;
            }

            public NodeStateTable<T>.Builder CreateTableBuilder<T>(
                NodeStateTable<T>? previousTable, string? stepName, IEqualityComparer<T>? equalityComparer, int? tableCapacity = null)
            {
                previousTable ??= NodeStateTable<T>.Empty;
                return previousTable.ToBuilder(stepName, DriverState.TrackIncrementalSteps, equalityComparer, tableCapacity);
            }

            public DriverStateTable ToImmutable()
            {
                return new DriverStateTable(_stateTableBuilder.ToImmutable());
            }
        }
    }
}
