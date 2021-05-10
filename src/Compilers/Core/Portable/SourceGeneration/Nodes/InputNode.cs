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
    /// Input nodes are the 'root' nodes in the graph, and get their values from the inputs of the driver state table
    /// </summary>
    /// <typeparam name="T">The type of the input</typeparam>
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
            HashSet<T> itemsSet = new HashSet<T>(_comparer);
            foreach (var item in inputItems)
            {
                itemsSet.Add(item);
            }

            var builder = previousTable.ToBuilder();

            // for each item in the previous table, check if its still in the new items
            foreach ((var oldItem, _) in previousTable)
            {
                if (itemsSet.Remove(oldItem))
                {
                    // we're iterating the table, so know that it has entries
                    builder.TryUseCachedEntries();
                }
                else
                {
                    builder.RemoveEntries();
                }
            }

            // any remaining new items are added
            foreach (var newItem in itemsSet)
            {
                builder.AddEntries(ImmutableArray.Create(newItem), EntryState.Added);
            }

            return builder.ToImmutableAndFree();
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new InputNode<T>(_inputSource, comparer);
    }
}
