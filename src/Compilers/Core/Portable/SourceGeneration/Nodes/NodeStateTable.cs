// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal enum EntryState { Added, Removed, Modified, Cached };

    internal interface IStateTable
    {
        IStateTable Compact();
    }

    /// <summary>
    /// A data structure that tracks the inputs and output of an execution node
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class NodeStateTable<T> : IStateTable
    {
        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<ImmutableArray<(T, EntryState)>>.Empty, exception: null);

        readonly ImmutableArray<ImmutableArray<(T item, EntryState state)>> _states;

        readonly Exception? _exception;

        private NodeStateTable(ImmutableArray<ImmutableArray<(T, EntryState)>> states, Exception? exception)
        {
            _states = states;
            _exception = exception;
        }

        public bool IsFaulted { get => _exception is not null; }

        public IEnumerator<(T item, EntryState state)> GetEnumerator()
        {
            return _states.SelectMany(s => s).GetEnumerator();
        }

        public IStateTable Compact()
        {
            var compacted = ArrayBuilder<ImmutableArray<(T, EntryState)>>.GetInstance();
            foreach (var entry in _states)
            {
                // we have to keep empty entries
                // we only remove all entries at once, so only need to check the first item
                if (entry.Length == 0 || entry[0].state != EntryState.Removed)
                {
                    compacted.Add(entry.SelectAsArray(e => (e.item, EntryState.Cached)));
                }
            }
            return new NodeStateTable<T>(compacted.ToImmutableAndFree(), _exception);
        }

        public static NodeStateTable<T> FromFaultedTable<U>(NodeStateTable<U> table)
        {
            Debug.Assert(table.IsFaulted);
            return new NodeStateTable<T>(Empty._states, table._exception);
        }

        public class Builder
        {
            private readonly ArrayBuilder<ImmutableArray<(T, EntryState)>> _states = ArrayBuilder<ImmutableArray<(T, EntryState)>>.GetInstance();

            private Exception? _exception = null;

            public void AddEntries(ImmutableArray<T> values, EntryState state)
            {
                _states.Add(values.SelectAsArray(v => (v, state)));
            }

            public void AddEntriesFromPreviousTable(NodeStateTable<T> previousTable, EntryState newState)
            {
                Debug.Assert(previousTable._states.Length > _states.Count);
                var previousEntries = previousTable._states[_states.Count].SelectAsArray(s => (s.item, newState));
                _states.Add(previousEntries);
            }

            public void SetFaulted(Exception e)
            {
                _exception = e;
            }

            public NodeStateTable<T> ToImmutableAndFree() => new NodeStateTable<T>(_states.ToImmutableAndFree(), exception: _exception);
        }
    }
}
