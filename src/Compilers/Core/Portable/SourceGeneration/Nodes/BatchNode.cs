// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class BatchNode<TInput> : IIncrementalGeneratorNode<ImmutableArray<TInput>>
    {
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;
        private readonly IEqualityComparer<ImmutableArray<TInput>> _comparer;
        private readonly string? _name;

        public BatchNode(IIncrementalGeneratorNode<TInput> sourceNode, IEqualityComparer<ImmutableArray<TInput>>? comparer = null, string? name = null)
        {
            _sourceNode = sourceNode;
            _comparer = comparer ?? EqualityComparer<ImmutableArray<TInput>>.Default;
            _name = name;
        }

        public IIncrementalGeneratorNode<ImmutableArray<TInput>> WithComparer(IEqualityComparer<ImmutableArray<TInput>> comparer) => new BatchNode<TInput>(_sourceNode, comparer, _name);

        public IIncrementalGeneratorNode<ImmutableArray<TInput>> WithTrackingName(string name) => new BatchNode<TInput>(_sourceNode, _comparer, name);

        public NodeStateTable<ImmutableArray<TInput>> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<ImmutableArray<TInput>> previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);

            // Semantics of a batch transform:
            // Batches will always exist (a batch of the empty table is still [])
            // There is only ever one input, the batch of the upstream table
            // - Output is cached when upstream is all cached
            // - Added when the previous table was empty
            // - Modified otherwise

            // update the table
            var newTable = builder.CreateTableBuilder(previousTable, _name);

            // If this execution is tracking steps, then the source table should have also tracked steps or be the empty table.
            Debug.Assert(!newTable.TrackIncrementalSteps || (sourceTable.HasTrackedSteps || sourceTable.IsEmpty));

            var stopwatch = SharedStopwatch.StartNew();

            var sourceValuesBuilder = ArrayBuilder<TInput>.GetInstance();
            var sourceInputsBuilder = newTable.TrackIncrementalSteps ? ArrayBuilder<(IncrementalGeneratorRunStep InputStep, int OutputIndex)>.GetInstance() : null;

            foreach (var entry in sourceTable)
            {
                // At this point, we can remove any 'Removed' items and ensure they're not in our list of states.
                if (entry.State != EntryState.Removed)
                    sourceValuesBuilder.Add(entry.Item);

                // However, regardless of if the entry was removed or not, we still keep track of its step information
                // so we can accurately report how long it took and what actually happened (for testing validation).
                sourceInputsBuilder?.Add((entry.Step!, entry.OutputIndex));
            }

            var sourceValues = sourceValuesBuilder.ToImmutableAndFree();
            var sourceInputs = newTable.TrackIncrementalSteps ? sourceInputsBuilder!.ToImmutableAndFree() : default;

            if (previousTable.IsEmpty)
            {
                newTable.AddEntry(sourceValues, EntryState.Added, stopwatch.Elapsed, sourceInputs, EntryState.Added);
            }
            else if (!sourceTable.IsCached || !newTable.TryUseCachedEntries(stopwatch.Elapsed, sourceInputs))
            {
                if (!newTable.TryModifyEntry(sourceValues, _comparer, stopwatch.Elapsed, sourceInputs, EntryState.Modified))
                {
                    newTable.AddEntry(sourceValues, EntryState.Added, stopwatch.Elapsed, sourceInputs, EntryState.Added);
                }
            }

            return newTable.ToImmutableAndFree();
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
