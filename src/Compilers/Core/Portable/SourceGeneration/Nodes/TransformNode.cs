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
        private readonly UserFunc<TInput, IEnumerable<TOutput>> _func;

        public TransformNode(INode<TInput> sourceNode, UserFunc<TInput, TOutput> userFunc)
            : this(sourceNode, func: SingleTransform(userFunc), comparer: null)
        {
        }

        public TransformNode(INode<TInput> sourceNode, UserFunc<TInput, IEnumerable<TOutput>> userFunc)
            : this(sourceNode, userFunc, comparer: null)
        {
        }

        private TransformNode(INode<TInput> sourceNode, UserFunc<TInput, IEnumerable<TOutput>> func, IEqualityComparer<TOutput>? comparer)
            : base(sourceNode, comparer)
        {
            _func = func;
        }

        private static UserFunc<TInput, IEnumerable<TOutput>> SingleTransform(UserFunc<TInput, TOutput> func) => (inputs) => ImmutableArray.Create(func(inputs));

        public INode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new TransformNode<TInput, TOutput>(_parentNode, _func, comparer);

        protected override StateTable<TOutput> UpdateStateTable(StateTable<TInput> sourceTable, StateTable<TOutput> previousTable)
        {
            var newTable = new StateTable<TOutput>.Builder();

            // foreach entry in the source table, we create a new updated table entry
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
                    var newEntries = _func(entry.item).ToImmutableArray();

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

    internal class BatchTransformNode<TInput, TOutput> : AbstractSingleParentNode<TInput, TOutput>, INode<TOutput>
    {
        private readonly UserFunc<IEnumerable<TInput>, IEnumerable<TOutput>> _func;

        public BatchTransformNode(INode<TInput> sourceNode, UserFunc<IEnumerable<TInput>, TOutput> userFunc)
            : this(sourceNode, func: SingleTransform(userFunc), comparer: null)
        {
        }

        public BatchTransformNode(INode<TInput> sourceNode, UserFunc<IEnumerable<TInput>, IEnumerable<TOutput>> userFunc)
            : this(sourceNode, userFunc, comparer: null)
        {
        }

        private BatchTransformNode(INode<TInput> sourceNode, UserFunc<IEnumerable<TInput>, IEnumerable<TOutput>> func, IEqualityComparer<TOutput>? comparer)
            : base(sourceNode, comparer)
        {
            _func = func;
        }

        private static UserFunc<IEnumerable<TInput>, IEnumerable<TOutput>> SingleTransform(UserFunc<IEnumerable<TInput>, TOutput> func) => (inputs) => ImmutableArray.Create(func(inputs));

        public INode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new BatchTransformNode<TInput, TOutput>(_parentNode, _func, comparer);

        protected override StateTable<TOutput> UpdateStateTable(StateTable<TInput> sourceTable, StateTable<TOutput> previousTable)
        {
            var newTable = new StateTable<TOutput>.Builder();

            var inputs = sourceTable.GetEnumerable().Where(t => t.state != EntryState.Removed).Select(t => t.item).ToImmutableArray();
            var outputs = _func(inputs).ToImmutableArray();

            // we still need to loop over each entry and make a decision per-the last entry.
            // it's essentially the same logic as the non-batch, except we don't do the transform

            // hmm, is it? 
            
            // I think we need to work thru a couple exampls here.
            // so maybe we should park this, and do the interface stuff, hmm?


            foreach (var entry in sourceTable)
            {
                if (entry.state == EntryState.Cached || entry.state == EntryState.Removed)
                {

                }
            }

            newTable.AddEntries(outputs, EntryState.Modified); //?

            return newTable.ToImmutableAndFree();
        }
    }



}
