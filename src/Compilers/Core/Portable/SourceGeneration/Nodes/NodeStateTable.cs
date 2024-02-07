// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<TableEntry>.Empty, ImmutableArray<IncrementalGeneratorRunStep>.Empty, hasTrackedSteps: true, isCached: false);

        private readonly ImmutableArray<TableEntry> _states;

        private NodeStateTable(ImmutableArray<TableEntry> states, ImmutableArray<IncrementalGeneratorRunStep> steps, bool hasTrackedSteps, bool isCached)
        {
            Debug.Assert(!hasTrackedSteps || steps.Length == states.Length);

            _states = states;
            Steps = steps;
            IsCached = isCached;
            HasTrackedSteps = hasTrackedSteps;
        }

        public int Count => _states.Length;

        /// <summary>
        /// Indicates that this table is unchanged from the previous version.
        /// </summary>
        public bool IsCached { get; }

        public bool IsEmpty => _states.IsEmpty;

        public bool HasTrackedSteps { get; }

        public ImmutableArray<IncrementalGeneratorRunStep> Steps { get; }

        public int GetTotalEntryItemCount()
            => _states.Sum(static e => e.Count);

        public struct Enumerator
        {
            private readonly NodeStateTable<T> _stateTable;
            private int _nextStatesIndex;
            private int _nextInputEntryIndex;
            private IncrementalGeneratorRunStep? _step;
            private TableEntry _inputEntry;
            private NodeStateEntry<T> _current;

            public Enumerator(NodeStateTable<T> stateTable)
            {
                _stateTable = stateTable;
                _nextStatesIndex = 0;

                UpdateAfterNextStatesIndexModification();
            }

            public NodeStateEntry<T> Current => _current;

            public bool MoveNext()
            {
                while (_nextStatesIndex < _stateTable._states.Length)
                {
                    if (_nextInputEntryIndex < _inputEntry.Count)
                    {
                        _current = new NodeStateEntry<T>(_inputEntry.GetItem(_nextInputEntryIndex), _inputEntry.GetState(_nextInputEntryIndex), _nextInputEntryIndex, _step);
                        _nextInputEntryIndex += 1;

                        return true;
                    }

                    _nextStatesIndex += 1;

                    UpdateAfterNextStatesIndexModification();
                }

                return false;
            }

            private void UpdateAfterNextStatesIndexModification()
            {
                _nextInputEntryIndex = 0;

                if (_nextStatesIndex < _stateTable._states.Length)
                {
                    _step = _stateTable.HasTrackedSteps ? _stateTable.Steps[_nextStatesIndex] : null;
                    _inputEntry = _stateTable._states[_nextStatesIndex];
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public NodeStateTable<T> AsCached()
        {
            if (IsCached)
                return this;

            // If the input to an entry was removed, we also need to remove the entry.
            // However, if the input was present, but the entry didn't produce any values (or removed them all),
            // we need to keep the empty entry as a placeholder, so that on the next generation pass
            // we can retrieve it as the cached value of the input.
            var nonRemovedCount = _states.Count(static e => !e.IsRemovedDueToInputRemoval);

            var compacted = ArrayBuilder<TableEntry>.GetInstance(nonRemovedCount);
            foreach (var entry in _states)
            {
                if (!entry.IsRemovedDueToInputRemoval)
                    compacted.Add(entry.AsCached());
            }

            // When we're preparing a table for caching between runs, we drop the step information as we cannot guarantee the graph structure while also updating
            // the input states

            // Ensure we are completely full so that ToImmutable translates to a MoveToImmutable
            Debug.Assert(compacted.Count == nonRemovedCount);
            return new NodeStateTable<T>(compacted.ToImmutableAndFree(), ImmutableArray<IncrementalGeneratorRunStep>.Empty, hasTrackedSteps: false, isCached: true);
        }

        IStateTable IStateTable.AsCached() => AsCached();

        public (T item, IncrementalGeneratorRunStep? step) Single()
        {
            Debug.Assert((_states.Length == 1 || _states.Length == 2 && _states[0].IsRemovedDueToInputRemoval) && _states[^1].Count == 1);
            return (_states[^1].GetItem(0), HasTrackedSteps ? Steps[^1] : null);
        }

        public Builder ToBuilder(string? stepName, bool stepTrackingEnabled, IEqualityComparer<T>? equalityComparer = null, int? tableCapacity = null)
            => new(this, stepName, stepTrackingEnabled, equalityComparer, tableCapacity);

        public NodeStateTable<T> CreateCachedTableWithUpdatedSteps<TInput>(NodeStateTable<TInput> inputTable, string? stepName, IEqualityComparer<T> equalityComparer)
        {
            Debug.Assert(inputTable.HasTrackedSteps && inputTable.IsCached);
            NodeStateTable<T>.Builder builder = ToBuilder(stepName, stepTrackingEnabled: true, equalityComparer);
            foreach (var entry in inputTable)
            {
                var inputs = ImmutableArray.Create((entry.Step!, entry.OutputIndex));
                bool usedCachedEntry = builder.TryUseCachedEntries(TimeSpan.Zero, inputs);
                Debug.Assert(usedCachedEntry);
            }

            return builder.ToImmutableAndFree();
        }

        public string GetPackedStates()
        {
            var pooled = PooledStringBuilder.GetInstance();
            foreach (var state in _states)
            {
                for (int i = 0; i < state.Count; i++)
                {
                    pooled.Builder.Append(state.GetState(i).ToString()[0]);
                }
                pooled.Builder.Append(',');
            }
            return pooled.ToStringAndFree();
        }

        /// <remarks>
        /// The builder is <b>not</b> threadsafe.
        /// </remarks>
        public sealed class Builder
        {
            private readonly ArrayBuilder<TableEntry> _states;
            private readonly NodeStateTable<T> _previous;

            private readonly string? _name;
            private readonly IEqualityComparer<T> _equalityComparer;
            private readonly ArrayBuilder<IncrementalGeneratorRunStep>? _steps;

            private int _insertedCount = 0;

            [MemberNotNullWhen(true, nameof(_steps))]
            public bool TrackIncrementalSteps => _steps is not null;

#if DEBUG
            private readonly int? _requestedTableCapacity;
#endif

            internal Builder(
                NodeStateTable<T> previous,
                string? name,
                bool stepTrackingEnabled,
                IEqualityComparer<T>? equalityComparer,
                int? tableCapacity)
            {
#if DEBUG
                _requestedTableCapacity = tableCapacity;
#endif
                // If the caller specified a desired capacity, then use that.  Otherwise, use the previous table's total
                // entry count as a reasonable approximation for what we will need.
                _states = ArrayBuilder<TableEntry>.GetInstance(tableCapacity ?? previous.GetTotalEntryItemCount());
                _previous = previous;
                _name = name;
                _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
                if (stepTrackingEnabled)
                {
                    _steps = ArrayBuilder<IncrementalGeneratorRunStep>.GetInstance();
                }
            }

            public int Count => _states.Count;

            public bool TryRemoveEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs)
            {
                if (!TryGetPreviousEntry(out var previousEntry))
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to remove.
                    return false;
                }

                // Mark the corresponding entries to this node execution in the previous table as removed.
                // Since they are removed due to their input having been removed, we won't have to keep placeholders for them.
                var previousEntries = previousEntry.AsRemovedDueToInputRemoval();
                _states.Add(previousEntries);
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Removed);
                return true;
            }

            public bool TryRemoveEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, out OneOrMany<T> entries)
            {
                if (!TryRemoveEntries(elapsedTime, stepInputs))
                {
                    entries = default;
                    return false;
                }

                entries = _states[^1].Items;
                return true;
            }

            public bool TryUseCachedEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs)
            {
                if (!TryGetPreviousEntry(out var previousEntries))
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to copy as cached.
                    return false;
                }

                Debug.Assert(previousEntries.IsCached);

                _states.Add(previousEntries);
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Cached);
                return true;
            }

            internal bool TryUseCachedEntries(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, out TableEntry entry)
            {
                if (!TryUseCachedEntries(elapsedTime, stepInputs))
                {
                    entry = default;
                    return false;
                }

                entry = _states[^1];
                return true;
            }

            public bool TryModifyEntry(T value, IEqualityComparer<T> comparer, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                if (!TryGetPreviousEntry(out var previousEntry))
                {
                    // The previous table had less node executions than this one, so we don't have any entries from a previous corresponding node execution to try to modify.
                    return false;
                }

                if (previousEntry.Count == 0)
                {
                    // it's possible that the previous execution removed this item, but we left in an empty entry as a placeholder. In which case, we can't modify it
                    return false;
                }

                Debug.Assert(previousEntry.Count == 1);
                var (chosen, state, _) = GetModifiedItemAndState(previousEntry.GetItem(0), value, comparer);
                _states.Add(new TableEntry(OneOrMany.Create(chosen), state));
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
                return true;
            }

            public bool TryModifyEntries(ImmutableArray<T> outputs, IEqualityComparer<T> comparer, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                // Semantics:
                // For each item in the row, we compare with the new matching new value.
                // - Cached when the same
                // - Modified when different
                // - Removed when old item position > outputs.length
                // - Added when new item position < previousTable.length

                if (!TryGetPreviousEntry(out var previousEntry))
                {
                    return false;
                }

                // when both entries have no items, we can short circuit
                if (previousEntry.Count == 0 && outputs.Length == 0)
                {
                    _states.Add(previousEntry);
                    if (TrackIncrementalSteps)
                    {
                        RecordStepInfoForLastEntry(elapsedTime, stepInputs, EntryState.Cached);
                    }

                    return true;
                }

                // We may be able to move the previous entry over wholesale.  So avoid creating an builder and doing any
                // expensive work there until necessary (e.g. we detected either a different item or a different state).
                // We can only do this if the counts of before/after are the same. If not, then obviously something
                // changed and we can't reuse the before item.

                var totalBuilderItems = Math.Max(previousEntry.Count, outputs.Length);
                var builder = previousEntry.Count == outputs.Length ? null : new TableEntry.Builder(capacity: totalBuilderItems);

                var sharedCount = Math.Min(previousEntry.Count, outputs.Length);

                // cached or modified items
                for (int i = 0; i < sharedCount; i++)
                {
                    var previousItem = previousEntry.GetItem(i);
                    var previousState = previousEntry.GetState(i);
                    var replacementItem = outputs[i];

                    var (chosenItem, state, chosePrevious) = GetModifiedItemAndState(previousItem, replacementItem, comparer);

                    if (builder != null)
                    {
                        // if we have a builder, then we're keeping track of all entries no matter what.
                        builder.Add(chosenItem, state);
                        continue;
                    }

                    if (!chosePrevious || state != previousState)
                    {
                        // We don't have a builder, but we also can't use the previous entry.  Make a builder, copy
                        // everything prior to this point to it, and then add the latest entry.
                        builder = new TableEntry.Builder(capacity: totalBuilderItems);
                        for (int j = 0; j < i; j++)
                            builder.Add(previousEntry.GetItem(j), previousEntry.GetState(j));

                        builder.Add(chosenItem, state);
                        continue;
                    }

                    // otherwise, we don't have a builder and we are still able to use the previous entry.  Keep going
                    // without constructing anything.
                }

                // removed
                for (int i = sharedCount; i < previousEntry.Count; i++)
                {
                    // We know we must have a builder because we only get into this path when the counts are different
                    // (and thus we created a builder at the start).
                    builder!.Add(previousEntry.GetItem(i), EntryState.Removed);
                }

                // added
                for (int i = sharedCount; i < outputs.Length; i++)
                {
                    // We know we must have a builder because we only get into this path when the counts are different
                    // (and thus we created a builder at the start).
                    builder!.Add(outputs[i], EntryState.Added);
                }

                // If we still don't have a builder, then we can reuse the previous table entry entirely.  Otherwise,
                // construct the new one from the values collected.
                _states.Add(builder == null ? previousEntry : builder.ToImmutableAndFree());

                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
                return true;
            }

            public bool TryModifyEntries(ImmutableArray<T> outputs, IEqualityComparer<T> comparer, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState, out TableEntry entry)
            {
                if (!TryModifyEntries(outputs, comparer, elapsedTime, stepInputs, overallInputState))
                {
                    entry = default;
                    return false;
                }

                entry = _states[^1];
                return true;
            }

            public void AddEntry(T value, EntryState state, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                _states.Add(new TableEntry(OneOrMany.Create(value), state));
                _insertedCount += state == EntryState.Added ? 1 : 0;
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
            }

            public TableEntry AddEntries(ImmutableArray<T> values, EntryState state, TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                var tableEntry = new TableEntry(OneOrMany.Create(values), state);
                _states.Add(tableEntry);
                _insertedCount += state == EntryState.Added ? 1 : 0;
                RecordStepInfoForLastEntry(elapsedTime, stepInputs, overallInputState);
                return tableEntry;
            }

            private bool TryGetPreviousEntry(out TableEntry previousEntry)
            {
                // When indexing into the previous table we need to subtract the number of entries that have been explicitly added
                // to the current table, as they didn't exist in the previous one.
                var previousTableEntryIndex = _states.Count - _insertedCount;

                var canUsePrevious = _previous._states.Length > previousTableEntryIndex;
                previousEntry = canUsePrevious ? _previous._states[previousTableEntryIndex] : default;
                return canUsePrevious;
            }

            private void RecordStepInfoForLastEntry(TimeSpan elapsedTime, ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)> stepInputs, EntryState overallInputState)
            {
                Debug.Assert(stepInputs.IsDefault == !TrackIncrementalSteps);
                if (TrackIncrementalSteps)
                {
                    // We should have already recorded step information for all steps before the most recently recorded step.
                    Debug.Assert(_steps.Count + 1 == _states.Count);

                    TableEntry outputInfo = _states[^1];

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
                    (EntryState.Modified, EntryState.Added) => IncrementalStepRunReason.New,
                    _ => throw ExceptionUtilities.UnexpectedValue((inputState, outputState))
                };
            }

            public NodeStateTable<T> ToImmutableAndFree()
            {
                Debug.Assert(!TrackIncrementalSteps || _states.Count == _steps.Count);

                if (_states.Count == 0)
                {
                    _states.Free();
                    return NodeStateTable<T>.Empty;
                }

#if DEBUG
                // If the caller requested a specific capacity, then we should have added either that amount, or some
                // amount less than that.  It's possible to have added less as a Where clause will mean some amount of
                // states are filtered out.
                Debug.Assert(_requestedTableCapacity == null || _states.Count <= _requestedTableCapacity);
#endif

                // if we added the exact same entries as before, then we can directly embed previous' entry array,
                // avoiding a costly allocation of the same data.
                ImmutableArray<TableEntry> finalStates;
                if (_states.Count == _previous.Count && _states.SequenceEqual(_previous._states, (e1, e2) => e1.Matches(e2, _equalityComparer)))
                {
                    finalStates = _previous._states;
                    _states.Free();
                }
                else
                {
                    // Important to use ToImmutableAndFree so that we will MoveToImmutable when the requested capacity
                    // equals the count.
                    finalStates = _states.ToImmutableAndFree();
                }

                return new NodeStateTable<T>(
                    finalStates,
                    TrackIncrementalSteps ? _steps.ToImmutableAndFree() : default,
                    hasTrackedSteps: TrackIncrementalSteps,
                    isCached: finalStates.All(static s => s.IsCached) && _previous.GetTotalEntryItemCount() == finalStates.Sum(static s => s.Count));
            }

            private static (T chosen, EntryState state, bool chosePrevious) GetModifiedItemAndState(T previous, T replacement, IEqualityComparer<T> comparer)
            {
                // when comparing an item to check if its modified we explicitly cache the *previous* item in the case where its 
                // considered to be equal. This ensures that subsequent comparisons are stable across future generation passes.
                return comparer.Equals(previous, replacement)
                    ? (previous, EntryState.Cached, chosePrevious: true)
                    : (replacement, EntryState.Modified, chosePrevious: false);
            }
        }

        internal readonly struct TableEntry
        {
            private static readonly ImmutableArray<EntryState> s_allAddedEntries = ImmutableArray.Create(EntryState.Added);
            private static readonly ImmutableArray<EntryState> s_allCachedEntries = ImmutableArray.Create(EntryState.Cached);
            private static readonly ImmutableArray<EntryState> s_allModifiedEntries = ImmutableArray.Create(EntryState.Modified);

            /// <summary>
            /// All items removed as part of a transformation from non-empty input.
            /// </summary>
            private static readonly ImmutableArray<EntryState> s_allRemovedEntries = ImmutableArray.Create(EntryState.Removed);

            /// <summary>
            /// All items removed because the input has been removed.
            /// </summary>
            private static readonly ImmutableArray<EntryState> s_allRemovedDueToInputRemoval = ImmutableArray.Create(EntryState.Removed);

            private readonly OneOrMany<T> _items;
            private readonly bool _anyRemoved;

            /// <summary>
            /// Represents the corresponding state of each item in <see cref="_items"/>, or contains a single state when
            /// <see cref="_items"/> is populated or when every state of <see cref="_items"/> has the same value.
            /// </summary>
            private readonly ImmutableArray<EntryState> _states;

            public TableEntry(OneOrMany<T> items, EntryState state)
                : this(items, GetSingleArray(state), anyRemoved: state == EntryState.Removed) { }

            private TableEntry(OneOrMany<T> items, ImmutableArray<EntryState> states, bool anyRemoved)
            {
                Debug.Assert(!states.IsDefault);
                Debug.Assert(states.Length == 1 || states.Distinct().Length > 1);

                _items = items;
                _states = states;
                _anyRemoved = anyRemoved;
            }

            public bool Matches(TableEntry entry, IEqualityComparer<T> equalityComparer)
            {
                if (!_states.SequenceEqual(entry._states))
                    return false;

                if (this.Count != entry.Count)
                    return false;

                for (int i = 0, n = this.Count; i < n; i++)
                {
                    if (!equalityComparer.Equals(this.GetItem(i), entry.GetItem(i)))
                        return false;
                }

                return true;
            }

            public bool IsCached => this._states == s_allCachedEntries || this._states.All(s => s == EntryState.Cached);

            public bool IsRemovedDueToInputRemoval => this._states == s_allRemovedDueToInputRemoval;

            public int Count => _items.Count;

            public T GetItem(int index) => _items[index];

            public EntryState GetState(int index) => _states.Length == 1 ? _states[0] : _states[index];

            public OneOrMany<T> Items => _items;

            public TableEntry AsCached()
            {
                if (!_anyRemoved)
                {
                    return new TableEntry(_items, s_allCachedEntries, anyRemoved: false);
                }

                var itemBuilder = ArrayBuilder<T>.GetInstance();
                for (int i = 0; i < this.Count; i++)
                {
                    if (this.GetState(i) != EntryState.Removed)
                    {
                        itemBuilder.Add(this.GetItem(i));
                    }
                }

                Debug.Assert(itemBuilder.Count < this.Count);
                return new TableEntry(OneOrMany.Create(itemBuilder.ToImmutableArray()), s_allCachedEntries, anyRemoved: false);
            }

            public TableEntry AsRemovedDueToInputRemoval() => new(_items, s_allRemovedDueToInputRemoval, anyRemoved: true);

            private static ImmutableArray<EntryState> GetSingleArray(EntryState state) => state switch
            {
                EntryState.Added => s_allAddedEntries,
                EntryState.Cached => s_allCachedEntries,
                EntryState.Modified => s_allModifiedEntries,
                EntryState.Removed => s_allRemovedEntries,
                _ => throw ExceptionUtilities.Unreachable()
            };

            public Enumerator GetEnumerator()
                => new(this);

            public struct Enumerator
            {
                private readonly TableEntry _entry;
                private int _index = -1;

                public Enumerator(TableEntry tableEntry)
                {
                    _entry = tableEntry;
                }

                public bool MoveNext()
                {
                    _index++;
                    return _index < _entry.Count;
                }

                public T Current => _entry.GetItem(_index);
            }

#if DEBUG
            public override string ToString()
            {
                if (this.Count == 1)
                {
                    return $"{GetItem(0)}: {GetState(0)}";
                }
                else
                {
                    var sb = PooledStringBuilder.GetInstance();
                    sb.Builder.Append('{');
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

            public sealed class Builder
            {
                private readonly ArrayBuilder<T> _items;

                private ArrayBuilder<EntryState>? _states;
                private EntryState? _currentState;
                private bool _anyRemoved;

                private readonly int _requestedCapacity;

                public Builder(int capacity)
                {
                    _items = ArrayBuilder<T>.GetInstance(capacity);
                    _requestedCapacity = capacity;
                }

                public void Add(T item, EntryState state)
                {
                    _items.Add(item);
                    _anyRemoved |= state == EntryState.Removed;
                    if (!_currentState.HasValue)
                    {
                        _currentState = state;
                    }
                    else if (_states is not null)
                    {
                        _states.Add(state);
                    }
                    else if (_currentState != state)
                    {
                        // Create a builder with the right capacity (so we don't waste scratch space). Copy all the same
                        // prior values all the way up to the last item we're about to add.
                        _states = ArrayBuilder<EntryState>.GetInstance(_requestedCapacity);
                        for (int i = 0, n = _items.Count - 1; i < n; i++)
                            _states.Add(_currentState.Value);

                        // then finally add the new value at the end.
                        _states.Add(state);
                    }
                }

                public TableEntry ToImmutableAndFree()
                {
                    Debug.Assert(_currentState.HasValue, "Created a builder with no values?");
                    Debug.Assert(_items.Count >= 1, "Created a builder with no values?");

                    Debug.Assert(_items.Count == _requestedCapacity);
                    Debug.Assert(_states == null || _states.Count == _requestedCapacity);

                    return new TableEntry(_items.ToOneOrManyAndFree(), _states?.ToImmutableAndFree() ?? GetSingleArray(_currentState.Value), anyRemoved: _anyRemoved);
                }
            }
        }
    }
}
