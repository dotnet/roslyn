// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal class TransformNode<TInput, TOutput> : INode<TOutput>
    {
        private readonly INode<TInput> _sourceNode;
        private readonly Func<TInput, TOutput>? _singleFunc;
        private readonly IEqualityComparer<TOutput> _comparer;
        private readonly Func<TInput, IEnumerable<TOutput>>? _multiFunc;

        public TransformNode(INode<TInput> sourceNode, Func<TInput, TOutput> singleFunc)
            : this(sourceNode, singleFunc, multiFunc: null, EqualityComparer<TOutput>.Default)
        {
        }

        public TransformNode(INode<TInput> sourceNode, Func<TInput, IEnumerable<TOutput>> multiFunc)
            : this(sourceNode, singleFunc: null, multiFunc, EqualityComparer<TOutput>.Default)
        {
        }

        private TransformNode(INode<TInput> sourceNode, Func<TInput, TOutput>? singleFunc, Func<TInput, IEnumerable<TOutput>>? multiFunc, IEqualityComparer<TOutput> comparer)
        {
            _sourceNode = sourceNode;
            _singleFunc = singleFunc;
            _comparer = comparer;
            _multiFunc = multiFunc;
        }

        public INode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new TransformNode<TInput, TOutput>(_sourceNode, _singleFunc, _multiFunc, comparer);

        public StateTable<TOutput> UpdateStateTable(GraphStateTable.Builder stateTable, StateTable<TOutput> previousTable)
        {
            // get the parent state table
            var sourceTable = stateTable.GetLatestStateTableForNode(_sourceNode);

            // TODO: if the input node returns all cached values, then we can just
            //       return the previous table.

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
                        //TODO: assert that this entry is at a greater index than the child table.
                        //      added nodes should always come at the *end* of the parent table, never in between
                        //      or it will alter the following indicies.

                        //TODO: how does that affect arrays when we collect/join?
                        //      I dont think that'll be true will it?

                        // insert the new entry. remember the new offset
                        newTable.AddEntries(newEntries, EntryState.Added);
                    }
                    else
                    {
                        var oldEntries = previousTable.GetEntries(entry.index).ToImmutableArray();

                        bool areEqualLength = oldEntries.Length == newEntries.Length;
                        var updatedEntries = ArrayBuilder<(TOutput, EntryState)>.GetInstance(areEqualLength ? oldEntries.Length : oldEntries.Length + newEntries.Length);

                        // if the old entries are the same length as the new ones, we do an element x element check for modification.
                        if (areEqualLength)
                        {
                            for (int i = 0; i < newEntries.Length; i++)
                            {
                                var cacheState = _comparer.Equals(oldEntries[i], newEntries[i]) ? EntryState.Cached : EntryState.Modified;
                                updatedEntries.Add((newEntries[i], cacheState));
                            }
                        }
                        // when they are different, we just set the old entries as removed, and add new ones.
                        else
                        {
                            updatedEntries.AddRange(oldEntries.SelectAsArray(o => (o, EntryState.Removed)));
                            updatedEntries.AddRange(newEntries.SelectAsArray(e => (e, EntryState.Added)));
                        }
                        newTable.AddEntries(updatedEntries.ToImmutableArray());
                    }
                }
            }

            return newTable.ToImmutableAndFree();
        }
    }
}
