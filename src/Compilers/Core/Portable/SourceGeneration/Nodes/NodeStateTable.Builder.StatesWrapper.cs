// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal sealed partial class NodeStateTable<T>
{
    public sealed partial class Builder
    {
        private struct StatesWrapper
        {
            private readonly Builder _builder;
            private readonly ArrayBuilder<TableEntry> _tableEntries;

            /// <summary>
            /// Tracks if the new table we're building has all the same entries as the previous table.  If we end up
            /// with the same set of entries at the end, we can just point our new table at that same array, avoiding a
            /// costly allocation.
            /// </summary>
            public bool UnchangedFromPrevious = true;

            public StatesWrapper(Builder builder)
            {
                _builder = builder;
                _tableEntries = ArrayBuilder<TableEntry>.GetInstance(builder._previous.Count);
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
                => _tableEntries.ToImmutableAndFree();

            public void Add(TableEntry entry)
            {
                var currentindex = _tableEntries.Count;
                _tableEntries.Add(entry);

                // Keep checking if we're producing the same entries as in _previous.
                if (this.UnchangedFromPrevious && currentindex < _builder._previous._states.Length)
                {
                    var previousEntry = _builder._previous._states[currentindex];
                    this.UnchangedFromPrevious = entry.Matches(previousEntry, _builder._equalityComparer);
                }
            }
        }
    }
}
