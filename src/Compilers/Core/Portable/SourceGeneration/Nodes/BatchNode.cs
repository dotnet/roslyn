// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private (ImmutableArray<TInput>, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)>) GetValuesAndInputs(
            NodeStateTable<TInput> sourceTable,
            NodeStateTable<ImmutableArray<TInput>>? previousTable,
            NodeStateTable<ImmutableArray<TInput>>.Builder newTable)
        {
            // Do an initial pass to both get the steps, and determine how many entries we'll have.
            var sourceInputsBuilder = newTable.TrackIncrementalSteps ? ArrayBuilder<(IncrementalGeneratorRunStep InputStep, int OutputIndex)>.GetInstance() : null;

            var entryCount = 0;
            foreach (var entry in sourceTable)
            {
                // Always keep track of its step information, regardless of if the entry was removed or not, so we
                // can accurately report how long it took and what actually happened (for testing validation).
                sourceInputsBuilder?.Add((entry.Step!, entry.OutputIndex));

                if (entry.State != EntryState.Removed)
                    entryCount++;
            }

            var sourceInputs = sourceInputsBuilder != null ? sourceInputsBuilder.ToImmutableAndFree() : default;

            // First, see if we can reuse the entries from previousTable.
            // If not, produce the actual values we need from sourceTable.
            var result = tryReusePreviousTableValues(entryCount) ?? computeCurrentTableValues(entryCount);
            return (result, sourceInputs);

            ImmutableArray<TInput>? tryReusePreviousTableValues(int entryCount)
            {
                if (previousTable is null)
                    return null;

                if (previousTable.Count != 1)
                    return null;

                var previousItems = previousTable.Single().item;

                // If they don't have the same length, we clearly can't reuse them.
                if (previousItems.Length != entryCount)
                    return null;

                var indexInPrevious = 0;
                foreach (var entry in sourceTable)
                {
                    if (entry.State == EntryState.Removed)
                        continue;

                    // If the entries aren't the same, we can't reuse.
                    if (!EqualityComparer<TInput>.Default.Equals(entry.Item, previousItems[indexInPrevious]))
                        return null;

                    indexInPrevious++;
                }

                // We better have the exact same count as previousItems as we checked that above.
                Debug.Assert(indexInPrevious == previousItems.Length);

                // Looks good, we can reuse this.
                return previousItems;
            }

            ImmutableArray<TInput> computeCurrentTableValues(int entryCount)
            {
                // Important: we initialize with the exact capacity we need here so that we don't make a pointless
                // scratch array that may be very large and may cause GC churn when it cannot be returned to the pool.
                var builder = ArrayBuilder<TInput>.GetInstance(entryCount);
                foreach (var entry in sourceTable)
                {
                    if (entry.State == EntryState.Removed)
                        continue;

                    builder.Add(entry.Item);
                }

                Debug.Assert(builder.Count == entryCount);
                return builder.ToImmutableAndFree();
            }
        }

        public NodeStateTable<ImmutableArray<TInput>> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<ImmutableArray<TInput>>? previousTable, CancellationToken cancellationToken)
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
            var newTable = builder.CreateTableBuilder(previousTable, _name, _comparer);

            // If this execution is tracking steps, then the source table should have also tracked steps or be the empty table.
            Debug.Assert(!newTable.TrackIncrementalSteps || (sourceTable.HasTrackedSteps || sourceTable.IsEmpty));

            var stopwatch = SharedStopwatch.StartNew();

            var (sourceValues, sourceInputs) = GetValuesAndInputs(sourceTable, previousTable, newTable);

            if (previousTable is null || previousTable.IsEmpty)
            {
                newTable.AddEntry(sourceValues, EntryState.Added, stopwatch.Elapsed, sourceInputs, EntryState.Added);
            }
            else if (!sourceTable.IsCached ||
                GetTotalValueCount(previousTable) != sourceValues.Length ||
                !newTable.TryUseCachedEntries(stopwatch.Elapsed, sourceInputs))
            {
                if (!newTable.TryModifyEntry(sourceValues, _comparer, stopwatch.Elapsed, sourceInputs, EntryState.Modified))
                {
                    newTable.AddEntry(sourceValues, EntryState.Added, stopwatch.Elapsed, sourceInputs, EntryState.Added);
                }
            }

            return newTable.ToImmutableAndFree();

            static int GetTotalValueCount(NodeStateTable<ImmutableArray<TInput>> table)
            {
                var count = 0;
                foreach (var entry in table)
                {
                    count += entry.Item.Length;
                }
                return count;
            }
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
