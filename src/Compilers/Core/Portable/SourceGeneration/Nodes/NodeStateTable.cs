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
    }

    /// <summary>
    /// A data structure that tracks the inputs and output of an execution node
    /// </summary>
    /// <typeparam name="T">The type of the items tracked by this table</typeparam>
    internal sealed class NodeStateTable<T> : IStateTable
    {
        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<TableEntry>.Empty, isCompacted: true);

        private readonly ImmutableArray<TableEntry> _states;


        private NodeStateTable(ImmutableArray<TableEntry> states, bool isCompacted)
        {
            Debug.Assert(!isCompacted || states.All(s => s.IsCached));

            _states = states;
            IsCached = isCompacted;
        }

        public int Count { get => _states.Length; }

        /// <summary>
        /// Indicates if every entry in this table has a state of <see cref="EntryState.Cached"/>
        /// </summary>
        public bool IsCached { get; }

        public IEnumerator<(T item, EntryState state)> GetEnumerator()
        {
            foreach (var inputEntry in _states)
            {
                for (int i = 0; i < inputEntry.Count; i++)
                {
                    yield return (inputEntry.GetItem(i), inputEntry.GetState(i));
                }
            }
        }

        public NodeStateTable<T> AsCached()
        {
            if (IsCached)
                return this;

            var compacted = ArrayBuilder<TableEntry>.GetInstance();
            foreach (var entry in _states)
            {
                if (!entry.IsRemoved)
                {
                    compacted.Add(entry.AsCached());
                }
            }
            return new NodeStateTable<T>(compacted.ToImmutableAndFree(), isCompacted: true);
        }

        IStateTable IStateTable.AsCached() => AsCached();

        public T Single()
        {
            Debug.Assert((_states.Length == 1 || _states.Length == 2 && _states[0].IsRemoved) && this._states[^1].Count == 1);
            return this._states[^1].GetItem(0);
        }

        public ImmutableArray<T> Batch()
        {
            var sourceBuilder = ArrayBuilder<T>.GetInstance();
            foreach (var entry in this)
            {
                // we don't return removed entries to the downstream node.
                // we're creating a new state table as part of this call, so they're no longer needed
                if (entry.state != EntryState.Removed)
                {
                    sourceBuilder.Add(entry.item);
                }
            }
            return sourceBuilder.ToImmutableAndFree();
        }

        public Builder ToBuilder()
        {
            return new Builder(this);
        }

        public sealed class Builder
        {
            private readonly ArrayBuilder<TableEntry> _states;
            private readonly NodeStateTable<T> _previous;

            internal Builder(NodeStateTable<T> previous)
            {
                _states = ArrayBuilder<TableEntry>.GetInstance();
                _previous = previous;
            }

            public void RemoveEntries()
            {
                // if a new table is asked to remove entries we can just do nothing
                // as it can't have any effect on downstream tables
                if (_previous._states.Length > _states.Count)
                {
                    var previousEntries = _previous._states[_states.Count].AsRemoved();
                    _states.Add(previousEntries);
                }
            }

            public bool TryUseCachedEntries()
            {
                if (_previous._states.Length <= _states.Count)
                {
                    return false;
                }

                var previousEntries = _previous._states[_states.Count];
                Debug.Assert(previousEntries.IsCached);

                _states.Add(previousEntries);
                return true;
            }

            public bool TryUseCachedEntries(out ImmutableArray<T> entries)
            {
                if (!TryUseCachedEntries())
                {
                    entries = default;
                    return false;
                }

                entries = _states[_states.Count - 1].ToImmutableArray();
                return true;
            }

            public bool TryModifyEntry(T value, IEqualityComparer<T> comparer)
            {
                if (_previous._states.Length <= _states.Count)
                {
                    return false;
                }

                Debug.Assert(_previous._states[_states.Count].Count == 1);
                var (chosen, state) = GetModifiedItemAndState(_previous._states[_states.Count].GetItem(0), value, comparer);
                _states.Add(new TableEntry(chosen, state));
                return true;
            }

            public bool TryModifyEntries(ImmutableArray<T> outputs, IEqualityComparer<T> comparer)
            {
                if (_previous._states.Length <= _states.Count)
                {
                    return false;
                }

                // Semantics:
                // For each item in the row, we compare with the new matching new value.
                // - Cached when the same
                // - Modified when different
                // - Removed when old item position > outputs.length
                // - Added when new item position < previousTable.length

                var previousEntry = _previous._states[_states.Count];

                // when both entries have no items, we can short circuit
                if (previousEntry.Count == 0 && outputs.Length == 0)
                {
                    _states.Add(previousEntry);
                    return true;
                }

                var modified = new TableEntry.Builder();
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

                _states.Add(modified.ToImmutableAndFree());
                return true;
            }

            public void AddEntry(T value, EntryState state)
            {
                _states.Add(new TableEntry(value, state));
            }

            public void AddEntries(ImmutableArray<T> values, EntryState state)
            {
                _states.Add(new TableEntry(values, state));
            }

            public NodeStateTable<T> ToImmutableAndFree()
            {
                if (_states.Count == 0)
                {
                    _states.Free();
                    return NodeStateTable<T>.Empty;
                }

                var hasNonCached = _states.Any(static s => !s.IsCached);
                return new NodeStateTable<T>(_states.ToImmutableAndFree(), isCompacted: !hasNonCached);
            }

            private (T chosen, EntryState state) GetModifiedItemAndState(T previous, T replacement, IEqualityComparer<T> comparer)
            {
                // when comparing an item to check if its modified we explicitly cache the *previous* item in the case where its 
                // considered to be equal. This ensures that subsequent comparisons are stable across future generation passes.
                return comparer.Equals(previous, replacement)
                    ? (previous, EntryState.Cached)
                    : (replacement, EntryState.Modified);
            }
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

            public sealed class Builder
            {
                private readonly ArrayBuilder<T> _items = ArrayBuilder<T>.GetInstance();

                private ArrayBuilder<EntryState>? _states;

                private EntryState? _currentState;

                public void Add(T item, EntryState state)
                {
                    _items.Add(item);
                    if (!_currentState.HasValue)
                    {
                        _currentState = state;
                    }
                    else if (_states is object)
                    {
                        _states.Add(state);
                    }
                    else if (_currentState != state)
                    {
                        _states = ArrayBuilder<EntryState>.GetInstance(_items.Count - 1, _currentState.Value);
                        _states.Add(state);
                    }
                }

                public TableEntry ToImmutableAndFree()
                {
                    Debug.Assert(_currentState.HasValue, "Created a builder with no values?");
                    return new TableEntry(item: default, _items.ToImmutableAndFree(), _states?.ToImmutableAndFree() ?? GetSingleArray(_currentState.Value));
                }
            }
        }
    }
}
