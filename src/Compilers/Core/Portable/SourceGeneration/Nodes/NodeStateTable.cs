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
        IStateTable Compact();
    }

    /// <summary>
    /// A data structure that tracks the inputs and output of an execution node
    /// </summary>
    /// <typeparam name="T">The type of the items tracked by this table</typeparam>
    internal sealed class NodeStateTable<T> : IStateTable
    {
        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<TableEntry>.Empty, isCompacted: true);

        private readonly ImmutableArray<TableEntry> _states;

        private readonly Exception? _exception;

        private NodeStateTable(ImmutableArray<TableEntry> states, bool isCompacted)
        {
            Debug.Assert(!isCompacted || states.All(s => s.IsCached));

            _states = states;
            IsCompacted = isCompacted;
            _exception = null;
        }

        private NodeStateTable(Exception exception)
        {
            _exception = exception;
            _states = ImmutableArray<TableEntry>.Empty;
            IsCompacted = false;
        }

        public int Count { get => _states.Length; }

        /// <summary>
        /// Indicates if every entry in this table has a state of <see cref="EntryState.Cached"/>
        /// </summary>
        public bool IsCompacted { get; }

        public bool IsFaulted { get => _exception is not null; }

        public UserFunctionException GetException()
        {
            Debug.Assert(_exception is not null);
            return _exception is UserFunctionException ufe ? ufe : new UserFunctionException(_exception);
        }

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

        public NodeStateTable<T> Compact()
        {
            if (IsCompacted || IsFaulted)
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

        IStateTable IStateTable.Compact() => Compact();

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
            Debug.Assert(!this.IsFaulted);
            return new Builder(this);
        }

        public static NodeStateTable<T> FromFaultedTable<U>(NodeStateTable<U> table)
        {
            Debug.Assert(table._exception is object);
            return new NodeStateTable<T>(table._exception);
        }

        public sealed class Builder
        {
            private readonly ArrayBuilder<TableEntry> _states;
            private readonly NodeStateTable<T> _previous;
            private Exception? _exception = null;

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
                var modified = new TableEntry.Builder();
                var sharedCount = Math.Min(previousEntry.Count, outputs.Length);

                // cached or modified items
                for (int i = 0; i < sharedCount; i++)
                {
                    var previous = previousEntry.GetItem(i);
                    var replacement = outputs[i];

                    var entryState = comparer.Equals(previous, replacement) ? EntryState.Cached : EntryState.Modified;
                    modified.Add(replacement, entryState);
                }

                // removed
                for (int i = sharedCount; i < previousEntry.Count; i++)
                {
                    modified.Add(previousEntry.GetItem(i), EntryState.Removed);
                }

                // added
                for (int i = sharedCount; i < outputs.Length; i++)
                {
                    modified.Add(outputs[i], EntryState.Modified);
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

            public void SetFaulted(Exception e)
            {
                _exception = e;
            }

            public NodeStateTable<T> ToImmutableAndFree()
            {
                if (_exception is object)
                {
                    _states.Free();
                    return new NodeStateTable<T>(_exception);
                }
                else if (_states.Count == 0)
                {
                    _states.Free();
                    return NodeStateTable<T>.Empty;
                }

                var hasNonCached = _states.Any(static s => !s.IsCached);
                return new NodeStateTable<T>(_states.ToImmutableAndFree(), isCompacted: !hasNonCached);
            }

            internal ImmutableArray<T> GetLastEntries()
            {
                Debug.Assert(_states.Count > 0);
                return _states[_states.Count - 1].GetItems();
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
            private readonly ImmutableArray<EntryState> _states;

            public TableEntry(T item, EntryState state)
                : this(item, default, GetSingleArray(state)) { }

            public TableEntry(ImmutableArray<T> items, EntryState state)
                : this(default, items, GetSingleArray(state)) { }

            private TableEntry(T? item, ImmutableArray<T> items, ImmutableArray<EntryState> states)
            {
                Debug.Assert(item is object || !items.IsDefault);
                Debug.Assert(!states.IsDefault);

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

            public ImmutableArray<T> GetItems() => IsSingle ? ImmutableArray.Create(_item) : _items;

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

            public class Builder
            {
                readonly ArrayBuilder<T> _items = ArrayBuilder<T>.GetInstance();

                ArrayBuilder<EntryState>? _states;

                EntryState? _currentState;

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
