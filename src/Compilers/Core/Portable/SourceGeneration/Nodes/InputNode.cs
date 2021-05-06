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
        private readonly InputNode<T> _inputSource;
        private readonly IEqualityComparer<T>? _comparer;

        public InputNode(InputNode<T>? inputSource = null, IEqualityComparer<T>? comparer = null)
        {
            _inputSource = inputSource ?? this;
            _comparer = comparer;
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            var inputItems = graphState.GetInputValue(_inputSource);

            // create a mutable hashset of the new items we can check against
            PooledHashSet<T> itemsSet = PooledHashSet<T>.GetInstance();
            foreach (var item in inputItems)
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

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new InputNode<T>(_inputSource, comparer);
    }
}
