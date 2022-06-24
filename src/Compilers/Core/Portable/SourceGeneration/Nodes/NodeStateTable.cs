// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // A node table is the fundamental structure we use to track changes through the incremental 
    // generator api. It can be thought of as a series of slots that take their input from an
    // upstream table and produce 0-or-more outputs. When viewed from a downstream table the outputs
    // are presented as a single unified list, with each output forming the new input to the downstream
    // table.
    // 
    // Each slot has an associated state which is used to inform the operation that should be performed
    // to create or update the outputs. States generally flow through from upstream to downstream tables.
    // For instance an Added state implies that the upstream table produced a value that was not seen 
    // in the previous iteration, and the table should run whatever transform it tracks on the input 
    // to produce the outputs. These new outputs will also have a state of Added. A cached input specifies
    // that the input has not changed, and thus the outputs will be the same as the previous run. Added,
    // and Modified inputs will always run a transform to produce new outputs. Cached and Removed
    // entries will always use the previous entries and perform no work.
    // 
    // It is important to track Removed entries while updating the downstream tables, as an upstream 
    // remove can result in multiple downstream entries being removed. However, once all tables are up 
    // to date, the removed entries are no longer needed, and the remaining entries can be considered to
    // be cached. This process is called 'compaction' and results in the actual tables which are stored
    // between runs, as opposed to the 'live' tables that exist during an update.
    //
    // Modified entries are similar to added inputs, but with a subtle difference. When an input is Added
    // all outputs are unconditionally added too. However when an input is modified, the outputs may still
    // be the same (for instance something changed elsewhere in a file that had no bearing on the produced
    // output). In this case, the state table checks the results against the previously produced values,
    // and any that are found to be the same instead get a cached state, meaning no new downstream work 
    // will be produced for them. Thus a modified input is the only slot that can have differing output 
    // states.

    internal enum EntryState { Added, Removed, Modified, Cached };

    internal interface IStateTable
    {
        IStateTable AsCached();

        bool HasTrackedSteps { get; }
        ImmutableArray<IncrementalGeneratorRunStep> Steps { get; }
    }

    internal readonly record struct NodeStateEntry<T>(T Item, EntryState State, int OutputIndex, IncrementalGeneratorRunStep? Step);

    /// <summary>
    /// A data structure that tracks the inputs and output of an execution node
    /// </summary>
    /// <typeparam name="T">The type of the items tracked by this table</typeparam>
    internal sealed class NodeStateTable<T> : IStateTable
    {
        private static readonly ConcurrentQueue<(ArrayBuilder<TableEntry> builder, PoolingStatistics statistics)> s_tableEntryPool = new();
        private static readonly ConcurrentQueue<(ArrayBuilder<NodeStateEntry<T>> builder, PoolingStatistics statistics)> s_nodeStateEntryPool = new();

        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<TableEntry>.Empty, ImmutableArray<IncrementalGeneratorRunStep>.Empty, isCompacted: true, hasTrackedSteps: true);

        private readonly ImmutableArray<TableEntry> _states;

        private NodeStateTable(ImmutableArray<TableEntry> states, ImmutableArray<IncrementalGeneratorRunStep> steps, bool isCompacted, bool hasTrackedSteps)
        {
            Debug.Assert(!isCompacted || states.All(s => s.IsCached));
            Debug.Assert(!hasTrackedSteps || steps.Length == states.Length);

            _states = states;
            Steps = steps;
            IsCached = isCompacted;
            HasTrackedSteps = hasTrackedSteps;
        }

        public int Count { get => _states.Length; }

        /// <summary>
        /// Indicates if every entry in this table has a state of <see cref="EntryState.Cached"/>
        /// </summary>
        public bool IsCached { get; }

        public bool IsEmpty => _states.IsEmpty;

        public bool HasTrackedSteps { get; }

        public ImmutableArray<IncrementalGeneratorRunStep> Steps { get; }

        public IEnumerator<NodeStateEntry<T>> GetEnumerator()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                TableEntry inputEntry = _states[i];
                IncrementalGeneratorRunStep? step = HasTrackedSteps ? Steps[i] : null;
                for (int j = 0; j < inputEntry.Count; j++)
                {
                    yield return new NodeStateEntry<T>(inputEntry.GetItem(j), inputEntry.GetState(j), j, step);
                }
            }
        }

        public NodeStateTable<T> AsCached()
        {
            if (IsCached)
                return this;

            var compacted = DequeuePooledItem(s_tableEntryPool);
            foreach (var entry in _states)
            {
                if (!entry.IsRemoved)
                {
                    compacted.builder.Add(entry.AsCached());
                }
            }
            // When we're preparing a table for caching between runs, we drop the step information as we cannot guarantee the graph structure while also updating
            // the input states
            var result = new NodeStateTable<T>(compacted.builder.ToImmutable(), ImmutableArray<IncrementalGeneratorRunStep>.Empty, isCompacted: true, hasTrackedSteps: false);
            ReturnPooledItem(s_tableEntryPool, compacted);
            return result;
        }

        IStateTable IStateTable.AsCached() => AsCached();

        public (T item, IncrementalGeneratorRunStep? step) Single()
        {
            Debug.Assert((_states.Length == 1 || _states.Length == 2 && _states[0].IsRemoved) && _states[^1].Count == 1);
            return (_states[^1].GetItem(0), HasTrackedSteps ? Steps[^1] : null);
        }

        public ImmutableArray<NodeStateEntry<T>> Batch()
        {
            var sourceBuilder = DequeuePooledItem(s_nodeStateEntryPool);
            foreach (var entry in this)
            {
                // If we have tracked steps, then we need to report removed entries to ensure all steps are in the graph.
                // Otherwise, we can just return non-removed entries to build the next value.
                if (entry.State != EntryState.Removed || HasTrackedSteps)
                {
                    sourceBuilder.builder.Add(entry);
                }
            }

            var result = sourceBuilder.builder.ToImmutable();
            ReturnPooledItem(s_nodeStateEntryPool, sourceBuilder);
            return result;
        }

        public Builder ToBuilder(string? stepName, bool stepTrackingEnabled)
        {
            return new Builder(this, stepName, stepTrackingEnabled);
        }

        public NodeStateTable<T> CreateCachedTableWithUpdatedSteps<TInput>(NodeStateTable<TInput> inputTable, string? stepName)
        {
            Debug.Assert(inputTable.HasTrackedSteps && inputTable.IsCached);
            NodeStateTable<T>.Builder builder = ToBuilder(stepName, stepTrackingEnabled: true);
            foreach (var entry in inputTable)
            {
                var inputs = ImmutableArray.Create((entry.Step!, entry.OutputIndex));
                bool usedCachedEntry = builder.TryUseCachedEntries(TimeSpan.Zero, inputs);
                Debug.Assert(usedCachedEntry);
            }
            return builder.ToImmutableAndFree();
        }

        public sealed class Builder
        {
            private readonly (ArrayBuilder<TableEntry> builder, PoolingStatistics statistics) _states = DequeuePooledItem(s_tableEntryPool);
            private readonly NodeStateTable<T> _previous;

            private readonly string? _name;
            private readonly ArrayBuilder<IncrementalGeneratorRunStep>? _steps;

            [MemberNotNullWhen(true, nameof(_steps))]
            public bool TrackIncrementalSteps => _steps is not null;

            internal Builder(NodeStateTable<T> previous, string? name, bool stepTrackingEnabled)
            {
                _previous = previous;
                _name = name;
                if (stepTrackingEnabled)
                {
                    _steps = ArrayBuilder<IncrementalGeneratorRunStep>.GetInstance();
                }
            }

            public bool TryRemoveEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs)
            {
                if (_previous._states.Length <= _states.builder.Count)
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to remove.
                    return false;
                }

                // Mark the corresponding entries to this node execution in the previous table as removed.
                var previousEntries = _previous._states[_states.builder.Count].AsRemoved();
                _states.builder.Add(previousEntries);
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Removed);
                return true;
            }

            public bool TryRemoveEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, out ImmutableArray<T> entries)
            {
                if (!TryRemoveEntries(elapsedTime, stepInputs))
                {
                    entries = default;
                    return false;
                }

                entries = _states.builder[^1].ToImmutableArray();
                return true;
            }

            public bool TryUseCachedEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs)
            {
                if (_previous._states.Length <= _states.builder.Count)
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to copy as cached.
                    return false;
                }

                var previousEntries = _previous._states[_states.builder.Count];
                Debug.Assert(previousEntries.IsCached);

                _states.builder.Add(previousEntries);
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Cached);
                return true;
            }

            public bool TryUseCachedEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, out ImmutableArray<T> entries)
            {
                if (!TryUseCachedEntries(elapsedTime, stepInputs))
                {
                    entries = default;
                    return false;
                }

                entries = _states.builder[^1].ToImmutableArray();
                return true;
            }

            public bool TryModifyEntry(T value, IEqualityComparer<T> comparer, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                if (_previous._states.Length <= _states.builder.Count)
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to try to modify.
                    return false;
                }

                Debug.Assert(_previous._states[_states.builder.Count].Count == 1);
                var (chosen, state) = GetModifiedItemAndState(_previous._states[_states.builder.Count].GetItem(0), value, comparer);
                _states.builder.Add(new TableEntry(chosen, state));
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
                return true;
            }

            public bool TryModifyEntries(ImmutableArray<T> outputs, IEqualityComparer<T> comparer, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                if (_previous._states.Length <= _states.builder.Count)
                {
                    return false;
                }

                // Semantics:
                // For each item in the row, we compare with the new matching new value.
                // - Cached when the same
                // - Modified when different
                // - Removed when old item position > outputs.length
                // - Added when new item position < previousTable.length

                var previousEntry = _previous._states[_states.builder.Count];

                // when both entries have no items, we can short circuit
                if (previousEntry.Count == 0 && outputs.Length == 0)
                {
                    _states.builder.Add(previousEntry);
                    if (TrackIncrementalSteps)
                    {
                        RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Cached);
                    }
                    return true;
                }

                using var modified = new TableEntry.Builder();
                var sharedCount = Math.Min(previousEntry.Count, outputs.Length);

                // cached or modified items
                for (int i = 0; i < sharedCount; i++)
                {
                    var previous = previousEntry.GetItem(i);
                    var replacement = outputs[i];

                    (var chosen, var state) = GetModifiedItemAndState(previous, replacement, comparer);
                    modified.Add(chosen, state);
                }

                // removed
                for (int i = sharedCount; i < previousEntry.Count; i++)
                {
                    modified.Add(previousEntry.GetItem(i), EntryState.Removed);
                }

                // added
                for (int i = sharedCount; i < outputs.Length; i++)
                {
                    modified.Add(outputs[i], EntryState.Added);
                }

                _states.builder.Add(modified.CreateEntry());
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
                return true;
            }

            public void AddEntry(T value, EntryState state, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                _states.builder.Add(new TableEntry(value, state));
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
            }

            public void AddEntries(ImmutableArray<T> values, EntryState state, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                _states.builder.Add(new TableEntry(values, state));
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
            }

            private void RecordStepInfoForLastEntry(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                Debug.Assert(stepInputs.IsDefault == !TrackIncrementalSteps);
                if (TrackIncrementalSteps)
                {
                    // We should have already recorded step information for all steps before the most recently recorded step.
                    Debug.Assert(_steps.Count + 1 == _states.builder.Count);

                    TableEntry outputInfo = _states.builder[^1];

                    var stepOutputBuilder = ArrayBuilder<(object, IncrementalStepRunReason)>.GetInstance(outputInfo.Count);

                    for (int i = 0; i < outputInfo.Count; i++)
                    {
                        stepOutputBuilder.Add((outputInfo.GetItem(i)!, AsStepState(overallInputState, outputInfo.GetState(i))));
                    }

                    _steps.Add(
                        new IncrementalGeneratorRunStep(
                            _name,
                            stepInputs,
                            stepOutputBuilder.ToImmutableAndFree(),
                            elapsedTime));
                }
            }

            public IReadOnlyList<IncrementalGeneratorRunStep> Steps => (IReadOnlyList<IncrementalGeneratorRunStep>?)_steps ?? ImmutableArray<IncrementalGeneratorRunStep>.Empty;

            private static IncrementalStepRunReason AsStepState(EntryState inputState, EntryState outputState)
            {
                return (inputState, outputState) switch
                {
                    (EntryState.Added, EntryState.Added) => IncrementalStepRunReason.New,
                    (EntryState.Modified, EntryState.Modified) => IncrementalStepRunReason.Modified,
                    (EntryState.Modified, EntryState.Cached) => IncrementalStepRunReason.Unchanged,
                    (EntryState.Cached, EntryState.Cached) => IncrementalStepRunReason.Cached,
                    (EntryState.Removed, EntryState.Removed) => IncrementalStepRunReason.Removed,
                    (EntryState.Modified, EntryState.Removed) => IncrementalStepRunReason.Removed,
                    (EntryState.Modified, EntryState.Added) => IncrementalStepRunReason.Modified,
                    _ => throw ExceptionUtilities.UnexpectedValue((inputState, outputState))
                };
            }

            public NodeStateTable<T> ToImmutableAndFree()
            {
                Debug.Assert(!TrackIncrementalSteps || _states.builder.Count == _steps.Count);

                try
                {
                    if (_states.builder.Count == 0)
                    {
                        return NodeStateTable<T>.Empty;
                    }

                    var hasNonCached = _states.builder.Any(static s => !s.IsCached);
                    return new NodeStateTable<T>(
                        _states.builder.ToImmutable(),
                        TrackIncrementalSteps ? _steps.ToImmutableAndFree() : default,
                        isCompacted: !hasNonCached,
                        hasTrackedSteps: TrackIncrementalSteps);
                }
                finally
                {
                    ReturnPooledItem(s_tableEntryPool, _states);
                }
            }

            private static (T chosen, EntryState state) GetModifiedItemAndState(T previous, T replacement, IEqualityComparer<T> comparer)
            {
                // when comparing an item to check if its modified we explicitly cache the *previous* item in the case where its 
                // considered to be equal. This ensures that subsequent comparisons are stable across future generation passes.
                return comparer.Equals(previous, replacement)
                    ? (previous, EntryState.Cached)
                    : (replacement, EntryState.Modified);
            }
        }

        private struct PoolingStatistics
        {
            /// <summary>
            /// The number of times this item has been added back to the pool.  Once this goes past some threshold
            /// we will start checking if we're continually returning a large array that is mostly empty.  If so, we
            /// will try to lower the capacity of the array to prevent wastage.
            /// </summary>
            public int NumberOfTimesPooled;

            /// <summary>
            /// The number of times we returned a large array to the pool that was barely filled.  If this is a
            /// significant number of the total times pooled, then we will attempt to lower the capacity of the
            /// array.
            /// </summary>
            public int NumberOfTimesPooledWhenSparse;
        }

        private static (ArrayBuilder<TValue> builder, PoolingStatistics statistics) DequeuePooledItem<TValue>(ConcurrentQueue<(ArrayBuilder<TValue>, PoolingStatistics)> queue)
            => queue.TryDequeue(out var item) ? item : (ArrayBuilder<TValue>.GetInstance(), new PoolingStatistics());

        private static void ReturnPooledItem<TValue>(
            ConcurrentQueue<(ArrayBuilder<TValue> builder, PoolingStatistics statistics)> queue,
            (ArrayBuilder<TValue> builder, PoolingStatistics statistics) item)
        {
            // Don't bother shrinking the array for arrays less than this capacity.  They're not going to be a
            // huge waste of space so we can just pool them forever.
            const int MinCapacityToConsiderThreshold = 1000;

            // The number of times something is added/removed to the pool before we start considering
            // statistics. This is so that we have enough data to reasonably see if something is consistently
            // sparse.
            const int MinTimesPooledToConsiderStatistics = 100;

            // The ratio of Count/Capacity to be at to be considered sparse.  under this, there is a lot of
            // wasted space and we would prefer to just throw the array away.  Above this and we're reasonably
            // filling the array and should keep it around.
            const double SparseThresholdRatio = 0.25;

            // The ratio of times we pooled something sparse.  Once above this, we will jettison the array as
            // being not worth keeping.
            const double ConsistentlySparseRatio = 0.75;

            // Note: the values 0.25 and 0.75 were picked as they reflect the common array practicing of growing
            // by doubling.  So once we've grown so much that we're consistently under 25% of the array, then we
            // want to shrink down.  To prevent shrinking and inflating over and over again, we only shrink when
            // we're highly confident we're going to stay small.

            var (builder, statistics) = item;
            statistics.NumberOfTimesPooled++;

            // See if we're pooling something both large and sparse.
            if (builder.Capacity > MinCapacityToConsiderThreshold &&
                ((double)builder.Count / builder.Capacity) < SparseThresholdRatio)
            {
                // Console.WriteLine($"Pooled when sparse: {builder.GetType()} {builder.Count} / {builder.Capacity}");
                statistics.NumberOfTimesPooledWhenSparse++;
            }

            builder.Clear();

            // See if this builder has been consistently sparse. If so then time to lower its capacity.
            if (statistics.NumberOfTimesPooled > MinTimesPooledToConsiderStatistics &&
                ((double)statistics.NumberOfTimesPooledWhenSparse / statistics.NumberOfTimesPooled) > ConsistentlySparseRatio)
            {
                // Console.WriteLine($"Halved capacity: {builder.GetType()} {statistics.NumberOfTimesPooledWhenSparse} / {statistics.NumberOfTimesPooled}");
                builder.Capacity /= 2;

                // Reset our statistics.  We'll wait another 100 pooling attempts to reassess if we need to
                // adjust the capacity here.
                statistics = new PoolingStatistics
                {
                    NumberOfTimesPooled = 1,
                };
            }

            queue.Enqueue((builder, statistics));
        }

        private readonly struct TableEntry
        {
            private static readonly ImmutableArray<EntryState> s_allAddedEntries = ImmutableArray.Create(EntryState.Added);
            private static readonly ImmutableArray<EntryState> s_allCachedEntries = ImmutableArray.Create(EntryState.Cached);
            private static readonly ImmutableArray<EntryState> s_allModifiedEntries = ImmutableArray.Create(EntryState.Modified);
            private static readonly ImmutableArray<EntryState> s_allRemovedEntries = ImmutableArray.Create(EntryState.Removed);

            private readonly ImmutableArray<T> _items;
            private readonly T? _item;

            /// <summary>
            /// Represents the corresponding state of each item in <see cref="_items"/>,
            /// or contains a single state when <see cref="_item"/> is populated or when every state of <see cref="_items"/> has the same value.
            /// </summary>
            private readonly ImmutableArray<EntryState> _states;

            public TableEntry(T item, EntryState state)
                : this(item, default, GetSingleArray(state)) { }

            public TableEntry(ImmutableArray<T> items, EntryState state)
                : this(default, items, GetSingleArray(state)) { }

            private TableEntry(T? item, ImmutableArray<T> items, ImmutableArray<EntryState> states)
            {
                Debug.Assert(!states.IsDefault);
                Debug.Assert(states.Length == 1 || states.Distinct().Count() > 1);

                this._item = item;
                this._items = items;
                this._states = states;
            }

            public bool IsCached => this._states == s_allCachedEntries || this._states.All(s => s == EntryState.Cached);

            public bool IsRemoved => this._states == s_allRemovedEntries || this._states.All(s => s == EntryState.Removed);

            public int Count => IsSingle ? 1 : _items.Length;

            public T GetItem(int index)
            {
                Debug.Assert(!IsSingle || index == 0);
                return IsSingle ? _item : _items[index];
            }

            public EntryState GetState(int index) => _states.Length == 1 ? _states[0] : _states[index];

            public ImmutableArray<T> ToImmutableArray() => IsSingle ? ImmutableArray.Create(_item) : _items;

            public TableEntry AsCached() => new(_item, _items, s_allCachedEntries);

            public TableEntry AsRemoved() => new(_item, _items, s_allRemovedEntries);

            [MemberNotNullWhen(true, new[] { nameof(_item) })]
            private bool IsSingle => this._items.IsDefault;

            private static ImmutableArray<EntryState> GetSingleArray(EntryState state) => state switch
            {
                EntryState.Added => s_allAddedEntries,
                EntryState.Cached => s_allCachedEntries,
                EntryState.Modified => s_allModifiedEntries,
                EntryState.Removed => s_allRemovedEntries,
                _ => throw ExceptionUtilities.Unreachable
            };

#if DEBUG
            public override string ToString()
            {
                if (IsSingle)
                {
                    return $"{GetItem(0)}: {GetState(0)}";
                }
                else
                {
                    var sb = PooledStringBuilder.GetInstance();
                    sb.Builder.Append("{");
                    for (int i = 0; i < Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Builder.Append(',');
                        }
                        sb.Builder.Append(" (");
                        sb.Builder.Append(GetItem(i));
                        sb.Builder.Append(':');
                        sb.Builder.Append(GetState(i));
                        sb.Builder.Append(')');
                    }
                    sb.Builder.Append(" }");
                    return sb.ToStringAndFree();
                }
            }
