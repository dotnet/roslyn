// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal class TransformNode<TInput, TOutput> : INode<TOutput>
    {
        private readonly INode<TInput> _sourceNode;
        private readonly Func<TInput, TOutput>? _singleFunc;
        private readonly Func<TInput, IEnumerable<TOutput>>? _multiFunc;

        public TransformNode(INode<TInput> sourceNode, Func<TInput, TOutput> func)
        {
            _sourceNode = sourceNode;
            _singleFunc = func;
            _multiFunc = null;
        }

        public TransformNode(INode<TInput> sourceNode, Func<TInput, IEnumerable<TOutput>> func)
        {
            _sourceNode = sourceNode;
            _singleFunc = null;
            _multiFunc = func;
        }

        public StateTable<TOutput> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<TOutput> previousTable)
        {
            // get the parent state table
            var sourceTable = stateTable.GetLatestStateTableForNode(_sourceNode);

            int addedOffset = 0;

            var newTable = new StateTable<TOutput>.Builder();

            // foreach entry in the source table, we create a new updated table
            foreach (var entry in sourceTable)
            {
                if (entry.state == EntryState.Cached || entry.state == EntryState.Removed)
                {
                    var previousEntries = previousTable.GetEntries(entry.index);
                    newTable.AddEntries(previousEntries, entry.state);
                }
                else if (entry.state == EntryState.Added || entry.state == EntryState.Modified)
                {
                    // generate the new entries
                    ImmutableArray<TOutput> newEntries = _singleFunc is object
                                                         ? ImmutableArray.Create(_singleFunc(entry.item))
                                                         : _multiFunc!(entry.item).ToImmutableArray();

                    if (entry.state == EntryState.Added)
                    {
                        // insert the new entry. remember the new offset
                        newTable.AddEntries(newEntries, EntryState.Added);
                        addedOffset++;
                    }
                    else
                    {
                        // grab the old entries. (TODO: we need to consider the added offset here, no?) Need to figure this part out, but my brain hurts without an example to do so.

                        var oldEntries = previousTable.GetEntries(entry.index).ToImmutableArray();

                        IEqualityComparer<TOutput> comparer = EqualityComparer<TOutput>.Default;

                        // if the old entries are the same length as the new ones, we do an element x element check for modification.
                        if (oldEntries.Length == newEntries.Length)
                        {
                            //TODO: use arraybuilder
                            List<(TOutput, EntryState)> updatedEntries = new List<(TOutput, EntryState)>(newEntries.Length);
                            for (int i = 0; i < newEntries.Length; i++)
                            {
                                var cacheState = comparer.Equals(oldEntries[i], newEntries[i]) ? EntryState.Cached : EntryState.Modified;
                                updatedEntries.Add((newEntries[i], cacheState));
                            }

                            //TODO: setEntries will never work now though? because we're *always* rebuilding the table from scratch?
                            newTable.AddEntries(updatedEntries.ToImmutableArray());
                        }
                        // when they are different, we just set the old entries as removed, and add new ones.
                        else
                        {
                            //TODO: use builder
                            List<(TOutput, EntryState)> updatedEntries = new List<(TOutput, EntryState)>(oldEntries.Length + newEntries.Length);
                            updatedEntries.AddRange(oldEntries.SelectAsArray(o => (o, EntryState.Removed)));
                            updatedEntries.AddRange(newEntries.SelectAsArray(e => (e, EntryState.Added)));
                            newTable.AddEntries(updatedEntries.ToImmutableArray());
                        }
                    }
                }
            }

            return newTable.ToImmutableAndFree();
        }
    }
}
