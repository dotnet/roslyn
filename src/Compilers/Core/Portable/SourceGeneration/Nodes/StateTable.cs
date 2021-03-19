// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal enum EntryState { Added, Removed, Modified, Cached };

    internal interface IStateTable
    {
        IStateTable Compact();
    }

    internal class StateTable<T> : IStateTable
    {
        internal static StateTable<T> Empty { get; } = new StateTable<T>(ImmutableArray<ImmutableArray<(T, EntryState)>>.Empty, 0);

        readonly ImmutableArray<ImmutableArray<(T, EntryState)>> _states;

        readonly int _removedCount;

        private StateTable(ImmutableArray<ImmutableArray<(T, EntryState)>> states, int removedCount)
        {
            _states = states;
            _removedCount = removedCount;
        }

        public ImmutableArray<T> GetEntries(int inputIndex)
        {
            if (inputIndex < _states.Length)
            {
                return _states[inputIndex].SelectAsArray(e => e.Item1);
            }
            else
            {
                return ImmutableArray<T>.Empty;
            }
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
            return new StateTable<T>(compacted.ToImmutableAndFree(), 0);
        }

        public class Builder
        {
            private readonly ArrayBuilder<ImmutableArray<(T, EntryState)>> _states = ArrayBuilder<ImmutableArray<(T, EntryState)>>.GetInstance();

            private int _removedCount = 0;

            public void AddEntries(ImmutableArray<T> values, EntryState state)
            {
                _states.Add(values.Select(v => (v, state)).ToImmutableArray());
            }

            public void AddEntries(ImmutableArray<(T, EntryState)> values)
            {
                _states.Add(values);
            }

            // always removes everything
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

            public StateTable<T> ToImmutableAndFree() => new StateTable<T>(_states.ToImmutableAndFree(), _removedCount);
        }
    }

    internal class GraphStateTable
    {
        readonly ImmutableDictionary<object, IStateTable> _tables; // PROTOTYPE

        internal static GraphStateTable Empty { get; } = new GraphStateTable(ImmutableDictionary<object, IStateTable>.Empty);

        private GraphStateTable(ImmutableDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        public StateTable<T> GetLatestStateTableForNode<T>(INode<T> source)
        {
            return _tables.ContainsKey(source) ? (StateTable<T>)_tables[source] : StateTable<T>.Empty;
        }

        public class Builder
        {
            private readonly ImmutableDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableDictionary<object, IStateTable>.Empty.ToBuilder();

            private readonly GraphStateTable _previousTable;

            public Builder(GraphStateTable previousTable)
            {
                _previousTable = previousTable;
            }

            public StateTable<T> GetLatestStateTableForNode<T>(INode<T> source)
            {
                // if we've already evaluated node during this build, we can just return the existing result
                if (_tableBuilder.ContainsKey(source))
                {
                    return (StateTable<T>)_tableBuilder[source];
                }

                StateTable<T> previousTable = _previousTable.GetLatestStateTableForNode(source);

                var newTable = source.UpdateStateTable(this, previousTable);
                _tableBuilder[source] = newTable;
                return newTable;
            }

            public GraphStateTable ToImmutable()
            {
                // we can compact the tables at this point, as we'll no longer be using them to determine current state
                var keys = _tableBuilder.Keys.ToArray();
                for(int i = 0; i < _tableBuilder.Count; i++)
                {
                    _tableBuilder[keys[i]] = _tableBuilder[keys[i]].Compact();
                }

                return new GraphStateTable(_tableBuilder.ToImmutable());
            }
        }
    }
}
