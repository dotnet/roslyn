// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal sealed partial class NodeStateTable<T>
{
    public sealed partial class Builder
    {
        /// <summary>
        /// Helper type that keeps track of the TableEntrys that are being added and will reuse the same entries from
        /// the previous node table if we're producing the exact same ones.
        /// </summary>
        private struct StatesWrapper
        {
            private readonly NodeStateTable<T> _previous;
            private readonly IEqualityComparer<T> _equalityComparer;

            /// <summary>
            /// Note: we could potentially lazily initialize this the first time we see ourselves deviating from
            /// _previous.  However, this approach keeps things very simple here.
            /// </summary>
            private readonly ArrayBuilder<TableEntry> _tableEntries;

            /// <summary>
            /// Tracks if the new table we're building has all the same entries as the previous table.  If we end up
            /// with the same set of entries at the end, we can just point our new table at that same array, avoiding a
            /// costly allocation.
            /// </summary>
            private bool _unchangedFromPrevious = true;

            public StatesWrapper(NodeStateTable<T> previous, IEqualityComparer<T> equalityComparer)
            {
                _previous = previous;
                _equalityComparer = equalityComparer;
                _tableEntries = ArrayBuilder<TableEntry>.GetInstance(previous.Count);
            }

            public int Count
                => _tableEntries.Count;

            public TableEntry this[int index]
                => _tableEntries[index];

            public bool Any(Func<TableEntry, bool> predicate)
                => _tableEntries.Any(predicate);

            public void Free()
                => _tableEntries.Free();

            public ImmutableArray<TableEntry> ToImmutableAndFree()
            {
                // if we added the exact same entries as before, then we can directly embed previous' entry array,
                // avoiding a costly allocation of the same data.
                if (_unchangedFromPrevious && _tableEntries.Count == _previous._states.Length)
                {
                    _tableEntries.Free();
                    return _previous._states;
                }

                return _tableEntries.ToImmutableAndFree();
            }

            public void Add(TableEntry entry)
            {
                var currentindex = _tableEntries.Count;
                _tableEntries.Add(entry);

                // Keep checking if we're producing the same entries as in _previous.
                if (_unchangedFromPrevious && currentindex < _previous._states.Length)
                {
                    var previousEntry = _previous._states[currentindex];
                    _unchangedFromPrevious = entry.Matches(previousEntry, _equalityComparer);
                }
            }
        }
    }
}
