// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class TransformNode<TInput, TOutput> : IIncrementalGeneratorNode<TOutput>
    {
        private readonly Func<TInput, ImmutableArray<TOutput>> _func;
        private readonly IEqualityComparer<TOutput> _comparer;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, TOutput> userFunc, IEqualityComparer<TOutput>? comparer = null)
            : this(sourceNode, userFunc: i => ImmutableArray.Create(userFunc(i)), comparer)
        {
        }

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, ImmutableArray<TOutput>> userFunc, IEqualityComparer<TOutput>? comparer = null)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
            _comparer = comparer ?? EqualityComparer<TOutput>.Default;
        }

        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new TransformNode<TInput, TOutput>(_sourceNode, _func, comparer);

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);
            if (sourceTable.IsCached)
            {
                return previousTable;
            }

            // Semantics of a transform:
            // Element-wise comparison of upstream table
            // - Cached or Removed: no transform, just use previous values
            // - Added: perform transform and add
            // - Modified: perform transform and do element wise comparison with previous results

            var newTable = previousTable.ToBuilder();

            foreach (var entry in sourceTable)
            {
                if (entry.state == EntryState.Removed)
                {
                    newTable.RemoveEntries();
                }
                else if (entry.state != EntryState.Cached || !newTable.TryUseCachedEntries())
                {
                    // generate the new entries
                    var newOutputs = _func(entry.item);

                    if (entry.state != EntryState.Modified || !newTable.TryModifyEntries(newOutputs, _comparer))
                    {
                        newTable.AddEntries(newOutputs, EntryState.Added);
                    }
                }
            }
            return newTable.ToImmutableAndFree();
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