#endif

            public ref struct Builder
            {
                private static readonly ConcurrentQueue<(ArrayBuilder<T> builder, PoolingStatistics statistics)> s_itemsPool = new();
                private static readonly ConcurrentQueue<(ArrayBuilder<EntryState> builder, PoolingStatistics statistics)> s_statesPool = new();

                private readonly (ArrayBuilder<T> items, PoolingStatistics statistics) _items = DequeuePooledItem(s_itemsPool);

                private (ArrayBuilder<EntryState> states, PoolingStatistics)? _states = null;
                private EntryState? _currentState = null;

                public Builder()
                {
                }

                public void Add(T item, EntryState state)
                {
                    _items.items.Add(item);
                    if (!_currentState.HasValue)
                    {
                        _currentState = state;
                        return;
                    }

                    if (_states is { states: var states })
                    {
                        states.Add(state);
                        return;
                    }

                    if (_currentState != state)
                    {
                        _states = DequeuePooledItem(s_statesPool);
                        states = _states.Value.states;

                        states.EnsureCapacity(_items.items.Count);
                        for (int i = 0, n = _items.items.Count - 1; i < n; i++)
                            states.Add(_currentState.Value);

                        states.Add(state);
                    }
                }

                public TableEntry CreateEntry()
                {
                    Debug.Assert(_currentState.HasValue, "Created a builder with no values?");
                    return new TableEntry(item: default, _items.items.ToImmutable(), _states?.states.ToImmutable() ?? GetSingleArray(_currentState.Value));
                }

                public void Dispose()
                {
                    ReturnPooledItem(s_itemsPool, _items);
                    if (_states != null)
                        ReturnPooledItem(s_statesPool, _states.Value);
                }
            }
        }
    }
}
