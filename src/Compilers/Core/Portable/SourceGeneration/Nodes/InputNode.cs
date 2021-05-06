// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Input nodes don't actually do anything. They are just placeholders for the value sources
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class InputNode<T> : IIncrementalGeneratorNode<T>
    {
        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            // the input node doesn't change the table. 
            // instead the driver manipulates the previous table to contain the current state of the node.
            // we can just return that state, as it's already up to date.
            return previousTable;
        }

        // PROTOTYPE(source-generators): how does this work? we definitely want to be able to add custom comparers to the input nodes
        // I guess its just a 'compare only' node with this as the input?
        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => this;

        public NodeStateTable<T> CreateInputTable(NodeStateTable<T> previousTable, T item)
        {
            // PROTOTYPE(source-generators): we should compare the values, not just assume they were added
            return NodeStateTable<T>.WithSingleItem(item, EntryState.Added);
        }

        public NodeStateTable<T> CreateInputTable(NodeStateTable<T> previousTable, IEnumerable<T> items)
        {
            // create a mutable hashset of the new items we can check against
            PooledHashSet<T> itemsSet = PooledHashSet<T>.GetInstance();
            foreach (var item in items)
            {
                itemsSet.Add(item);
            }

            var builder = previousTable.ToBuilderWithABetterName();

            // for each item in the previous table, check if its still in the new items
            foreach ((var oldItem, _) in previousTable)
            {
                bool inItemSet = itemsSet.Remove(oldItem);
                builder.AddEntriesFromPreviousTable(inItemSet ? EntryState.Cached : EntryState.Removed);
            }

            // any remaining new items are added
            foreach (var newItem in itemsSet)
            {
                builder.AddEntries(ImmutableArray.Create(newItem), EntryState.Added);
            }
            itemsSet.Free();
            return builder.ToImmutableAndFree();
        }
    }
}
