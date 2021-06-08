// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class BatchTransformNode<TInput, TOutput> : IIncrementalGeneratorNode<TOutput>
    {
        private readonly Func<ImmutableArray<TInput>, ImmutableArray<TOutput>> _func;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;
        private readonly IEqualityComparer<TOutput> _comparer;

        public BatchTransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<ImmutableArray<TInput>, TOutput> userFunc, IEqualityComparer<TOutput>? comparer = null)
            : this(sourceNode, userFunc: (i) => ImmutableArray.Create(userFunc(i)), comparer)
        {
        }

        public BatchTransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<ImmutableArray<TInput>, ImmutableArray<TOutput>> userFunc, IEqualityComparer<TOutput>? comparer = null)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
            _comparer = comparer ?? EqualityComparer<TOutput>.Default;
        }

        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new BatchTransformNode<TInput, TOutput>(_sourceNode, _func, comparer);

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);
            if (sourceTable.IsCached)
            {
                return previousTable;
            }

            // Semantics of a batch transform:
            // Batches will always exist (a batch of the empty table is still [])
            // There is only ever one input, the batch of the upstream table
            // - Output is cached when upstream is all cached
            // - Added when the previous table was empty
            // - Modified otherwise

            var source = sourceTable.Batch();

            // apply the transform
            var transformed = _func(source);

            // update the table 
            var newTable = previousTable.ToBuilder();
            if (!newTable.TryModifyEntries(transformed, _comparer))
            {
                newTable.AddEntries(transformed, EntryState.Added);
            }

            return newTable.ToImmutableAndFree();
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
