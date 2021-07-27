// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class BatchNode<TInput> : IIncrementalGeneratorNode<ImmutableArray<TInput>>
    {
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;
        private readonly IEqualityComparer<ImmutableArray<TInput>> _comparer;

        public BatchNode(IIncrementalGeneratorNode<TInput> sourceNode, IEqualityComparer<ImmutableArray<TInput>>? comparer = null)
        {
            _sourceNode = sourceNode;
            _comparer = comparer ?? EqualityComparer<ImmutableArray<TInput>>.Default;
        }

        public IIncrementalGeneratorNode<ImmutableArray<TInput>> WithComparer(IEqualityComparer<ImmutableArray<TInput>> comparer) => new BatchNode<TInput>(_sourceNode, comparer);

        public NodeStateTable<ImmutableArray<TInput>> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<ImmutableArray<TInput>> previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);

            // Semantics of a batch transform:
            // Batches will always exist (a batch of the empty table is still [])
            // There is only ever one input, the batch of the upstream table
            // - Output is cached when upstream is all cached
            // - Added when the previous table was empty
            // - Modified otherwise

            var source = sourceTable.Batch();

            // update the table 
            var newTable = previousTable.ToBuilder();
            if (!sourceTable.IsCached || !newTable.TryUseCachedEntries())
            {
                if (!newTable.TryModifyEntry(source, _comparer))
                {
                    newTable.AddEntry(source, EntryState.Added);
                }
            }

            return newTable.ToImmutableAndFree();
        }

        public void RegisterOutput(IIncrementalGeneratorOutputNode output) => _sourceNode.RegisterOutput(output);
    }
}
