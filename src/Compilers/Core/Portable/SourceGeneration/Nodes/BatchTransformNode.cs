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
    internal class BatchTransformNode<TInput, TOutput> : IIncrementalGeneratorNode<TOutput>
    {
        private readonly Func<IEnumerable<TInput>, IEnumerable<TOutput>> _func;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;

        public BatchTransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<IEnumerable<TInput>, TOutput> userFunc)
            : this(sourceNode, userFunc: (i) => ImmutableArray.Create(userFunc(i)))
        {
        }

        public BatchTransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<IEnumerable<TInput>, IEnumerable<TOutput>> userFunc)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
        }

        // PROTOTYPE(source-generators):
        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => this;

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            // PROTOTYPE(source-generators):caching, faulted etc.

            // Semantics of a batch transform:
            // Batches will always exist (a batch of the empty table is still [])
            // There is only ever one input, the batch of the upstream table
            // - Output is cached when upsteam is all cached
            // - Added when the previous table was empty
            // - Modified otherwise

            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);

            var source = sourceTable.Batch(out var allCached); // PROTOTYPE(source-generators): we don't need the all cached if we have an IsCached property on the table?

            //if all cached, then we don't do anything, right?
            if (allCached)
                return previousTable;

            // apply the transform
            var transformed = _func(source).ToImmutableArray();

            // update the table 
            var newTable = new NodeStateTable<TOutput>.Builder();
            if (previousTable.IsEmpty)
            {
                newTable.AddEntries(transformed, EntryState.Added);
            }
            else
            {
                newTable.ModifyEntriesFromPreviousTable(previousTable, transformed);
            }
            return newTable.ToImmutableAndFree();
        }
    }
}
