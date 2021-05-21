// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Input nodes are the 'root' nodes in the graph, and get their values from the inputs of the driver state table
    /// </summary>
    /// <typeparam name="T">The type of the input</typeparam>
    internal sealed class InputNode<T> : IIncrementalGeneratorNode<T>
    {
        private readonly Func<DriverStateTable.Builder, ImmutableArray<T>> _getInput;
        private readonly Action<IIncrementalGeneratorOutputNode> _registerOutput;
        private readonly IEqualityComparer<T>? _comparer;

        public InputNode(Func<DriverStateTable.Builder, ImmutableArray<T>> getInput)
            : this(getInput, registerOutput: null, comparer: null)
        {
        }

        private InputNode(Func<DriverStateTable.Builder, ImmutableArray<T>> getInput, Action<IIncrementalGeneratorOutputNode>? registerOutput, IEqualityComparer<T>? comparer = null)
        {
            _getInput = getInput;
            _comparer = comparer;
            _registerOutput = registerOutput ?? (o => throw ExceptionUtilities.Unreachable);
        }

        public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
        {
            var inputItems = _getInput(graphState);

            // create a mutable hashset of the new items we can check against
            HashSet<T> itemsSet = new HashSet<T>(_comparer);
            foreach (var item in inputItems)
            {
                var added = itemsSet.Add(item);
                Debug.Assert(added);
            }

            var builder = previousTable.ToBuilder();

            // for each item in the previous table, check if its still in the new items
            foreach ((var oldItem, _) in previousTable)
            {
                if (itemsSet.Remove(oldItem))
                {
                    // we're iterating the table, so know that it has entries
                    var usedCache = builder.TryUseCachedEntries();
                    Debug.Assert(usedCache);
                }
                else
                {
                    builder.RemoveEntries();
                }
            }

            // any remaining new items are added
            foreach (var newItem in itemsSet)
            {
                builder.AddEntry(newItem, EntryState.Added);
            }

            return builder.ToImmutableAndFree();
        }

        public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => new InputNode<T>(_getInput, _registerOutput, comparer);

        public InputNode<T> WithRegisterOutput(Action<IIncrementalGeneratorOutputNode> registerOutput) => new InputNode<T>(_getInput, registerOutput, _comparer);

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _registerOutput(output);
    }
}
