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
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal class TransformNode<TInput, TOutput> : AbstractSingleParentNode<TInput, TOutput>, INode<TOutput>
    {
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
            : base(sourceNode)
        {
            _singleFunc = singleFunc;
            _comparer = comparer;
            _multiFunc = multiFunc;
        }

        public INode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new TransformNode<TInput, TOutput>(_parentNode, _singleFunc, _multiFunc, comparer);

        protected override StateTable<TOutput> UpdateStateTable(StateTable<TInput> sourceTable, StateTable<TOutput> previousTable)
        {
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
                    if (!TryGetTransformedEntries(entry.index, entry.item, entry.state, out var newEntries, out var exc))
                    {
                        newTable.SetFaulted(exc);
                        break;
                    }

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

        private bool TryGetTransformedEntries(int index, TInput item, EntryState state, out ImmutableArray<TOutput> output, [NotNullWhen(false)]out Exception? exc)
        {
            if (_singleFunc is object)
            {
                TOutput singleItem;
                try
                {
                    singleItem = _singleFunc(item);
                }
                catch (Exception e)
                {
                    output = ImmutableArray<TOutput>.Empty;
                    exc = e;
                    return false;
                }
                output = ImmutableArray.Create(singleItem);
            }
            else
            {
                Debug.Assert(_multiFunc is object);
                try
                {
                    output = _multiFunc(item).ToImmutableArray();
                }
                catch (Exception e)
                {
                    output = ImmutableArray<TOutput>.Empty;
                    exc = e;
                    return false;
                }
            }

            exc = null;
            return true;
        }


    }
}
