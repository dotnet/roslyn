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
    /// <summary>
    /// Input nodes are the 'root' nodes in the graph, and get their values from the inputs of the driver state table
    /// </summary>
    /// <typeparam name="T">The type of the input</typeparam>
    internal sealed class InputNode<T> : IIncrementalGeneratorNode<T>
    {
        private static readonly string? s_tableType = typeof(T).FullName;

        private readonly Func<DriverStateTable.Builder, ImmutableArray<T>> _getInput;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T>? _comparer;
        private readonly ObjectPool<PooledHashSet<T>>? _hashSetPool;
        private readonly string? _name;

        public InputNode(Func<DriverStateTable.Builder, ImmutableArray<T>> getInput, IEqualityComparer<T>? inputComparer = null)
            : this(getInput, registerOutput: null, CreateHashSetPool(inputComparer), comparer: null)
        {
        }

        private InputNode(
            Func<DriverStateTable.Builder, ImmutableArray<T>> getInput,
            Action<IIncrementalGeneratorOutputNode>? registerOutput,
            ObjectPool<PooledHashSet<T>>? hashSetPool,
            IEqualityComparer<T>? comparer = null,
            string? name = null)
        {
            _getInput = getInput;
            _comparer = comparer;
            _registerOutput = registerOutput ?? (o => throw ExceptionUtilities.Unreachable());
            _hashSetPool = hashSetPool;
            _name = name;
        }

        private static ObjectPool<PooledHashSet<T>>? CreateHashSetPool(IEqualityComparer<T>? inputComparer)
            => inputComparer is null || inputComparer == EqualityComparer<T>.Default
                ? null
                : PooledHashSet<T>.CreatePool(inputComparer);

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T>? previousTable, CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            var inputItems = _getInput(graphState);
            TimeSpan elapsedTime = stopwatch.Elapsed;

            // create a mutable hashset of the new items we can check against
            var itemsSet = getPooledHashSet(inputItems.Length);
            foreach (var item in inputItems)
            {
                var added = itemsSet.Add(item);
                Debug.Assert(added);
            }

            var tableBuilder = graphState.CreateTableBuilder(previousTable, _name, _comparer);

            // We always have no inputs steps into an InputNode, but we track the difference between "no inputs" (empty collection) and "no step information" (default value)
            var noInputStepsStepInfo = tableBuilder.TrackIncrementalSteps ? ImmutableArray<(IncrementalGeneratorRunStep, int)>.Empty : default;

            if (previousTable is not null)
            {
                // When the item count is unchanged, instead of Remove+Add we can modify the removed item
                // with a newly added item instead. This keeps the table the same size and avoids unnecessary
                // downstream invalidation. The list of replacement items is computed lazily on first need
                // so that pure reorders and identical inputs skip the work entirely.
                var canReplace = inputItems.Length == previousTable.Count;
                ImmutableArray<T> replacements = default;
                int replacementIndex = 0;

                foreach (var (oldItem, _, _, _) in previousTable)
                {
                    if (itemsSet.Remove(oldItem))
                    {
                        // we're iterating the table, so know that it has entries
                        var usedCache = tableBuilder.TryUseCachedEntries(elapsedTime, noInputStepsStepInfo);
                        Debug.Assert(usedCache);
                    }
                    else if (canReplace)
                    {
                        if (replacements.IsDefault)
                            replacements = getNewInputItems(inputItems, previousTable);

                        // The old item was removed. Use the next pre-computed replacement to modify in place.
                        var replacementItem = replacements[replacementIndex++];
                        var removed = itemsSet.Remove(replacementItem);
                        Debug.Assert(removed);

                        var modified = tableBuilder.TryModifyEntry(replacementItem, elapsedTime, noInputStepsStepInfo, EntryState.Modified);
                        Debug.Assert(modified);
                    }
                    else
                    {
                        var removed = tableBuilder.TryRemoveEntries(elapsedTime, noInputStepsStepInfo);
                        Debug.Assert(removed);
                    }
                }
            }

            // When the count is unchanged, every new item was consumed as either a cache hit or a
            // replacement above, so itemsSet is empty and this loop is a no-op. Otherwise, any items
            // remaining in itemsSet are genuinely new and need to be added.
            Debug.Assert(previousTable is null || inputItems.Length != previousTable.Count || itemsSet.Count == 0);
            foreach (var newItem in itemsSet)
            {
                tableBuilder.AddEntry(newItem, EntryState.Added, elapsedTime, noInputStepsStepInfo, EntryState.Added);
            }
            itemsSet.Free();

            var newTable = tableBuilder.ToImmutableAndFree();
            this.LogTables(previousTable, newTable, inputItems);

            return newTable;

            PooledHashSet<T> getPooledHashSet(int capacity)
            {
                var set = _hashSetPool?.Allocate() ?? PooledHashSet<T>.GetInstance();
#if NET
                set.EnsureCapacity(capacity);
#endif
                return set;
            }

            // Builds an array of new items (present in inputItems but not in the previous
            // table) in input order. These are used to populate a modified replacement slot
            // rather than a pair of add/remove entries.
            ImmutableArray<T> getNewInputItems(ImmutableArray<T> inputs, NodeStateTable<T> previous)
            {
                var previousItemsSet = getPooledHashSet(previous.Count);
                foreach (var (item, _, _, _) in previous)
                {
                    previousItemsSet.Add(item);
                }

                var builder = ArrayBuilder<T>.GetInstance();
                foreach (var item in inputs)
                {
                    if (!previousItemsSet.Contains(item))
                    {
                        builder.Add(item);
                    }
                }

                previousItemsSet.Free();
                return builder.ToImmutableAndFree();
            }
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new InputNode<T>(_getInput, _registerOutput, _hashSetPool, comparer, _name);

        public IIncrementalGeneratorNode<T> WithTrackingName(string name) => new InputNode<T>(_getInput, _registerOutput, _hashSetPool, _comparer, name);

        public InputNode<T> WithRegisterOutput(Action<IIncrementalGeneratorOutputNode> registerOutput) => new InputNode<T>(_getInput, registerOutput, _hashSetPool, _comparer, _name);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutput(output);

        private void LogTables(NodeStateTable<T>? previousTable, NodeStateTable<T> newTable, ImmutableArray<T> inputs)
        {
            if (!CodeAnalysisEventSource.Log.IsEnabled())
            {
                // don't bother building the dummy table if we're not going to log anyway
                return;
            }

            var tableBuilder = NodeStateTable<T>.Empty.ToBuilder(_name, stepTrackingEnabled: false, tableCapacity: inputs.Length);
            foreach (var input in inputs)
            {
                tableBuilder.AddEntry(input, EntryState.Added, TimeSpan.Zero, stepInputs: default, EntryState.Added);
            }
            var inputTable = tableBuilder.ToImmutableAndFree();

            this.LogTables(_name, s_tableType, previousTable, newTable, inputTable);
        }
    }
}
