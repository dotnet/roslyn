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
        internal static NodeStateTable<T> Empty { get; } = new NodeStateTable<T>(ImmutableArray<ImmutableArray<(T, EntryState)>>.Empty, 0, exception: null);

        readonly ImmutableArray<ImmutableArray<(T, EntryState)>> _states;

        readonly int _removedCount;

        readonly Exception? _exception;

        private NodeStateTable(ImmutableArray<ImmutableArray<(T, EntryState)>> states, int removedCount, Exception? exception)
        {
            _states = states;
            _removedCount = removedCount;
            _exception = exception;
        }

        public bool IsFaulted { get => _exception is not null; }

        //PROTOTYPE: should this just be an indexer?
        public ImmutableArray<T> GetEntries(int inputIndex)
        {
            Debug.Assert(inputIndex < _states.Length);
            return _states[inputIndex].SelectAsArray(e => e.Item1);
        }

        public IEnumerator<(int index, T item, EntryState state)> GetEnumerator()
        {
            int index = 0;
            foreach (var entry in _states)
                foreach (var item in entry)
                    yield return (index++, item.Item1, item.Item2);
        }

        public IStateTable Compact()
        {
            var compacted = ArrayBuilder<ImmutableArray<(T, EntryState)>>.GetInstance(_states.Length - _removedCount);
            foreach (var entry in _states)
            {
                // we have to keep empty entries
                // we only remove all entries at once, so only need to check the first item
                if (entry.Length == 0 || entry[0].Item2 != EntryState.Removed)
                {
                    compacted.Add(entry.SelectAsArray(e => (e.Item1, EntryState.Cached)));
                }
            }
            return new NodeStateTable<T>(compacted.ToImmutableAndFree(), 0, _exception);
        }

        public static NodeStateTable<T> FromFaultedTable<U>(NodeStateTable<U> table)
        {
            Debug.Assert(table.IsFaulted);
            return new NodeStateTable<T>(Empty._states, 0, table._exception);
        }

        public class Builder
        {
            private readonly ArrayBuilder<ImmutableArray<(T, EntryState)>> _states = ArrayBuilder<ImmutableArray<(T, EntryState)>>.GetInstance();

            private int _removedCount = 0;

            private Exception? _exception = null;

            public void AddEntries(ImmutableArray<T> values, EntryState state)
            {
                _states.Add(values.Select(v => (v, state)).ToImmutableArray());
            }

            public void AddEntries(ImmutableArray<(T, EntryState)> values)
            {
                _states.Add(values);
            }

            public void RemoveEntries(int inputIndex)
            {
                Debug.Assert(inputIndex < _states.Count);
                _removedCount++;
                _states[inputIndex] = _states[inputIndex].Select((t) => (t.Item1, EntryState.Removed)).ToImmutableArray();
            }

            public void SetEntries(int inputIndex, IEnumerable<(T, EntryState)> values)
            {
                Debug.Assert(inputIndex < _states.Count);
                _states[inputIndex] = values.ToImmutableArray();
            }

            public void SetFaulted(Exception e)
            {
                _exception = e;
            }

            public NodeStateTable<T> ToImmutableAndFree() => new NodeStateTable<T>(_states.ToImmutableAndFree(), _removedCount, exception: _exception);
        }
    }
}
